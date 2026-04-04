using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class TenantRentHistoryConfiguration : IEntityTypeConfiguration<TenantRentHistory>
    {
        public void Configure(EntityTypeBuilder<TenantRentHistory> builder)
        {
            builder.HasOne(trh => trh.Tenant)
                .WithMany()
                .HasForeignKey(trh => trh.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(trh => trh.RoomRentHistory)
                .WithMany()
                .HasForeignKey(trh => trh.RoomRentHistoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
