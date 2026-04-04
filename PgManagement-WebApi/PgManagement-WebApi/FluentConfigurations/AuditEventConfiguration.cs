using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
    {
        public void Configure(EntityTypeBuilder<AuditEvent> builder)
        {
            builder.HasOne(a => a.PG)
                .WithMany()
                .HasForeignKey(a => a.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.ReviewedByUser)
                .WithMany()
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
