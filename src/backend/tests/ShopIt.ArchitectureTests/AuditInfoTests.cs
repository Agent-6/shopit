using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Framework.Persistence.Auditing;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards the framework's audit convention: creation stamps are written once, modification stamps are
/// written on every change, and the actor is whatever the request says — or null when there is none.
/// </summary>
public class AuditInfoTests
{
    [Fact]
    public void Creating_an_entity_stamps_creation_and_modification_with_the_actor()
    {
        var actor = Guid.NewGuid();
        using var dbContext = NewContext();
        var entity = new AuditedTestEntity { Id = Guid.NewGuid(), Name = "first" };

        dbContext.Entities.Add(entity);
        AuditInfo.Apply(dbContext, actor);

        Assert.NotEqual(default, entity.CreatedOn);
        Assert.Equal(actor, entity.CreatedBy);
        Assert.Equal(entity.CreatedOn, entity.LastModifiedOn);
        Assert.Equal(actor, entity.LastModifiedBy);
    }

    [Fact]
    public void Modifying_an_entity_updates_modification_stamps_and_leaves_creation_stamps_alone()
    {
        using var dbContext = NewContext();
        var entity = new AuditedTestEntity { Id = Guid.NewGuid(), Name = "first" };
        var creator = Guid.NewGuid();

        dbContext.Entities.Add(entity);
        AuditInfo.Apply(dbContext, creator);
        dbContext.SaveChanges();

        var createdOn = entity.CreatedOn;
        var createdBy = entity.CreatedBy;
        var modifier = Guid.NewGuid();

        entity.Name = "second";
        AuditInfo.Apply(dbContext, modifier);

        // A later change does not rewrite who created the row.
        Assert.Equal(createdOn, entity.CreatedOn);
        Assert.Equal(createdBy, entity.CreatedBy);
        Assert.Equal(modifier, entity.LastModifiedBy);
        Assert.True(entity.LastModifiedOn >= createdOn);
    }

    [Fact]
    public void A_system_change_records_no_actor_rather_than_a_placeholder_id()
    {
        // Seeding, migrations and inbox handlers have no interactive user. Null says so; Guid.Empty
        // would look like a real actor with an all-zero id.
        using var dbContext = NewContext();
        var entity = new AuditedTestEntity { Id = Guid.NewGuid(), Name = "seeded" };

        dbContext.Entities.Add(entity);
        AuditInfo.Apply(dbContext, actorId: null);

        Assert.NotEqual(default, entity.CreatedOn);
        Assert.Null(entity.CreatedBy);
    }

    [Fact]
    public void Unstamped_entities_are_ignored()
    {
        using var dbContext = NewContext();
        var plain = new PlainTestEntity { Id = Guid.NewGuid() };

        dbContext.PlainEntities.Add(plain);
        AuditInfo.Apply(dbContext, Guid.NewGuid());

        Assert.Equal(EntityState.Added, dbContext.Entry(plain).State);
    }

    private static TestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"audit-{Guid.NewGuid()}")
            .Options;

        return new TestDbContext(options);
    }

    public sealed class AuditedTestEntity : IAuditedEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTimeOffset CreatedOn { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTimeOffset? LastModifiedOn { get; set; }

        public Guid? LastModifiedBy { get; set; }

        public DateTimeOffset? DeletedOn { get; set; }

        public Guid? DeletedBy { get; set; }
    }

    public sealed class PlainTestEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<AuditedTestEntity> Entities => Set<AuditedTestEntity>();

        public DbSet<PlainTestEntity> PlainEntities => Set<PlainTestEntity>();
    }
}
