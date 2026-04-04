using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasOne(t => t.PG)
                .WithMany(p => p.Tenants)
                .HasForeignKey(t => t.PgId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
