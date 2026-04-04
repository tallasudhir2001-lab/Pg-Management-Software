using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class AccessPointConfiguration : IEntityTypeConfiguration<AccessPoint>
    {
        public void Configure(EntityTypeBuilder<AccessPoint> builder)
        {
            builder.HasIndex(a => a.Key)
                .IsUnique();
        }
    }
}
