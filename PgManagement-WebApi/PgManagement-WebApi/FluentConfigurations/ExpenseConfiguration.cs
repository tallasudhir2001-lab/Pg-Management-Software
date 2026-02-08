using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasMaxLength(36);

            builder.Property(x => x.Amount)
                   .HasPrecision(12, 2);

            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne(x => x.Pg)
                   .WithMany()
                   .HasForeignKey(x => x.PgId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.PaymentMode)
                    .WithMany()
                    .HasForeignKey(e => e.PaymentModeCode)
                    .HasPrincipalKey(pm => pm.Code)
                    .OnDelete(DeleteBehavior.Restrict);

        }
    }

}
