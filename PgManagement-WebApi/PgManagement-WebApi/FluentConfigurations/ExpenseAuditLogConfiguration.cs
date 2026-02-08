using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PgManagement_WebApi.Models;

public class ExpenseAuditLogConfiguration : IEntityTypeConfiguration<ExpenseAuditLog>
{
    public void Configure(EntityTypeBuilder<ExpenseAuditLog> builder)
    {
        // Primary Key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasMaxLength(36)
               .IsRequired();

        // Properties
        builder.Property(x => x.Action)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(x => x.OldValue)
               .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValue)
               .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ChangedBy)
               .IsRequired()
               .HasMaxLength(36);

        builder.Property(x => x.ChangedAt)
               .HasDefaultValueSql("GETUTCDATE()")
               .IsRequired();

        // Indexes
        builder.HasIndex(x => x.ExpenseId);

        // Relationships
        builder.HasOne(x => x.Expense)
               .WithMany(e => e.AuditLogs)
               .HasForeignKey(x => x.ExpenseId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
