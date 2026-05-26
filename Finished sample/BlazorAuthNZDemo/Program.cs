using BlazorAuthNZDemo.Client.Authorization;
using BlazorAuthNZDemo.Client.Models;
using BlazorAuthNZDemo.Client.Services;
using BlazorAuthNZDemo.Components;
using BlazorAuthNZDemo.DelegatingHandlers;
using BlazorAuthNZDemo.Services;
using BlazorAuthNZDemo.TokenStores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

const string entraIdScheme = "EntraIDOpenIDConnect";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddScoped<BandsRepository>();

builder.Services.AddSingleton<ServerSideTokenStore>();
builder.Services.AddTransient<AccessTokenHandler>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("RemoteAPIClient", cfg =>
{
    cfg.BaseAddress = new Uri(
        builder.Configuration["RemoteAPIBaseAddress"]
            ?? throw new Exception("RemoteAPIBaseAddress is missing."));
}).AddHttpMessageHandler<AccessTokenHandler>();

// Register an HttpClient for components that inject HttpClient directly
// (used by InteractiveAuto components during server-side prerendering)
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient();
    client.BaseAddress = new Uri(
        builder.Configuration["HostBaseAddress"]
            ?? throw new Exception("HostBaseAddress is missing."));
    return client;
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = entraIdScheme;
}).AddOpenIdConnect(entraIdScheme, oidcOptions =>
{
    oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
    oidcOptions.UsePkce = true;
    oidcOptions.Scope.Add(OpenIdConnectScope.OpenIdProfile);
    oidcOptions.Scope.Add("api://daea9f32-4036-4c37-8fb3-0e2caad96b98/FullAccess");
    oidcOptions.Scope.Add("offline_access");
    oidcOptions.MapInboundClaims = false;
    oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
    oidcOptions.TokenValidationParameters.RoleClaimType = "role";
    oidcOptions.SaveTokens = true;
    oidcOptions.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            var tokenStore = context.HttpContext.RequestServices
                .GetRequiredService<ServerSideTokenStore>();
            var expiration = context.TokenEndpointResponse?.ExpiresIn is string exp
                        ? DateTimeOffset.UtcNow.AddSeconds(double.Parse(exp))
                        : DateTimeOffset.UtcNow.AddMinutes(60);
            tokenStore.StoreTokens(
                        context.Principal!,
                        context.TokenEndpointResponse?.AccessToken,
                        context.TokenEndpointResponse?.RefreshToken,
                        expiration);
            return Task.CompletedTask;
        }
    };

}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.AccessDeniedPath = "/accessdenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.IsFromBelgium, Policies.IsFromBelgiumPolicy());
    options.AddPolicy(Policies.RequiresAdminRole, Policies.RequiresAdminRolePolicy());
});
builder.Services.AddScoped<IBandsClient, ServerBandsClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorAuthNZDemo.Client._Imports).Assembly);

// Define a local API for testing
app.MapGet("/localapi/bands", (BandsRepository bandsRepository) =>
{
    return Results.Ok(bandsRepository.GetBands());
}).RequireAuthorization(Policies.IsFromBelgium); 

// Forward remote API calls through the host
app.MapGet("/forward-to-remote-api/bands", async (IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("RemoteAPIClient");
    var response = await httpClient.GetAsync("remoteapi/bands");
    response.EnsureSuccessStatusCode();

    var bands = await response.Content.ReadFromJsonAsync<IEnumerable<Band>>(
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    return Results.Ok(bands);
}).RequireAuthorization(Policies.IsFromBelgium);

app.MapGet("/login", (string? returnUrl, HttpContext httpContext) =>
{
    returnUrl = ValidateReturnUrl(httpContext, returnUrl);
    return TypedResults.Challenge(new AuthenticationProperties
    {
        RedirectUri = returnUrl
    });
}).AllowAnonymous();

app.MapPost("/logout", ([FromForm] string? returnUrl, HttpContext httpContext) =>
{
    returnUrl = ValidateReturnUrl(httpContext, returnUrl);
    return TypedResults.SignOut(new AuthenticationProperties
    {
        RedirectUri = returnUrl
    },
        [CookieAuthenticationDefaults.AuthenticationScheme, entraIdScheme]);
});


app.Run();

public partial class Program
{
    private static string ValidateReturnUrl(HttpContext httpContext, string? returnUrl)
    {
        string basePath = string.IsNullOrEmpty(httpContext.Request.PathBase)
            ? "/" : httpContext.Request.PathBase;
        if (string.IsNullOrEmpty(returnUrl))
        {
            return basePath;
        }
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            return new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        }
        else if (returnUrl[0] != '/')
        {
            return $"{basePath}{returnUrl}";
        }
        return returnUrl;
    }
}