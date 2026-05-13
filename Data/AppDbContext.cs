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
            var seedCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<ParkingLot>(entity =>
            {
                entity.Property(x => x.OpenTime).HasColumnType("time");
                entity.Property(x => x.CloseTime).HasColumnType("time");
            });

            modelBuilder.Entity<ParkingGate>(entity =>
            {
                entity.ToTable("ParkingGates");
                entity.Property(x => x.GateName).HasMaxLength(100).IsRequired();
                entity.HasOne(x => x.ParkingLot)
                    .WithMany(x => x.Gates)
                    .HasForeignKey(x => x.ParkingLotId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ParkingTransaction>(entity =>
            {
                entity.HasOne(x => x.InGate)
                    .WithMany()
                    .HasForeignKey(x => x.InGateId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.OutGate)
                    .WithMany()
                    .HasForeignKey(x => x.OutGateId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

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
        public DbSet<ParkingGate> ParkingGates { get; set; }
        public DbSet<ParkingRateRule> ParkingRateRules { get; set; }
        public DbSet<User> Users { get; set; }
    }
}