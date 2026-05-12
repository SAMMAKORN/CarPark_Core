using CarPark.Models;
using CarPark.Shared;
using CarPark.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CarPark.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var normalUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var operatorUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var seedCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Password).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.HasIndex(x => x.Username).IsUnique();

                entity.HasData(
                    new User
                    {
                        Id = adminUserId,
                        Username = "admin",
                        Password = "PBKDF2$SHA256$100000$CNMtqGjCwsaY4gv7R9CXhw==$lGxIuuvrpQU4rT2dzxg8Y7sbv2kmTK8mG2kMgrOUMc4=",
                        Name = "System Admin",
                        Role = Role.Admin,
                        MustChangePassword = true,
                        CreateAt = seedCreatedAt,
                        IsDeleted = false
                    },
                    new User
                    {
                        Id = normalUserId,
                        Username = "user",
                        Password = "PBKDF2$SHA256$100000$W6wNPlF5a8q79g6h3inyRQ==$0N6YuOugjBTtdFCNrIBjdkXyi9B7xVfx6a75yU147r4=",
                        Name = "Normal User",
                        Role = Role.User,
                        MustChangePassword = true,
                        CreateAt = seedCreatedAt,
                        IsDeleted = false
                    },
                    new User
                    {
                        Id = operatorUserId,
                        Username = "operator",
                        Password = "PBKDF2$SHA256$100000$ITDALgYVh6dezGnLr7jgFg==$Gu2S3x9mUOixGYSLAxZQqQUc1dU2xzIY2mUGT/aiYks=",
                        Name = "Parking Operator",
                        Role = Role.User,
                        MustChangePassword = true,
                        CreateAt = seedCreatedAt,
                        IsDeleted = false
                    });
            });

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(AppDbContext).GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(null, [modelBuilder]);
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted);
        }

        public override int SaveChanges()
        {
            throw new NotSupportedException("Synchronous SaveChanges() is not allowed. Use SaveChangesAsync() instead.");
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException("Synchronous SaveChanges() is not allowed. Use SaveChangesAsync() instead.");
        }

        public DbSet<ParkingTransaction> ParkingTransactions { get; set; }
        public DbSet<ParkingLot> ParkingLots { get; set; }
        public DbSet<ParkingRateRule> ParkingRateRules { get; set; }
        public DbSet<User> Users { get; set; }
    }
}