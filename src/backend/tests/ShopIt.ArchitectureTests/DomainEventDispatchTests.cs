using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Persistence.Events;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards that domain events are collected and dispatched the same way from both persistence paths.
/// </summary>
/// <remarks>
/// <para>
/// The command path (<c>UnitOfWork.CommitAsync</c>) collected domain events from tracked aggregates and
/// dispatched them; the inbox path saved directly and did not, so any event an inbox handler raised was
/// discarded. That is not visible in behaviour until something relies on such an event — which is
/// precisely what made it worth fixing before a handler existed.
/// </para>
/// <para>
/// Both paths now call <see cref="DomainEventDispatch.DispatchPendingAsync"/>, so these tests pin the
/// helper's contract: collect from tracked aggregates, clear, dispatch, return.
/// </para>
/// </remarks>
public class DomainEventDispatchTests
{
    [Fact]
    public async Task Pending_domain_events_are_dispatched_and_cleared()
    {
        await using var dbContext = NewContext();
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };
        aggregate.Raise(new TestDomainEvent(Guid.NewGuid()));

        dbContext.Aggregates.Add(aggregate);
        await dbContext.SaveChangesAsync();

        var dispatcher = new RecordingDispatcher();

        var dispatched = await DomainEventDispatch.DispatchPendingAsync(dbContext, dispatcher);

        Assert.Single(dispatched);
        Assert.Single(dispatcher.Dispatched);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public async Task An_aggregate_with_no_events_dispatches_nothing()
    {
        await using var dbContext = NewContext();
        dbContext.Aggregates.Add(new TestAggregate { Id = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var dispatcher = new RecordingDispatcher();

        var dispatched = await DomainEventDispatch.DispatchPendingAsync(dbContext, dispatcher);

        Assert.Empty(dispatched);
        Assert.Empty(dispatcher.Dispatched);
    }

    [Fact]
    public async Task Events_are_cleared_before_dispatch_so_a_failing_handler_cannot_replay_them()
    {
        await using var dbContext = NewContext();
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };
        aggregate.Raise(new TestDomainEvent(Guid.NewGuid()));

        dbContext.Aggregates.Add(aggregate);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DomainEventDispatch.DispatchPendingAsync(dbContext, new ThrowingDispatcher()));

        // The transaction decides whether the effects survive; the aggregate does not offer the event
        // again, so a retry cannot double-apply it.
        Assert.Empty(aggregate.DomainEvents);
    }

    private static TestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"domain-events-{Guid.NewGuid()}")
            .Options;

        return new TestDbContext(options);
    }

    public sealed record TestDomainEvent(Guid Id) : DomainEvent;

    public sealed class TestAggregate : IAggregateRoot
    {
        public Guid Id { get; set; }

        private readonly List<DomainEvent> _domainEvents = [];

        public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

        public void Raise(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvents() => _domainEvents.Clear();
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>().Ignore(aggregate => aggregate.DomainEvents);
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class RecordingDispatcher : IDomainEventDispatcher
    {
        public List<DomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(events);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("handler failed");
    }
}
