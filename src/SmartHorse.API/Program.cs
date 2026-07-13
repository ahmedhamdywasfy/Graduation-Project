using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using SmartHorse.API.Common;
using SmartHorse.API.Extensions;
using SmartHorse.API.Middleware;
using SmartHorse.Application;
using SmartHorse.Infrastructure;
using SmartHorse.Infrastructure.HealthChecks;
using SmartHorse.Infrastructure.Persistence.Seed;

// Bootstrap logger: catches startup failures before the full Serilog pipeline is configured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---- Serilog (v0.1 Section 23 — Logging Strategy) ----
    // Output templates include {Properties} so CorrelationId (pushed by
    // CorrelationIdMiddleware) and the structured fields written by the
    // diagnostic interceptors (SQL-DIAG / CHANGETRACKER-DIAG — temporary,
    // Login DbUpdateConcurrencyException investigation) are visible in both
    // sinks instead of only being available to structured log viewers.
    const string diagnosticOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithCorrelationId()
        .WriteTo.Console(outputTemplate: diagnosticOutputTemplate)
        .WriteTo.File("logs/smarthorse-.log", rollingInterval: RollingInterval.Day, outputTemplate: diagnosticOutputTemplate));

    // ---- Layer registrations (v0.1 Section 10 — Clean Architecture composition) ----
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ---- API layer concerns ----
    builder.Services.AddControllers(options =>
    {
        // Sprint 2 §8 — API Response Wrapper, applied globally so every controller
        // action gets a consistent success envelope without repeating boilerplate.
        options.Filters.Add<ApiResponseWrapperFilter>();
    });
    builder.Services.AddSmartHorseSwagger();
    builder.Services.AddSmartHorseAuthentication(builder.Configuration);

    // ---- API Versioning (Sprint 2 §13) — current version v1; routes stay literal
    // "api/v1/..." (v0.1 §25/§26 convention) rather than URL-segment-substituted,
    // so this is additive metadata/Swagger-grouping, not a routing change.
    builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = false;
        });

    // CORS: allow-list only, per v0.1/v0.2 Security Review Section 21/8.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SmartHorseCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Rate limiting on authentication endpoints (v0.2 Security Review, Section 8).
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });

    // ---- Health Checks (Sprint 2 §11) ----
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    builder.Services.AddHealthChecks()
        .AddSqlServer(connectionString, name: "database", tags: new[] { "ready" })
        .AddCheck<AuthenticationHealthCheck>("authentication", tags: new[] { "ready" })
        .AddCheck("application", () => HealthCheckResult.Healthy("API is running."), tags: new[] { "live" });

    var app = builder.Build();

    // Force eager construction of the JWT signing key now, at startup. It is
    // registered as a Singleton (see Infrastructure.DependencyInjection) and
    // would otherwise only be built lazily on the first Register/Login call —
    // a malformed Jwt:PrivateKeyPem should fail application startup, not the
    // first user-facing request.
    app.Services.GetRequiredService<SmartHorse.Infrastructure.Identity.IJwtSigningKeyProvider>();

    // ---- Seed Roles / Permissions / Administrator (v0.1 checklist items 20-21) ----
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }

    // ---- DIAGNOSTIC: log exactly which physical database this process is
    // talking to (Login DbUpdateConcurrencyException investigation — requirement
    // #7, "verify Register and Login use the same SQL Server database and
    // connection string"). This gives you a baseline log line at startup to diff
    // against the per-request "[SQL-DIAG]" / "[CHANGETRACKER-DIAG]" lines that
    // ChangeTrackerDiagnosticsInterceptor and SqlDiagnosticsInterceptor emit for
    // every Register and Login call. If the startup line, the Register call's
    // line, and the failing Login call's line ever show a different DataSource
    // or Database, that alone is the root cause. Remove this block once resolved.
    using (var scope = app.Services.CreateScope())
    {
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var diagContext = scope.ServiceProvider.GetRequiredService<SmartHorse.Infrastructure.Persistence.ApplicationDbContext>();
        var connection = diagContext.Database.GetDbConnection();

        // CanConnectAsync opens/closes the connection using the exact same
        // connection string/pool the app uses for every real request.
        var canConnect = await diagContext.Database.CanConnectAsync();

        startupLogger.LogWarning(
            "[STARTUP-DIAG] ApplicationDbContext resolves to DataSource={DataSource} Database={Database} " +
            "ConnectionState={ConnectionState} CanConnect={CanConnect}. " +
            "Compare this line's DataSource/Database against every later " +
            "[SQL-DIAG]/[CHANGETRACKER-DIAG] line for Register and Login requests.",
            connection.DataSource, connection.Database, connection.State, canConnect);
    }

    // ---- Middleware pipeline ----
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Skipped in the "Testing" environment (Sprint 2 §14 integration tests run
    // in-process via WebApplicationFactory over plain HTTP; redirecting would
    // turn every test request into a 307 instead of exercising the real endpoint).
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseCors("SmartHorseCorsPolicy");
    app.UseRateLimiter();

    // Serves uploaded avatars (Sprint 2 §3) from wwwroot — paired with FileStorage:PublicBaseUrl.
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // /health — overall liveness; /health/ready — readiness (DB + auth key checks).
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    error = e.Value.Exception?.Message
                })
            });
            await context.Response.WriteAsync(result);
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SmartHorse API terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}

// Sprint 2 §14 — exposes the top-level Program for WebApplicationFactory<Program> in SmartHorse.API.IntegrationTests.
public partial class Program { }
