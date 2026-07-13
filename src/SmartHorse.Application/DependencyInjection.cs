using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartHorse.Application.Common.Behaviors;

namespace SmartHorse.Application;

/// <summary>
/// Application-layer composition root. Registers MediatR handlers, FluentValidation
/// validators, the validation pipeline behavior, and AutoMapper profiles found in
/// this assembly. Called once from the API layer's Program.cs (v0.1 Section 10 —
/// dependencies point inward; the API layer is the only place that knows about
/// every layer's DI extension method).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
