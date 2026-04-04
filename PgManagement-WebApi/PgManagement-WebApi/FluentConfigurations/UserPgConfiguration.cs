using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class UserPgConfiguration : IEntityTypeConfiguration<UserPg>
    {
        public void Configure(EntityTypeBuilder<UserPg> builder)
        {
            builder.HasKey(up => new { up.UserId, up.PgId });

            builder.HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(up => up.PG)
                .WithMany()
                .HasForeignKey(up => up.PgId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
