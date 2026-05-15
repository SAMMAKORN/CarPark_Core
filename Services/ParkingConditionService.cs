using CarPark.Data;
using CarPark.Models;
using CarPark.Shared;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingConditionService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<ParkingCondition>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingConditions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ConditionLots)
                .OrderBy(x => x.ConditionName)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ParkingCondition>> GetApplicableAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingConditions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ConditionLots)
                .Where(x => x.IsActive && x.ConditionLots.Any(cl => cl.ParkingLotId == lotId))
                .OrderBy(x => x.ConditionName)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// นับจำนวนรถที่ใช้เงื่อนไขนี้ในลานนี้ในวันทำการปัจจุบัน
        /// (นับจาก operatingDayStartUtc ถึงปัจจุบัน)
        /// </summary>
        public async Task<int> GetQuotaUsedTodayAsync(
            Guid conditionId,
            Guid lotId,
            TimeSpan openTime,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var nowLocal = DateTime.Now;
            var todayOpen = nowLocal.Date + openTime;
            var operatingDayStart = todayOpen > nowLocal ? todayOpen.AddDays(-1) : todayOpen;
            var operatingDayStartUtc = operatingDayStart.ToUniversalTime();

            return await db.ParkingTransactions
                .AsNoTracking()
                .CountAsync(x =>
                    x.ParkingConditionId == conditionId
                    && x.ParkingLotId == lotId
                    && x.OutAt.HasValue
                    && x.OutAt >= operatingDayStartUtc,
                    cancellationToken);
        }

        public async Task<ParkingCondition> CreateAsync(ParkingCondition condition, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var entity = new ParkingCondition
            {
                ConditionName = condition.ConditionName.Trim(),
                ConditionType = condition.ConditionType,
                FreeMinutes = condition.FreeMinutes,
                QuotaPerDay = condition.QuotaPerDay,
                WaiveOvernightPenalty = condition.WaiveOvernightPenalty,
                Notes = string.IsNullOrWhiteSpace(condition.Notes) ? null : condition.Notes.Trim(),
                IsActive = condition.IsActive,
            };
            entity.SetCreated(currentUserContext.CurrentUserId);

            db.ParkingConditions.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(ParkingCondition condition, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingConditions
                .FirstOrDefaultAsync(x => x.Id == condition.Id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบเงื่อนไขที่ต้องการแก้ไข");

            existing.ConditionName = condition.ConditionName.Trim();
            existing.ConditionType = condition.ConditionType;
            existing.FreeMinutes = condition.FreeMinutes;
            existing.QuotaPerDay = condition.QuotaPerDay;
            existing.WaiveOvernightPenalty = condition.WaiveOvernightPenalty;
            existing.Notes = string.IsNullOrWhiteSpace(condition.Notes) ? null : condition.Notes.Trim();
            existing.IsActive = condition.IsActive;
            existing.SetUpdated(currentUserContext.CurrentUserId);

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<HashSet<Guid>> GetLinkedConditionIdsAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var ids = await db.ParkingConditionLots
                .Where(x => x.ParkingLotId == lotId)
                .Select(x => x.ParkingConditionId)
                .ToListAsync(cancellationToken);
            return [.. ids];
        }

        public async Task UpdateLotConditionsAsync(Guid lotId, IEnumerable<Guid> conditionIds, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db.ParkingConditionLots
                .Where(x => x.ParkingLotId == lotId)
                .ToListAsync(cancellationToken);
            db.ParkingConditionLots.RemoveRange(existing);
            foreach (var condId in conditionIds)
                db.ParkingConditionLots.Add(new ParkingConditionLot { ParkingConditionId = condId, ParkingLotId = lotId });
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingConditions
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบเงื่อนไขที่ต้องการลบ");

            existing.SetDeleted(currentUserContext.CurrentUserId);

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}