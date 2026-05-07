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

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowBlazorHost");

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
});

app.Run();
