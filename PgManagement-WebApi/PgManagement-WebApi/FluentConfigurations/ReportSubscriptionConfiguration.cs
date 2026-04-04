using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class ReportSubscriptionConfiguration : IEntityTypeConfiguration<ReportSubscription>
    {
        public void Configure(EntityTypeBuilder<ReportSubscription> builder)
        {
            builder.HasOne(rs => rs.PG)
                .WithMany()
                .HasForeignKey(rs => rs.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rs => rs.User)
                .WithMany()
                .HasForeignKey(rs => rs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rs => new { rs.PgId, rs.UserId, rs.ReportType })
                .IsUnique();
        }
    }
}
