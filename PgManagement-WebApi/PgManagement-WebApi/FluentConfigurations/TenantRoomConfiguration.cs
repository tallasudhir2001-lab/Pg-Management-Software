using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class TenantRoomConfiguration : IEntityTypeConfiguration<TenantRoom>
    {
        public void Configure(EntityTypeBuilder<TenantRoom> builder)
        {
            builder.HasOne(tr => tr.Tenant)
                .WithMany()
                .HasForeignKey(tr => tr.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(tr => tr.Room)
                .WithMany()
                .HasForeignKey(tr => tr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
