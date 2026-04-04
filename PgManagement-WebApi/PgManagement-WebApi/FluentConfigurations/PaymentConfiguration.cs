using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.Property(p => p.Amount)
                .HasPrecision(18, 2);

            builder.HasOne(p => p.Tenant)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.PG)
                .WithMany()
                .HasForeignKey(p => p.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.PaymentMode)
                .WithMany(pm => pm.Payments)
                .HasForeignKey(p => p.PaymentModeCode)
                .HasPrincipalKey(pm => pm.Code)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.DeletedByUser)
                .WithMany()
                .HasForeignKey(p => p.DeletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.PaymentType)
                .WithMany()
                .HasForeignKey(p => p.PaymentTypeCode)
                .HasPrincipalKey(pt => pt.Code)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
