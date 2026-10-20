using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Identity.Persistence.Data;
using Xunit;

namespace ShopIt.ArchitectureTests
{

/// <summary>
/// Guards the inbox's event-type registry: dispatch is a lookup of the message's discriminator in a map
/// built at startup, not a scan of whatever assemblies happen to be loaded.
/// </summary>
/// <remarks>
/// The scan it replaced matched on the <em>simple</em> type name, so two events named the same in
/// different namespaces collided — the winner chosen by assembly load order — and an unknown event was
/// silently skipped rather than reported. These tests pin the three properties that replaced it:
/// namespace-qualified names, load-order independence, and an error for the unknown.
/// </remarks>
public class InboxEventRegistryTests
{
    [Fact]
    public void Two_events_with_the_same_simple_name_resolve_to_their_own_types()
    {
        var registry = RegistryFor(
            InboxSubscription.Consuming(
                typeof(RegistryOne.CollidingTestEvent),
                typeof(RegistryTwo.CollidingTestEvent)));

        var firstName = InboxEventRegistry.EventTypeNameOf(typeof(RegistryOne.CollidingTestEvent));
        var secondName = InboxEventRegistry.EventTypeNameOf(typeof(RegistryTwo.CollidingTestEvent));

        // Same simple name, different wire names — which is precisely why the discriminator became
        // namespace-qualified.
        Assert.Equal(nameof(RegistryOne.CollidingTestEvent), nameof(RegistryTwo.CollidingTestEvent));
        Assert.NotEqual(firstName, secondName);

        Assert.True(registry.TryResolve(firstName, out var first));
        Assert.True(registry.TryResolve(secondName, out var second));
        Assert.Equal(typeof(RegistryOne.CollidingTestEvent), first);
        Assert.Equal(typeof(RegistryTwo.CollidingTestEvent), second);
    }

    [Fact]
    public void Resolution_does_not_depend_on_which_assemblies_are_loaded()
    {
        // The registry is a fact about the service's handlers, fixed at startup. Resolving twice — and
        // after the test assembly has been loaded, which is what the old scan searched — yields the same
        // mapping.
        var registry = RegistryFor(InboxSubscription.Consuming(typeof(RegistryOne.CollidingTestEvent)));

        _ = typeof(RegistryTwo.CollidingTestEvent).Assembly.GetTypes();

        Assert.True(registry.TryResolve(
            InboxEventRegistry.EventTypeNameOf(typeof(RegistryOne.CollidingTestEvent)),
            out var resolved));
        Assert.Equal(typeof(RegistryOne.CollidingTestEvent), resolved);
    }

    [Fact]
    public void An_event_the_service_has_no_handler_for_is_not_resolvable()
    {
        var registry = RegistryFor(InboxSubscription.Consuming(typeof(RegistryOne.CollidingTestEvent)));

        // The processor turns this into an InvalidOperationException naming the event and the consumer
        // group; it used to return quietly and drop the message.
        Assert.False(registry.TryResolve("Some.Event.Nobody.Handles", out var unresolved));
        Assert.Null(unresolved);
    }

    private static InboxEventRegistry RegistryFor(InboxSubscription inbox)
    {
        var services = new ServiceCollection();
        services.AddKafkaIntegration<ApplicationDbContext>(
            "registry-test-inbox",
            inbox,
            assemblies: [typeof(InboxEventRegistryTests).Assembly]);

        return services
            .Where(d => d.ServiceType == typeof(InboxEventRegistry))
            .Select(d => (InboxEventRegistry)d.ImplementationInstance!)
            .Single();
    }

    public sealed class CollidingTestEventOneHandler
        : IIntegrationEventHandler<RegistryOne.CollidingTestEvent>
    {
        public Task HandleAsync(RegistryOne.CollidingTestEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    public sealed class CollidingTestEventTwoHandler
        : IIntegrationEventHandler<RegistryTwo.CollidingTestEvent>
    {
        public Task HandleAsync(RegistryTwo.CollidingTestEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}

}

namespace ShopIt.ArchitectureTests.RegistryOne
{
    /// <summary>Deliberately shares its simple name with the event in the other namespace.</summary>
    public sealed record CollidingTestEvent : IntegrationEvent;
}

namespace ShopIt.ArchitectureTests.RegistryTwo
{
    /// <summary>Deliberately shares its simple name with the event in the other namespace.</summary>
    public sealed record CollidingTestEvent : IntegrationEvent;
}
