using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasOne(b => b.Tenant)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Room)
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.PG)
                .WithMany()
                .HasForeignKey(b => b.PgId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(b => b.AdvanceAmount)
                .HasPrecision(18, 2);
        }
    }
}
