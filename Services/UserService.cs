using CarPark.Data;
using CarPark.Models;
using CarPark.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class UserService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.Users
                .OrderBy(x => x.Username)
                .ToListAsync(cancellationToken);
        }

        public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await ValidateUniqueUsernameAsync(db, user.Username, null, cancellationToken);

            var entity = new User
            {
                Username = user.Username.Trim(),
                Password = PasswordHashService.HashPassword(user.Password),
                Name = user.Name.Trim(),
                Role = user.Role,
                MustChangePassword = true,
                CreateBy = currentUserContext.CurrentUserId,
                CreateAt = DateTime.UtcNow
            };

            db.Users.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(User user, string? newPassword, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db.Users.FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            await ValidateUniqueUsernameAsync(db, user.Username, user.Id, cancellationToken);

            existing.Username = user.Username.Trim();
            existing.Name = user.Name.Trim();
            existing.Role = user.Role;
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existing.Password = PasswordHashService.HashPassword(newPassword);
                existing.MustChangePassword = true;
            }

            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            if (existing.Role == Role.Admin)
            {
                var adminCount = await db.Users.CountAsync(x => x.Role == Role.Admin, cancellationToken);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Cannot delete the last admin user.");
                }
            }

            existing.IsDeleted = true;
            existing.DeletedBy = currentUserContext.CurrentUserId;
            existing.DeleteAt = DateTime.UtcNow;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task ValidateUniqueUsernameAsync(
            AppDbContext db,
            string username,
            Guid? currentId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Username is required.");
            }

            var normalizedUsername = username.Trim();
            var exists = await db.Users
                .AnyAsync(
                    x => x.Username == normalizedUsername
                         && (!currentId.HasValue || x.Id != currentId.Value),
                    cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("Username already exists.");
            }
        }
    }
}
