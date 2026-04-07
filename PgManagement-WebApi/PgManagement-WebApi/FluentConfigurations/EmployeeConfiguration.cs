using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.FluentConfigurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasKey(e => e.EmployeeId);

            builder.Property(e => e.EmployeeId)
                   .HasMaxLength(36);

            builder.HasOne(e => e.PG)
                   .WithMany()
                   .HasForeignKey(e => e.PgId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.EmployeeRole)
                   .WithMany()
                   .HasForeignKey(e => e.RoleCode)
                   .HasPrincipalKey(r => r.Code)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.SalaryHistories)
                   .WithOne(sh => sh.Employee)
                   .HasForeignKey(sh => sh.EmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.SalaryPayments)
                   .WithOne(sp => sp.Employee)
                   .HasForeignKey(sp => sp.EmployeeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
