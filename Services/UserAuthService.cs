using CarPark.Data;
using CarPark.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class UserAuthService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<User?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = username.Trim();

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.Users
                .FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);

            if (user is null || !PasswordHashService.VerifyPassword(user.Password, password))
            {
                return null;
            }

            if (!PasswordHashService.IsHashed(user.Password))
            {
                user.Password = PasswordHashService.HashPassword(password);
                user.UpdateAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            currentUserContext.SignIn(user);
            return user;
        }

        public void Logout()
        {
            currentUserContext.SignOut();
        }

        public async Task ChangePasswordAsync(
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            if (!currentUserContext.IsAuthenticated || !currentUserContext.CurrentUserId.HasValue)
            {
                throw new InvalidOperationException("Please login first.");
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                throw new InvalidOperationException("Current password is required.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new InvalidOperationException("New password is required.");
            }

            if (newPassword.Length < 6)
            {
                throw new InvalidOperationException("New password must be at least 6 characters.");
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.Users
                .FirstOrDefaultAsync(x => x.Id == currentUserContext.CurrentUserId.Value, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            if (!PasswordHashService.VerifyPassword(user.Password, currentPassword))
            {
                throw new InvalidOperationException("Current password is invalid.");
            }

            if (PasswordHashService.VerifyPassword(user.Password, newPassword))
            {
                throw new InvalidOperationException("New password must be different from current password.");
            }

            user.Password = PasswordHashService.HashPassword(newPassword);
            user.MustChangePassword = false;
            user.UpdateBy = currentUserContext.CurrentUserId;
            user.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.SetMustChangePassword(false);
        }
    }
}