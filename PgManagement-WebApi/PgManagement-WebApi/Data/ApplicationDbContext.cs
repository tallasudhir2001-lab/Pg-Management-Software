using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PgManagement_WebApi.FluentConfigurations;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;
using PgManagement_WebApi.Models.BaseAuditableEntity;
using PgManagement_WebApi.Services;
using System.Linq.Expressions;

namespace PgManagement_WebApi.Data
{
    public class ApplicationDbContext:IdentityDbContext<ApplicationUser>
    {
        private readonly ICurrentUserService _currentUser;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser) : base(options)
        {
            _currentUser = currentUser;
        }
        public override int SaveChanges()
        {
            HandleAuditing();
            HandleExpenseAuditLogs();
            return base.SaveChanges();
        }
        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            HandleAuditing();
            HandleExpenseAuditLogs();
            return base.SaveChangesAsync(cancellationToken);
        }

        public DbSet<PG> PGs { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMode> PaymentModes { get; set; }
        public DbSet<UserPg> UserPgs { get; set; }
        public DbSet<PgRole> PgRoles { get; set; }
        public DbSet<TenantRoom> TenantRooms { get; set; }
        public DbSet<TenantRentHistory> TenantRentHistories { get; set; }
        public DbSet<RoomRentHistory> RoomRentHistories { get; set; }
        public DbSet<PaymentFrequency> PaymentFrequencies { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<ExpenseAuditLog> ExpenseAuditLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* ============================================================
               User ↔ PG (many-to-many)
               ============================================================ */
            modelBuilder.Entity<UserPg>()
                .HasKey(up => new { up.UserId, up.PgId });

            modelBuilder.Entity<UserPg>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPg>()
                .HasOne(up => up.PG)
                .WithMany()
                .HasForeignKey(up => up.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            /* ============================================================
               Tenant → PG
               ============================================================ */
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.PG)
                .WithMany(p => p.Tenants)
                .HasForeignKey(t => t.PgId)
                .OnDelete(DeleteBehavior.Restrict);

            /* ============================================================
               Room → PG
               ============================================================ */
            modelBuilder.Entity<Room>()
                .HasOne(r => r.PG)
                .WithMany(p => p.Rooms)
                .HasForeignKey(r => r.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            /* ============================================================
               TenantRoom (Occupancy History)
               ============================================================ */
            modelBuilder.Entity<TenantRoom>()
                .HasOne(tr => tr.Tenant)
                .WithMany()
                .HasForeignKey(tr => tr.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TenantRoom>()
                .HasOne(tr => tr.Room)
                .WithMany()
                .HasForeignKey(tr => tr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            /* ============================================================
               RoomRentHistory (Pricing History)
               ============================================================ */
            modelBuilder.Entity<RoomRentHistory>()
                .HasOne(rrh => rrh.Room)
                .WithMany()
                .HasForeignKey(rrh => rrh.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomRentHistory>()
                .Property(rrh => rrh.RentAmount)
                .HasPrecision(18, 2);

            /* ============================================================
               TenantRentHistory (Rent Applicability History)
               ============================================================ */
            modelBuilder.Entity<TenantRentHistory>()
                .HasOne(trh => trh.Tenant)
                .WithMany()
                .HasForeignKey(trh => trh.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TenantRentHistory>()
                .HasOne(trh => trh.RoomRentHistory)
                .WithMany()
                .HasForeignKey(trh => trh.RoomRentHistoryId)
                .OnDelete(DeleteBehavior.Restrict);

            /* ============================================================
               Payments
               ============================================================ */
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Tenant)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // 🔒 prevents cascade path

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PG)
                .WithMany()
                .HasForeignKey(p => p.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentMode)
                .WithMany(pm => pm.Payments)
                .HasForeignKey(p => p.PaymentModeCode)
                .HasPrincipalKey(pm => pm.Code)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.DeletedByUser)
                .WithMany()
                .HasForeignKey(p => p.DeletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            /* ============================================================
               Tenant Financial Fields
               ============================================================ */
            modelBuilder.Entity<Tenant>()
                .Property(t => t.AdvanceAmount)
                .HasPrecision(18, 2);

            modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
            modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new ExpenseAuditLogConfiguration());

            // ============================================================
            // Global soft-delete filter
            // ============================================================
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
                    var condition = Expression.Equal(property, Expression.Constant(false));

                    var lambda = Expression.Lambda(condition, parameter);

                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(lambda);
                }
            }

        }
        private void HandleAuditing()
        {
            var entries = ChangeTracker.Entries<AuditableEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUser.UserId ?? "SYSTEM";
                    entry.Entity.IsDeleted = false;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = _currentUser.UserId ?? "SYSTEM";
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // 🔒 THIS IS THE KEY PART
                    entry.State = EntityState.Modified;

                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = _currentUser.UserId ?? "SYSTEM";
                }
            }
        }
        private void HandleExpenseAuditLogs()
        {
            var expenseEntries = ChangeTracker.Entries<Expense>()
                .Where(e =>
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in expenseEntries)
            {
                var action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => null
                };

                if (action == null)
                    continue;

                var auditLog = new ExpenseAuditLog
                {
                    Id = Guid.NewGuid().ToString(),
                    ExpenseId = entry.Entity.Id,
                    Action = action,
                    OldValue = entry.State == EntityState.Added
                        ? null
                        : SerializeValues(entry.OriginalValues),
                    NewValue = entry.State == EntityState.Deleted
                        ? null
                        : SerializeValues(entry.CurrentValues),
                    ChangedBy = _currentUser.UserId ?? "SYSTEM",
                    ChangedAt = DateTime.UtcNow
                };

                ExpenseAuditLogs.Add(auditLog);
            }
        }
        private static string SerializeValues(PropertyValues values)
        {
            var dict = values.Properties.ToDictionary(
                p => p.Name,
                p => values[p]?.ToString()
            );

            return System.Text.Json.JsonSerializer.Serialize(dict);
        }


    }
}
