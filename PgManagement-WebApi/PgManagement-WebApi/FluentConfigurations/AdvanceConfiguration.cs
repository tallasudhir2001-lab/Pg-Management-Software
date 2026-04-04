using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class AdvanceConfiguration : IEntityTypeConfiguration<Advance>
    {
        public void Configure(EntityTypeBuilder<Advance> builder)
        {
            builder.Property(a => a.Amount)
                .HasPrecision(18, 2);

            builder.Property(a => a.DeductedAmount)
                .HasPrecision(18, 2);

            builder.HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.SettledByUser)
                .WithMany()
                .HasForeignKey(a => a.SettledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
