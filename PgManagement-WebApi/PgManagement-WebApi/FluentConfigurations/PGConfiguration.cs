using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class PGConfiguration : IEntityTypeConfiguration<PG>
    {
        public void Configure(EntityTypeBuilder<PG> builder)
        {
            builder.HasOne(p => p.Branch)
                .WithMany(b => b.PGs)
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
