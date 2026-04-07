using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class EmployeeSalaryHistoryConfiguration : IEntityTypeConfiguration<EmployeeSalaryHistory>
    {
        public void Configure(EntityTypeBuilder<EmployeeSalaryHistory> builder)
        {
            builder.HasKey(sh => sh.EmployeeSalaryHistoryId);

            builder.Property(sh => sh.Amount)
                   .HasPrecision(12, 2);

            builder.HasOne(sh => sh.Employee)
                   .WithMany(e => e.SalaryHistories)
                   .HasForeignKey(sh => sh.EmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
