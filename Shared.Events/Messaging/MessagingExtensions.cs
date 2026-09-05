using Microsoft.Extensions.DependencyInjection;

namespace Shared.Events.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }
}
