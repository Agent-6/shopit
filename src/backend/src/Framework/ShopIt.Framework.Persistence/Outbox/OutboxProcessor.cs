using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShopIt.Framework.Persistence.Outbox;

namespace ShopIt.Framework.Persistence.Outbox;

/// <summary>
/// Background service that polls the <see cref="OutboxMessage"/> table and publishes
/// unprocessed messages to Kafka. Runs continuously with a configurable polling interval.
/// </summary>
/// <remarks>
/// This service implements the Transactional Outbox pattern. Domain changes and outbox rows
/// are committed in the same DB transaction; this service then reliably delivers them to Kafka.
/// </remarks>
public sealed class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly OutboxOptions _options = options.Value;
    private readonly ILogger<OutboxProcessor<TContext>> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started. Polling every {Interval}ms.", _options.PollingIntervalMs);

        var producerConfig = new ProducerConfig { BootstrapServers = _options.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(producer, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OutboxProcessor. Retrying after interval.");
            }

            await Task.Delay(_options.PollingIntervalMs, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessor stopped.");
    }

    private async Task ProcessBatchAsync(IProducer<string, string> producer, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("Processing {Count} outbox message(s).", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var kafkaMessage = new Message<string, string>
                {
                    Key = message.Id.ToString(),
                    Value = message.Payload,
                    Headers = new Headers
                    {
                        { "EventType", System.Text.Encoding.UTF8.GetBytes(message.EventType) },
                    },
                };

                var topic = _options.TopicResolver?.Invoke(message.EventType) ?? message.EventType;

                await producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;

                _logger.LogDebug(
                    "Published outbox message {EventType} (Id: {Id}) to topic '{Topic}'.",
                    message.EventType, message.Id, topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish outbox message {EventType} (Id: {Id}).",
                    message.EventType, message.Id);

                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
