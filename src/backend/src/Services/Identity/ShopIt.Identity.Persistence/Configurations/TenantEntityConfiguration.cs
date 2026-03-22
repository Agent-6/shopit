using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Persistence.Configurations;

internal class TenantEntityConfiguration<TTenantEntity> : IEntityTypeConfiguration<TTenantEntity> where TTenantEntity : class, IEntity, ITenantEntity
{
    public void Configure(EntityTypeBuilder<TTenantEntity> builder) => builder
        .HasQueryFilter(e => e.TenantId == TenantContextAccessor.Current.CurrentTenantId);
}
