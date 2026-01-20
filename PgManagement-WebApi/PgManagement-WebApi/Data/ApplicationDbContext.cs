using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Data
{
    public class ApplicationDbContext:IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<PG> PGs { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMode> PaymentModes { get; set; }
        public DbSet<UserPg> UserPgs { get; set; }
        public DbSet<PgRole> PgRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserPg>()
        .HasKey(up => new { up.UserId, up.PgId });

            modelBuilder.Entity<UserPg>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPg>()
                .HasOne(up => up.PG)
                .WithMany()
                .HasForeignKey(up => up.PgId);
            /*
             1. Pg -> tenants
             2. Pg -> pg -->rooms-tenants
             */
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Room)
                .WithMany(r => r.Tenants)
                .HasForeignKey(t => t.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.PG)
                .WithMany(p => p.Tenants)
                .HasForeignKey(t => t.PgId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.PG)
                .WithMany(p => p.Rooms)
                .HasForeignKey(r => r.PgId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .Property(r => r.RentAmount)
                .HasPrecision(18, 2);
        }
    }
}
