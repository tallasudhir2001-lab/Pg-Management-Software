using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

            // Auto-discover all IEntityTypeConfiguration<T> classes
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global soft-delete filter
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
