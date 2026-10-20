using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Persistence.Auditing;

/// <summary>
/// Stamps the framework's audit fields on entities that implement <see cref="IAuditedEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Audit data is a convention, not a decision each handler makes. Before this existed, entities were
/// created with whatever the calling code supplied — Identity stored the string <c>"system"</c>, and
/// <c>CreateTenantCommandHandler</c> wrote <c>CreatedBy = Guid.Empty</c> with a comment admitting it did
/// not know the actor. A handler that forgot produced data that silently claimed "nobody".
/// </para>
/// <para>
/// Both persistence paths call <see cref="Apply"/> immediately before saving: the command path
/// (<c>UnitOfWork.CommitAsync</c>) and the inbox path (<c>InboxProcessor</c>). A future path that saves
/// directly would bypass stamping — the same shape of risk as the cache-invalidation contract in
/// ADR-0002, and documented for the same reason.
/// </para>
/// </remarks>
public static class AuditInfo
{
    /// <summary>
    /// Applies creation and modification stamps to the added and modified entries tracked by
    /// <paramref name="dbContext"/>.
    /// </summary>
    /// <param name="dbContext">The context whose tracked entities are stamped.</param>
    /// <param name="actorId">
    /// The acting user, or <c>null</c> when there is none — which is what is stored, rather than a
    /// sentinel that happens to be a valid-looking id.
    /// </param>
    public static void Apply(DbContext dbContext, Guid? actorId)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var now = new DateTimeOffset(DomainProviders.Date.UtcNow, TimeSpan.Zero);

        foreach (var entry in dbContext.ChangeTracker.Entries<IAuditedEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                // An aggregate may record its own creation instant (so a command can report it before
                // the save); stamping only fills what is missing.
                if (entry.Entity.CreatedOn == default)
                {
                    entry.Entity.CreatedOn = now;
                }

                entry.Entity.CreatedBy = actorId;
                entry.Entity.LastModifiedOn = now;
                entry.Entity.LastModifiedBy = actorId;
            }
            else if (entry.State is EntityState.Modified)
            {
                // Creation stamps describe the row, not this change: never rewrite them.
                entry.Property(nameof(IAuditedEntity.CreatedOn)).IsModified = false;
                entry.Property(nameof(IAuditedEntity.CreatedBy)).IsModified = false;

                entry.Entity.LastModifiedOn = now;
                entry.Entity.LastModifiedBy = actorId;
            }
        }
    }
}
