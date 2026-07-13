using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartHorse.Infrastructure.Persistence;

namespace SmartHorse.API.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (Program.cs, middleware, DI graph) against an
/// isolated EF Core InMemory database instead of SQL Server (Sprint 2 §14 —
/// Integration Tests), so tests are fast and require no external SQL Server
/// instance. A fresh RSA key pair is generated per test run so JWT
/// issuance/validation is exercised end-to-end, not mocked.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"SmartHorseTests_{Guid.NewGuid()}";
    private readonly (string PrivateKeyPem, string PublicKeyPem) _jwtKeys = GenerateRsaKeyPair();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "SmartHorseApi.Tests",
                ["Jwt:Audience"] = "SmartHorseClients.Tests",
                ["Jwt:PrivateKeyPem"] = _jwtKeys.PrivateKeyPem,
                ["Jwt:PublicKeyPem"] = _jwtKeys.PublicKeyPem,
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["Seed:AdminEmail"] = "admin@smarthorse.tests",
                ["Seed:AdminPassword"] = "AdminPass123!",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["ConnectionStrings:DefaultConnection"] = "Server=(unused);Database=Unused;Trusted_Connection=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    private static (string PrivateKeyPem, string PublicKeyPem) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        return (privatePem, publicPem);
    }
}
