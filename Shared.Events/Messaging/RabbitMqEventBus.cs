using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Events.Messaging;

public class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private const string ExchangeName = "erp.events";
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqEventBus(IConfiguration configuration, ILogger<RabbitMqEventBus> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var eventName = typeof(T).Name;
        var routingKey = GetRoutingKey(eventName);

        try
        {
            await EnsureChannelAsync();

            if (_channel != null)
            {
                var payload = JsonSerializer.Serialize(@event);
                var body = Encoding.UTF8.GetBytes(payload);

                var props = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                    ContentType = "application/json",
                    Type = eventName
                };

                await _channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(" [x] Published Event '{EventName}' with RoutingKey '{RoutingKey}' to RabbitMQ", eventName, routingKey);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not publish event '{EventName}' to RabbitMQ. (RabbitMQ might not be running locally). Event details: {Event}", eventName, JsonSerializer.Serialize(@event));
        }
    }

    private async Task EnsureChannelAsync()
    {
        if (_channel != null && _channel.IsOpen)
        {
            return;
        }

        await _lock.WaitAsync();
        try
        {
            if (_channel != null && _channel.IsOpen)
            {
                return;
            }

            var hostName = _configuration["RabbitMQ:Host"] ?? "localhost";
            var userName = _configuration["RabbitMQ:Username"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";
            var portStr = _configuration["RabbitMQ:Port"];
            int port = int.TryParse(portStr, out var p) ? p : 5672;

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Port = port
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ connection could not be established at {Host}: {Message}", _configuration["RabbitMQ:Host"] ?? "localhost", ex.Message);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string GetRoutingKey(string eventName)
    {
        return eventName switch
        {
            "StockLowEvent" => "stock.low",
            "StockReceivedEvent" => "stock.received",
            "POCreatedEvent" => "po.created",
            "POApprovedEvent" => "po.approved",
            "PORejectedEvent" => "po.rejected",
            _ => eventName.ToLowerInvariant()
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
