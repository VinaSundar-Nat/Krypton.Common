using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kr.Common.Mediator;

public static class MediateExtensions
{
    /// <summary>
    /// Registers the Mediator and all handlers/behaviors from the specified assemblies
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register mediator as singleton (cache lives for app lifetime)
        services.AddSingleton<IMediate, Mediate>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register handlers and behaviors from all provided assemblies
        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
            RegisterBehaviors(services, assembly);
            RegisterValidators(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Convenience method to register from a type's assembly
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Type[] markerTypes)
    {
        var assemblies = markerTypes.Select(t => t.Assembly).Distinct().ToArray();
        return services.AddMediator(assemblies);
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces(), (type, iface) => new { Type = type, Interface = iface })
            .Where(x => x.Interface.IsGenericType && 
                       (x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                        x.Interface.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Type);
        }
    }

    private static void RegisterBehaviors(IServiceCollection services, Assembly assembly)
    {
        var behaviorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces(), (type, iface) => new { Type = type, Interface = iface })
            .Where(x => x.Interface.IsGenericType && 
                       (x.Interface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) ||
                        x.Interface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<>))) // Fixed: Added no-response behaviors
            .ToList();

        foreach (var behavior in behaviorTypes)
        {
            services.AddScoped(behavior.Interface, behavior.Type);
        }
    }

    private static void RegisterValidators(IServiceCollection services, Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces(), (type, iface) => new { Type = type, Interface = iface })
            .Where(x => x.Interface.IsGenericType && 
                       x.Interface.GetGenericTypeDefinition() == typeof(IValidator<>))
            .ToList();

        foreach (var validator in validatorTypes)
        {
            services.AddScoped(validator.Interface, validator.Type);
        }
    }
}