using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Identity.Persistence.Data;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards that an inbox which consumes nothing cannot be registered, and that one which subscribes to
/// a topic nobody handles fails at startup.
/// </summary>
/// <remarks>
/// <para>
/// Before this, a service with no inbox configuration registered an inbox processor that started,
/// logged a warning and exited. "Deliberately publish-only" and "someone forgot to configure it" were
/// indistinguishable — same log, same running system — and a subscribed topic with no handler looked
/// like a healthy service that simply never reacted.
/// </para>
/// <para>
/// The fix makes both situations explicit at registration: <see cref="InboxSubscription.None"/> or a
/// non-empty topic list whose topics all have handlers.
/// </para>
/// </remarks>
public class InboxSubscriptionTests
{
    private sealed record UnhandledTestEvent : IntegrationEvent;

    [Fact]
    public void A_consuming_inbox_requires_at_least_one_topic()
    {
        // This is the case that used to be expressible: configureInbox that adds nothing. It would
        // start an inbox, log a warning and exit.
        var exception = Assert.Throws<ArgumentException>(() => InboxSubscription.Consuming());

        Assert.Contains("publish-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_consuming_inbox_rejects_a_type_that_is_not_an_integration_event()
    {
        Assert.Throws<ArgumentException>(() => InboxSubscription.Consuming(typeof(string)));
    }

    [Fact]
    public void A_publish_only_service_registers_no_inbox_processor()
    {
        var services = new ServiceCollection();

        services.AddKafkaIntegration<ApplicationDbContext>("publish-only-inbox", InboxSubscription.None);

        Assert.DoesNotContain(services, descriptor =>
            descriptor.ImplementationType is { IsGenericType: true } type
            && type.GetGenericTypeDefinition() == typeof(InboxProcessor<>));

        // The outbox still runs: a publish-only service is not a silent inbox, it is a sender.
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType is { IsGenericType: true } type
            && type.GetGenericTypeDefinition() == typeof(OutboxProcessor<>));
    }

    [Fact]
    public void A_consuming_inbox_registers_the_processor_and_records_the_topics()
    {
        var services = new ServiceCollection();

        services.AddKafkaIntegration<ApplicationDbContext>(
            "consuming-inbox",
            InboxSubscription.Consuming(typeof(ConsumedTestEvent)),
            assemblies: [typeof(InboxSubscriptionTests).Assembly]);

        Assert.Contains(services, descriptor =>
            descriptor.ImplementationType is { IsGenericType: true } type
            && type.GetGenericTypeDefinition() == typeof(InboxProcessor<>));

        var options = services
            .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Options.IOptions<InboxOptions>))
            .Select(d => ((Microsoft.Extensions.Options.IOptions<InboxOptions>)d.ImplementationInstance!).Value)
            .Single();

        Assert.Equal("consuming-inbox", options.ConsumerGroupId);
        Assert.Equal([typeof(ConsumedTestEvent).FullName], options.Topics);
    }

    [Fact]
    public void Subscribing_to_a_topic_nobody_handles_fails_registration()
    {
        var services = new ServiceCollection();

        // The scanned assembly contains no handler for this event — the message would be consumed,
        // recorded and dropped.
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddKafkaIntegration<ApplicationDbContext>(
                "misconfigured-inbox",
                InboxSubscription.Consuming(typeof(UnhandledTestEvent)),
                assemblies: [typeof(InboxSubscriptionTests).Assembly]));

        Assert.Contains(nameof(UnhandledTestEvent), exception.Message);
        Assert.Contains("misconfigured-inbox", exception.Message);
    }

    public sealed record ConsumedTestEvent : IntegrationEvent;

    public sealed class ConsumedTestEventHandler : IIntegrationEventHandler<ConsumedTestEvent>
    {
        public Task HandleAsync(ConsumedTestEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
