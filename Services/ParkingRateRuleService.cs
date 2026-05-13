using CarPark.Data;
using CarPark.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingRateRuleService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<ParkingLot>> GetParkingLotsAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLots
                .OrderBy(x => x.LotCode)
                .ToListAsync(cancellationToken);
        }

        public async Task<ParkingLot?> GetParkingLotByIdAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLots
                .FirstOrDefaultAsync(x => x.Id == parkingLotId, cancellationToken);
        }

        /// <summary>คืน rules ทั้งหมด (global + schedule) ใช้ cache ที่ login</summary>
        public async Task<List<ParkingRateRule>> GetActiveRulesByLotAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingRateRules
                .Where(x => x.ParkingLotId == parkingLotId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);
        }

        /// <summary>คืน rules ของ lot+schedule สำหรับแสดงในหน้าจัดการ</summary>
        public async Task<List<ParkingRateRule>> GetRulesByLotAsync(Guid parkingLotId, Guid? scheduleId = null, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingRateRules
                .Where(x => x.ParkingLotId == parkingLotId && x.ParkingScheduleId == scheduleId)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);
        }

        public async Task<ParkingRateRule> CreateAsync(ParkingRateRule rule, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            await ValidateAsync(db, rule, null, cancellationToken);

            var entity = new ParkingRateRule
            {
                ParkingLotId = rule.ParkingLotId,
                ParkingScheduleId = rule.ParkingScheduleId,
                RuleName = rule.RuleName.Trim(),
                Sequence = rule.Sequence,
                StartMinute = rule.StartMinute,
                EndMinute = rule.EndMinute,
                CalculationType = rule.CalculationType,
                Amount = rule.Amount,
                BillingStepMinutes = rule.BillingStepMinutes,
                IsActive = rule.IsActive,
                CreateBy = currentUserContext.CurrentUserId,
                CreateAt = DateTime.UtcNow
            };

            db.ParkingRateRules.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(rule.ParkingLotId);
            return entity;
        }

        public async Task UpdateAsync(ParkingRateRule rule, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingRateRules.FirstOrDefaultAsync(x => x.Id == rule.Id, cancellationToken)
                ?? throw new InvalidOperationException("Parking rate rule not found.");

            await ValidateAsync(db, rule, rule.Id, cancellationToken);

            existing.RuleName = rule.RuleName.Trim();
            existing.Sequence = rule.Sequence;
            existing.StartMinute = rule.StartMinute;
            existing.EndMinute = rule.EndMinute;
            existing.CalculationType = rule.CalculationType;
            existing.Amount = rule.Amount;
            existing.BillingStepMinutes = rule.BillingStepMinutes;
            existing.IsActive = rule.IsActive;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(existing.ParkingLotId);
        }

        /// <summary>Copy global rules (ParkingScheduleId = null) จาก lot ต้นทางไปยังปลายทาง</summary>
        public async Task CopyRulesFromLotAsync(Guid sourceLotId, Guid targetLotId, Guid? targetScheduleId = null, CancellationToken cancellationToken = default)
        {
            if (sourceLotId == targetLotId && targetScheduleId == null)
                throw new InvalidOperationException("ลานต้นทางและปลายทางต้องไม่เป็นลานเดียวกัน");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var sourceLotExists = await db.ParkingLots.AnyAsync(x => x.Id == sourceLotId, cancellationToken);
            if (!sourceLotExists)
                throw new InvalidOperationException("ไม่พบลานต้นทาง");

            // Copy only global rules from source lot
            var sourceRules = await db.ParkingRateRules
                .Where(x => x.ParkingLotId == sourceLotId && x.ParkingScheduleId == null)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);

            if (sourceRules.Count == 0)
                throw new InvalidOperationException("ลานต้นทางไม่มีอัตราค่าบริการทั่วไป");

            var maxSeq = await db.ParkingRateRules
                .Where(x => x.ParkingLotId == targetLotId && x.ParkingScheduleId == targetScheduleId)
                .MaxAsync(x => (int?)x.Sequence, cancellationToken) ?? 0;

            var now = DateTime.UtcNow;
            for (var i = 0; i < sourceRules.Count; i++)
            {
                var src = sourceRules[i];
                db.ParkingRateRules.Add(new ParkingRateRule
                {
                    ParkingLotId = targetLotId,
                    ParkingScheduleId = targetScheduleId,
                    RuleName = src.RuleName,
                    Sequence = maxSeq + i + 1,
                    StartMinute = src.StartMinute,
                    EndMinute = src.EndMinute,
                    CalculationType = src.CalculationType,
                    Amount = src.Amount,
                    BillingStepMinutes = src.BillingStepMinutes,
                    IsActive = src.IsActive,
                    CreateBy = currentUserContext.CurrentUserId,
                    CreateAt = now
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(targetLotId);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingRateRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Parking rate rule not found.");

            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.DeletedBy = currentUserContext.CurrentUserId;
            existing.DeleteAt = DateTime.UtcNow;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(existing.ParkingLotId);
        }

        // ─── Private ────────────────────────────────────────────────────

        private static async Task ValidateAsync(AppDbContext db, ParkingRateRule rule, Guid? currentRuleId, CancellationToken cancellationToken)
        {
            if (rule.ParkingLotId == Guid.Empty)
                throw new InvalidOperationException("Parking lot is required.");

            var lotExists = await db.ParkingLots.AnyAsync(x => x.Id == rule.ParkingLotId, cancellationToken);
            if (!lotExists)
                throw new InvalidOperationException("Parking lot not found.");

            if (string.IsNullOrWhiteSpace(rule.RuleName))
                throw new InvalidOperationException("Rule name is required.");

            if (rule.Sequence <= 0)
                throw new InvalidOperationException("Sequence must be greater than 0.");

            if (rule.StartMinute < 0)
                throw new InvalidOperationException("Start minute must be 0 or greater.");

            if (rule.EndMinute.HasValue && rule.EndMinute.Value < rule.StartMinute)
                throw new InvalidOperationException("End minute must be greater than or equal to start minute.");

            if (rule.Amount < 0)
                throw new InvalidOperationException("Amount must be 0 or greater.");

            if (rule.BillingStepMinutes.HasValue && rule.BillingStepMinutes.Value <= 0)
                throw new InvalidOperationException("Billing step minutes must be greater than 0.");

            // Sequence unique within same (lot, schedule) group
            var sequenceExists = await db.ParkingRateRules
                .AnyAsync(
                    x => x.ParkingLotId == rule.ParkingLotId
                         && x.ParkingScheduleId == rule.ParkingScheduleId
                         && x.Sequence == rule.Sequence
                         && (!currentRuleId.HasValue || x.Id != currentRuleId.Value),
                    cancellationToken);

            if (sequenceExists)
                throw new InvalidOperationException("ลำดับนี้มีอยู่แล้วในกลุ่มนี้");
        }
    }
}