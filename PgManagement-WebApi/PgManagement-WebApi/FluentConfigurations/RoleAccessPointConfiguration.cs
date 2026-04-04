using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class RoleAccessPointConfiguration : IEntityTypeConfiguration<RoleAccessPoint>
    {
        public void Configure(EntityTypeBuilder<RoleAccessPoint> builder)
        {
            builder.HasKey(rap => new { rap.RoleId, rap.AccessPointId });

            builder.HasOne(rap => rap.Role)
                .WithMany()
                .HasForeignKey(rap => rap.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rap => rap.AccessPoint)
                .WithMany(a => a.RoleAccessPoints)
                .HasForeignKey(rap => rap.AccessPointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
