using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Infrastructure.Email;
using SmartHorse.Infrastructure.Identity;
using SmartHorse.Infrastructure.Images;
using SmartHorse.Infrastructure.Persistence;
using SmartHorse.Infrastructure.Persistence.Interceptors;
using SmartHorse.Infrastructure.Persistence.Repositories;
using SmartHorse.Infrastructure.Persistence.Seed;
using SmartHorse.Infrastructure.Services;

namespace SmartHorse.Infrastructure;

/// <summary>
/// Infrastructure-layer composition root (v0.1 Section 10). Wires EF Core, the
/// repository implementations, JWT/password/token services, caching, file
/// storage, and the email subsystem to their Application-layer interfaces.
/// Called once from the API layer's Program.cs, alongside AddApplication().
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- DIAGNOSTIC INTERCEPTORS (temporary — Login DbUpdateConcurrencyException
        // root-cause investigation). Both interceptors log full SQL parameter values
        // and full entity property values — including password hashes and refresh-
        // token hashes — on every request. That logging is NOT just a formatting
        // detail of EnableSensitiveDataLogging: DbCommandInterceptor/SaveChangesInterceptor
        // read raw parameter/property values directly and log them via ILogger
        // regardless of any EF Core "sensitive data" setting. So the only safe way to
        // keep sensitive values out of the logs by default is to not register these
        // interceptors at all unless verbose diagnostics have been explicitly turned on.
        // Gated entirely behind Diagnostics:EnableVerboseSqlLogging, which defaults to
        // false in appsettings.json and is only set to true in
        // appsettings.Development.json. Remove these two registrations (and the
        // "AddInterceptors" call below) plus the "Diagnostics" section in appsettings
        // once the root cause is confirmed.
        var enableVerboseSqlLogging = configuration.GetValue<bool>("Diagnostics:EnableVerboseSqlLogging");

        if (enableVerboseSqlLogging)
        {
            services.AddScoped<ChangeTrackerDiagnosticsInterceptor>();
            services.AddScoped<SqlDiagnosticsInterceptor>();
        }

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            if (enableVerboseSqlLogging)
            {
                // EnableSensitiveDataLogging is required for parameter VALUES (not just
                // parameter names) to appear in EF's own
                // Microsoft.EntityFrameworkCore.Database.Command log category. It is
                // deliberately gated behind this config flag — do not leave it on in a
                // real production environment, since it will log emails, token hashes,
                // and IP addresses. The interceptors are only registered above when this
                // same flag is true, so nothing in this block runs at all — not merely
                // "runs without sensitive values" — unless verbose diagnostics were
                // explicitly enabled.
                options.AddInterceptors(
                    provider.GetRequiredService<ChangeTrackerDiagnosticsInterceptor>(),
                    provider.GetRequiredService<SqlDiagnosticsInterceptor>());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // ---- Repositories (v0.1 §11 — Repository Pattern) ----
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // ---- Horse Core repositories (Person 2 Sprint 1 §8 — Repository Pattern) ----
        services.AddScoped<IHorseRepository, HorseRepository>();
        services.AddScoped<IBreedRepository, BreedRepository>();
        services.AddScoped<IColorRepository, ColorRepository>();
        services.AddScoped<IGenderRepository, GenderRepository>();
        services.AddScoped<IHorseStatusRepository, HorseStatusRepository>();
        services.AddScoped<IOwnershipHistoryRepository, OwnershipHistoryRepository>();

        // ---- Horse Images (Person 2 Sprint 2 §7 — Cloudinary, behind IImageStorageService) ----
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.Configure<ImageValidationSettings>(configuration.GetSection(ImageValidationSettings.SectionName));
        services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();

        // ---- Identity / JWT (v0.2 §8; Sprint 2 §9 — Configuration Validation) ----
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

        // Singleton by design: JwtSigningKeyProvider parses the RSA private key
        // PEM exactly once and holds one long-lived RSA instance for the whole
        // process lifetime. This is the root-cause fix for the
        // ObjectDisposedException('RSA') failure — see the XML doc on
        // IJwtSigningKeyProvider for the full explanation. Do not change this to
        // Scoped/Transient.
        services.AddSingleton<IJwtSigningKeyProvider, JwtSigningKeyProvider>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        // ---- Email (Sprint 2 §1 — SMTP now, SendGrid ready) ----
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));

        var emailProvider = configuration[$"{EmailSettings.SectionName}:Provider"] ?? "Smtp";
        if (string.Equals(emailProvider, "SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SendGridEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        services.AddScoped<IEmailService, EmailService>();

        // ---- File storage (Sprint 2 §3 — local disk now; v0.2 §6 targets Azure Blob later) ----
        services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // ---- Caching (Sprint 2 §12 — IMemoryCache now, Redis-ready abstraction) ----
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // ---- Request context / current user (v0.1 §11; Sprint 2 §6 audit metadata) ----
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestContextAccessor, RequestContextAccessor>();

        services.AddScoped<DbSeeder>();

        return services;
    }
}
