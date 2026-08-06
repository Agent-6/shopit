using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Persistence.Inbox;

namespace ShopIt.Framework.Persistence.Inbox;

/// <summary>
/// Background service that consumes integration events from Kafka and dispatches them
/// to registered <see cref="IIntegrationEventHandler{TEvent}"/> implementations.
/// Implements the Inbox pattern to guarantee idempotent, at-least-once processing.
/// </summary>
/// <typeparam name="TContext">The EF Core <see cref="DbContext"/> type for the service.</typeparam>
public sealed class InboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxOptions> options,
    ILogger<InboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly InboxOptions _options = options.Value;
    private readonly ILogger<InboxProcessor<TContext>> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Topics.Count == 0)
        {
            _logger.LogWarning("InboxProcessor: no topics configured. Exiting.");
            return;
        }

        _logger.LogInformation(
            "InboxProcessor started. Subscribing to topics: {Topics}.",
            string.Join(", ", _options.Topics));

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.KafkaBootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,   // manual commit after successful processing
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_options.Topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(_options.ConsumeTimeoutMs));

                    if (result is null)
                        continue;

                    await ProcessMessageAsync(result.Message, stoppingToken);

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing Kafka message.");
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("InboxProcessor stopped.");
        }
    }

    private async Task ProcessMessageAsync(Message<string, string> message, CancellationToken cancellationToken)
    {
        // Extract event type from Kafka header
        var eventTypeHeader = message.Headers.FirstOrDefault(h => h.Key == "EventType");
        if (eventTypeHeader is null)
        {
            _logger.LogWarning("Received Kafka message without 'EventType' header. Skipping.");
            return;
        }

        var eventType = Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
        var eventId = Guid.TryParse(message.Key, out var id) ? id : Guid.NewGuid();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        // Idempotency check: skip if already processed
        var alreadyProcessed = await dbContext.Set<InboxMessage>()
            .AnyAsync(m => m.Id == eventId && m.ProcessedOn != null, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogDebug("Duplicate inbox message {EventType} (Id: {Id}). Skipping.", eventType, eventId);
            return;
        }

        // Upsert inbox record (mark as received)
        var inbox = await dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == eventId, cancellationToken)
            ?? new InboxMessage { Id = eventId, ReceivedOn = DateTime.UtcNow };

        inbox.EventType = eventType;
        inbox.Payload = message.Value;

        if (inbox.Id == eventId && !dbContext.Set<InboxMessage>().Local.Contains(inbox))
            dbContext.Set<InboxMessage>().Add(inbox);

        try
        {
            await DispatchToHandlersAsync(scope.ServiceProvider, eventType, message.Value, cancellationToken);

            inbox.ProcessedOn = DateTime.UtcNow;
            inbox.Error = null;

            _logger.LogDebug("Processed inbox message {EventType} (Id: {Id}).", eventType, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle inbox message {EventType} (Id: {Id}).", eventType, eventId);
            inbox.Error = ex.Message;
        }
        finally
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task DispatchToHandlersAsync(
        IServiceProvider serviceProvider,
        string eventTypeName,
        string payload,
        CancellationToken cancellationToken)
    {
        // Resolve all registered integration event types and find the matching one by name
        var eventType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == eventTypeName && t.IsAssignableTo(typeof(IntegrationEvent)));

        if (eventType is null)
            return; // No handler registered for this event type in this service

        var integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(payload, eventType, _jsonOptions);
        if (integrationEvent is null)
            return;

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;

            var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync))!;
            var task = (Task)method.Invoke(handler, [integrationEvent, cancellationToken])!;
            await task;
        }
    }
}
