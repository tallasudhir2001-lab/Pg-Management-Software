using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class RoomRentHistoryConfiguration : IEntityTypeConfiguration<RoomRentHistory>
    {
        public void Configure(EntityTypeBuilder<RoomRentHistory> builder)
        {
            builder.HasOne(rrh => rrh.Room)
                .WithMany()
                .HasForeignKey(rrh => rrh.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(rrh => rrh.RentAmount)
                .HasPrecision(18, 2);
        }
    }
}
