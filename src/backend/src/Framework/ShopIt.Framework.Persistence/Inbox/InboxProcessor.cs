using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Persistence.Events;

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
    InboxEventRegistry eventRegistry,
    ILogger<InboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly InboxOptions _options = options.Value;
    private readonly InboxEventRegistry _eventRegistry = eventRegistry;
    private readonly ILogger<InboxProcessor<TContext>> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Unreachable through AddKafkaIntegration: the processor is only registered when a service
        // declared InboxSubscription.Consuming, which rejects an empty topic list. Kept as a guard so a
        // future registration path cannot produce an inbox that starts and quietly consumes nothing.
        if (_options.Topics.Count == 0)
        {
            throw new InvalidOperationException(
                $"The inbox for consumer group '{_options.ConsumerGroupId}' was started with no topics. " +
                "Register it with InboxSubscription.Consuming(...) instead.");
        }

        _logger.LogInformation(
            "InboxProcessor started. Consumer group '{ConsumerGroupId}', subscribing to topics: {Topics}.",
            _options.ConsumerGroupId,
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

        try
        {
            await ProcessInTransactionAsync(eventId, eventType, message.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle inbox message {EventType} (Id: {Id}).", eventType, eventId);

            // Record the failure on its own, in a fresh scope: the rolled-back transaction took the
            // handler's partial writes with it, so what is persisted is only the fact that this message
            // failed — not half of its effects.
            await RecordFailureAsync(eventId, eventType, message.Value, ex, cancellationToken);
        }
    }

    /// <summary>
    /// Handles one message atomically: the handler's writes, the outbox rows its domain-event handlers
    /// add, and the inbox row commit together or not at all.
    /// </summary>
    /// <remarks>
    /// The work runs inside <c>CreateExecutionStrategy()</c> because the PostgreSQL provider is configured
    /// with <c>EnableRetryOnFailure</c>, and a user-initiated transaction is rejected outright unless it is
    /// executed as a retriable unit. Each attempt gets its own scope, so a retry starts from a clean
    /// <c>DbContext</c> rather than from the failed attempt's tracked changes.
    /// </remarks>
    private async Task ProcessInTransactionAsync(
        Guid eventId,
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        // The strategy is obtained from a context; the delegate below resolves its own per attempt.
        await using var strategyScope = _scopeFactory.CreateAsyncScope();
        var strategy = strategyScope.ServiceProvider
            .GetRequiredService<TContext>()
            .Database
            .CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var domainEventDispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

            // Idempotency check: skip if already processed
            var alreadyProcessed = await dbContext.Set<InboxMessage>()
                .AnyAsync(m => m.Id == eventId && m.ProcessedOn != null, cancellationToken);

            if (alreadyProcessed)
            {
                _logger.LogDebug("Duplicate inbox message {EventType} (Id: {Id}). Skipping.", eventType, eventId);
                return;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Upsert the inbox record (mark as received) inside the transaction.
                var inbox = await dbContext.Set<InboxMessage>()
                    .FirstOrDefaultAsync(m => m.Id == eventId, cancellationToken)
                    ?? new InboxMessage { Id = eventId, ReceivedOn = DateTime.UtcNow };

                inbox.EventType = eventType;
                inbox.Payload = payload;

                if (inbox.Id == eventId && !dbContext.Set<InboxMessage>().Local.Contains(inbox))
                    dbContext.Set<InboxMessage>().Add(inbox);

                await DispatchToHandlersAsync(scope.ServiceProvider, eventType, payload, cancellationToken);

                // The same dispatch the command path uses: an inbox handler that raises a domain event —
                // User.ConfirmEmail() does — now gets its handlers run instead of having it discarded.
                var domainEvents = await DomainEventDispatch.DispatchPendingAsync(
                    dbContext, domainEventDispatcher, cancellationToken);

                if (domainEvents.Count > 0)
                {
                    _logger.LogInformation(
                        "Dispatched {Count} domain event(s) raised by inbox message {EventType} (Id: {Id}): {DomainEvents}.",
                        domainEvents.Count,
                        eventType,
                        eventId,
                        string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
                }

                inbox.ProcessedOn = DateTime.UtcNow;
                inbox.Error = null;

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogDebug("Processed inbox message {EventType} (Id: {Id}).", eventType, eventId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Persists the inbox row for a message whose handling failed.
    /// </summary>
    private async Task RecordFailureAsync(
        Guid eventId,
        string eventType,
        string payload,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var inbox = await dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == eventId, cancellationToken)
            ?? new InboxMessage { Id = eventId };

        if (inbox.ReceivedOn == default)
        {
            inbox.ReceivedOn = DateTime.UtcNow;
        }

        inbox.EventType = eventType;
        inbox.Payload = payload;
        inbox.Error = exception.Message;
        inbox.ProcessedOn = null;

        if (!dbContext.Set<InboxMessage>().Local.Contains(inbox))
        {
            dbContext.Set<InboxMessage>().Add(inbox);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchToHandlersAsync(
        IServiceProvider serviceProvider,
        string eventTypeName,
        string payload,
        CancellationToken cancellationToken)
    {
        // The registry holds the events this service registered handlers for, keyed by the same
        // namespace-qualified name the outbox wrote. An unknown name is an error, not something to skip
        // quietly: it means a producer sent an event this service was supposed to handle, or the service
        // subscribes to a topic whose handler was never registered.
        if (!_eventRegistry.TryResolve(eventTypeName, out var eventType) || eventType is null)
        {
            throw new InvalidOperationException(
                $"No handler is registered for event '{eventTypeName}' in consumer group " +
                $"'{_options.ConsumerGroupId}'. Known events: " +
                $"{string.Join(", ", _eventRegistry.EventTypes.Select(t => t.FullName))}.");
        }

        var integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(payload, eventType, _jsonOptions)
            ?? throw new InvalidOperationException(
                $"Could not deserialize event '{eventTypeName}' into '{eventType.FullName}'.");

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
