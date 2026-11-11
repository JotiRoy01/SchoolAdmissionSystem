
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.BackgroundServices;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services.Implementations;
using SchoolAdmission.Api.Services.Interfaces;
using SchoolAdmission.Api.Middleware;

// Load the repository-level .env file before building configuration. Values use
// ASP.NET Core's nested-key syntax, for example ConnectionStrings__DefaultConnection.
var envFile = FindEnvironmentFile(Directory.GetCurrentDirectory());
if (envFile is not null)
{
    DotNetEnv.Env.Load(envFile);
}

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL database connection.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection must be configured.");
}

builder.Services.AddDbContext<AdmissionDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register application services.
builder.Services.AddScoped<IAdmissionHoldService, AdmissionHoldService>();
builder.Services.AddScoped<IAdmissionClassService, AdmissionClassService>();

// Register automatic hold expiration worker.
builder.Services.AddHostedService<HoldExpiryWorker>();

// Register controllers.
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Standardize automatic DTO/model validation responses.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? $"{entry.Key} is invalid."
                    : error.ErrorMessage))
            .ToList();

        return new BadRequestObjectResult(
            new ApiErrorResponse
            {
                Message = "Request validation failed.",
                Errors = errors
            });
    };
});

// OpenAPI configuration.
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
    var retries = 10;

    while (true)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (retries-- > 0)
        {
            app.Logger.LogWarning(
                ex,
                "Database is not ready. Retrying migration ({RetriesRemaining} retries remaining).",
                retries);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();



// Configure HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("FrontendPolicy");

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static string? FindEnvironmentFile(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);

    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, ".env");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    return null;
}
