using BlazorAuthNZDemo.Client.Models;
using BlazorAuthNZDemo.Client.Pages;
using BlazorAuthNZDemo.Components;
using BlazorAuthNZDemo.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<BandsRepository>();

builder.Services.AddHttpClient("RemoteAPIClient", cfg =>
{
    cfg.BaseAddress = new Uri(
        builder.Configuration["RemoteAPIBaseAddress"]
            ?? throw new Exception("RemoteAPIBaseAddress is missing."));
});

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
});

// Forward remote API calls through the host
app.MapGet("/forward-to-remote-api/bands", async (IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("RemoteAPIClient");
    var response = await httpClient.GetAsync("remoteapi/bands");
    response.EnsureSuccessStatusCode();

    var bands = await response.Content.ReadFromJsonAsync<IEnumerable<Band>>(
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    return Results.Ok(bands);
});

app.Run();
