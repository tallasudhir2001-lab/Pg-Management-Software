using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.HasOne(r => r.PG)
                .WithMany(p => p.Rooms)
                .HasForeignKey(r => r.PgId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
