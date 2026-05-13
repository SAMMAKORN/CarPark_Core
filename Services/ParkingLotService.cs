using CarPark.Data;
using CarPark.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingLotService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<ParkingLot>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLots
                .OrderBy(x => x.CreateAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ParkingLot> CreateAsync(ParkingLot lot, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var entity = new ParkingLot
            {
                LotCode = lot.LotCode.Trim(),
                LotName = lot.LotName.Trim(),
                IsAllDay = lot.IsAllDay,
                OpenTime = lot.OpenTime,
                CloseTime = lot.CloseTime,
                IsActive = lot.IsActive,
                CreateBy = currentUserContext.CurrentUserId,
                CreateAt = DateTime.UtcNow
            };

            db.ParkingLots.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(ParkingLot lot, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == lot.Id, cancellationToken)
                ?? throw new InvalidOperationException("Parking lot not found.");

            existing.LotCode = lot.LotCode.Trim();
            existing.LotName = lot.LotName.Trim();
            existing.IsAllDay = lot.IsAllDay;
            existing.OpenTime = lot.OpenTime;
            existing.CloseTime = lot.CloseTime;
            existing.IsActive = lot.IsActive;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Parking lot not found.");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.DeletedBy = currentUserContext.CurrentUserId;
            existing.DeleteAt = DateTime.UtcNow;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}