using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
    {
        public void Configure(EntityTypeBuilder<NotificationSettings> builder)
        {
            builder.HasOne(ns => ns.PG)
                .WithMany()
                .HasForeignKey(ns => ns.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(ns => ns.PgId)
                .IsUnique();
        }
    }
}
