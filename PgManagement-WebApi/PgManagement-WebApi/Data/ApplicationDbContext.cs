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

        public DbSet<Branch> Branches { get; set; }
        public DbSet<PG> PGs { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMode> PaymentModes { get; set; }
        public DbSet<UserPg> UserPgs { get; set; }
        public DbSet<TenantRoom> TenantRooms { get; set; }
        public DbSet<TenantRentHistory> TenantRentHistories { get; set; }
        public DbSet<RoomRentHistory> RoomRentHistories { get; set; }
        public DbSet<PaymentFrequency> PaymentFrequencies { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<ExpenseAuditLog> ExpenseAuditLogs { get; set; }
        public DbSet<Advance> Advances { get; set; }
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<AccessPoint> AccessPoints { get; set; }
        public DbSet<RoleAccessPoint> RoleAccessPoints { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<NotificationSettings> NotificationSettings { get; set; }
        public DbSet<ReportSubscription> ReportSubscriptions { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* ============================================================
               Branch → PG (one-to-many)
               ============================================================ */
            modelBuilder.Entity<PG>()
                .HasOne(p => p.Branch)
                .WithMany(b => b.PGs)
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

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
               UserPg no longer carries RoleId — roles are in AspNetUserRoles
               ============================================================ */

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

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentType)
                .WithMany()
                .HasForeignKey(p => p.PaymentTypeCode)
                .HasPrincipalKey(pt => pt.Code)
                .OnDelete(DeleteBehavior.Restrict);


            /* ============================================================
               Advance
               ============================================================ */

            modelBuilder.Entity<Advance>()
                .Property(a => a.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Advance>()
                .Property(a => a.DeductedAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Advance>()
                .HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Advance>()
                .HasOne(a => a.CreatedByUser)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Advance>()
                .HasOne(a => a.SettledByUser)
                .WithMany()
                .HasForeignKey(a => a.SettledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            /* ============================================================
   Booking
   ============================================================ */
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Tenant)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.PG)
                .WithMany()
                .HasForeignKey(b => b.PgId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .Property(b => b.AdvanceAmount)
                .HasPrecision(18, 2);


            /* ============================================================
               AccessPoint
               ============================================================ */
            modelBuilder.Entity<AccessPoint>()
                .HasIndex(a => a.Key)
                .IsUnique();

            /* ============================================================
               RoleAccessPoint (join table)
               ============================================================ */
            modelBuilder.Entity<RoleAccessPoint>()
                .HasKey(rap => new { rap.RoleId, rap.AccessPointId });

            modelBuilder.Entity<RoleAccessPoint>()
                .HasOne(rap => rap.Role)
                .WithMany()
                .HasForeignKey(rap => rap.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoleAccessPoint>()
                .HasOne(rap => rap.AccessPoint)
                .WithMany(a => a.RoleAccessPoints)
                .HasForeignKey(rap => rap.AccessPointId)
                .OnDelete(DeleteBehavior.Restrict);

            /* ============================================================
               RefreshToken
               ============================================================ */
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            /* ============================================================
               NotificationSettings → PG (one-to-one per PG)
               ============================================================ */
            modelBuilder.Entity<NotificationSettings>()
                .HasOne(ns => ns.PG)
                .WithMany()
                .HasForeignKey(ns => ns.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationSettings>()
                .HasIndex(ns => ns.PgId)
                .IsUnique();

            /* ============================================================
               ReportSubscription → PG + User
               ============================================================ */
            modelBuilder.Entity<ReportSubscription>()
                .HasOne(rs => rs.PG)
                .WithMany()
                .HasForeignKey(rs => rs.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReportSubscription>()
                .HasOne(rs => rs.User)
                .WithMany()
                .HasForeignKey(rs => rs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReportSubscription>()
                .HasIndex(rs => new { rs.PgId, rs.UserId, rs.ReportType })
                .IsUnique();

            /* ============================================================
               AuditEvent
               ============================================================ */
            modelBuilder.Entity<AuditEvent>()
                .HasOne(a => a.PG)
                .WithMany()
                .HasForeignKey(a => a.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AuditEvent>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditEvent>()
                .HasOne(a => a.ReviewedByUser)
                .WithMany()
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

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
