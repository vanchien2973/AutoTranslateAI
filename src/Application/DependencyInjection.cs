using Application.Behaviors;
using Application.Interfaces;
using Application.Publishing;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Multi-platform publish orchestration (used by the Worker's PublishConsumer).
        services.AddScoped<IPublishExecutor, PublishExecutor>();
        return services;
    }
}
