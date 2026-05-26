using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorHost", policy =>
    {
        policy.WithOrigins("https://localhost:7224")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.microsoftonline.com/5c154a7e-0c13-4f92-8531-e3f4d8fbeae9/v2.0";
        options.Audience = "api://daea9f32-4036-4c37-8fb3-0e2caad96b98";
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "role",
            ValidIssuer = "https://sts.windows.net/5c154a7e-0c13-4f92-8531-e3f4d8fbeae9/"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("IsFromBelgium", new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("ctry", "BE")
        .Build());

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowBlazorHost");

app.UseAuthentication();
app.UseAuthorization();

// Define an API for testing
app.MapGet("/remoteapi/bands", () =>
{
    return Results.Ok(new[]
    {
        new { Id = 1, Name = "Arctic Monkeys (from remote API)" },
        new { Id = 2, Name = "Nine Inch Nails (from remote API)" },
        new { Id = 3, Name = "Bruce Springsteen (from remote API)" },
        new { Id = 4, Name = "Fleetwood Mac (from remote API)" }
    });
}).RequireAuthorization("IsFromBelgium");

app.Run();
