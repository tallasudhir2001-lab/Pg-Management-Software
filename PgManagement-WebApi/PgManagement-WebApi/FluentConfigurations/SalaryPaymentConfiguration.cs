using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class SalaryPaymentConfiguration : IEntityTypeConfiguration<SalaryPayment>
    {
        public void Configure(EntityTypeBuilder<SalaryPayment> builder)
        {
            builder.HasKey(sp => sp.SalaryPaymentId);

            builder.Property(sp => sp.SalaryPaymentId)
                   .HasMaxLength(36);

            builder.Property(sp => sp.Amount)
                   .HasPrecision(12, 2);

            builder.HasOne(sp => sp.PG)
                   .WithMany()
                   .HasForeignKey(sp => sp.PgId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sp => sp.Employee)
                   .WithMany(e => e.SalaryPayments)
                   .HasForeignKey(sp => sp.EmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sp => sp.PaymentMode)
                   .WithMany()
                   .HasForeignKey(sp => sp.PaymentModeCode)
                   .HasPrincipalKey(pm => pm.Code)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
