using CarPark.Data;
using CarPark.Models;
using CarPark.Shared;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingRateRuleService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<ParkingLot?> GetParkingLotByIdAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parkingLotId, cancellationToken);
        }

        /// <summary>คืน rules ทั้งหมด (global + schedule) ใช้ cache ที่ login</summary>
        public async Task<List<ParkingRateRule>> GetActiveRulesByLotAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingRateRules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == parkingLotId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);
        }

        /// <summary>คืน rules ของ lot+schedule สำหรับแสดงในหน้าจัดการ</summary>
        public async Task<List<ParkingRateRule>> GetRulesByLotAsync(Guid parkingLotId, Guid? scheduleId = null, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingRateRules
                .AsNoTracking()
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
            };
            entity.SetCreated(currentUserContext.CurrentUserId);

            db.ParkingRateRules.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(rule.ParkingLotId);
            return entity;
        }

        public async Task UpdateAsync(ParkingRateRule rule, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingRateRules.FirstOrDefaultAsync(x => x.Id == rule.Id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบอัตราค่าบริการ");

            await ValidateAsync(db, rule, rule.Id, cancellationToken);

            existing.RuleName = rule.RuleName.Trim();
            existing.Sequence = rule.Sequence;
            existing.StartMinute = rule.StartMinute;
            existing.EndMinute = rule.EndMinute;
            existing.CalculationType = rule.CalculationType;
            existing.Amount = rule.Amount;
            existing.BillingStepMinutes = rule.BillingStepMinutes;
            existing.IsActive = rule.IsActive;
            existing.SetUpdated(currentUserContext.CurrentUserId);

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(existing.ParkingLotId);
        }

        public async Task CopyRulesFromLotAsync(Guid sourceLotId, Guid targetLotId, Guid? targetScheduleId = null, CancellationToken cancellationToken = default)
        {
            if (sourceLotId == targetLotId && targetScheduleId == null)
                throw new InvalidOperationException("ลานต้นทางและปลายทางต้องไม่เป็นลานเดียวกัน");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var sourceLotExists = await db.ParkingLots.AnyAsync(x => x.Id == sourceLotId, cancellationToken);
            if (!sourceLotExists) throw new InvalidOperationException("ไม่พบลานต้นทาง");

            var sourceRules = await db.ParkingRateRules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == sourceLotId && x.ParkingScheduleId == null)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);

            if (sourceRules.Count == 0)
                throw new InvalidOperationException("ลานต้นทางไม่มีอัตราค่าบริการทั่วไป");

            var maxSeq = await db.ParkingRateRules
                .Where(x => x.ParkingLotId == targetLotId && x.ParkingScheduleId == targetScheduleId)
                .MaxAsync(x => (int?)x.Sequence, cancellationToken) ?? 0;

            var userId = currentUserContext.CurrentUserId;
            for (var i = 0; i < sourceRules.Count; i++)
            {
                var src = sourceRules[i];
                var entity = new ParkingRateRule
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
                };
                entity.SetCreated(userId);
                db.ParkingRateRules.Add(entity);
            }

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(targetLotId);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingRateRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบอัตราค่าบริการ");

            existing.SetDeleted(currentUserContext.CurrentUserId);
            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(existing.ParkingLotId);
        }

        private static async Task ValidateAsync(AppDbContext db, ParkingRateRule rule, Guid? currentRuleId, CancellationToken cancellationToken)
        {
            if (rule.ParkingLotId == Guid.Empty)
                throw new InvalidOperationException("กรุณาระบุลานจอดรถ");

            if (!await db.ParkingLots.AnyAsync(x => x.Id == rule.ParkingLotId, cancellationToken))
                throw new InvalidOperationException("ไม่พบลานจอดรถ");

            if (string.IsNullOrWhiteSpace(rule.RuleName))
                throw new InvalidOperationException("กรุณากรอกชื่อกฎ");

            if (rule.Sequence <= 0)
                throw new InvalidOperationException("ลำดับต้องมากกว่า 0");

            if (rule.StartMinute < 0)
                throw new InvalidOperationException("นาทีเริ่มต้องไม่ติดลบ");

            if (rule.EndMinute.HasValue && rule.EndMinute.Value < rule.StartMinute)
                throw new InvalidOperationException("นาทีสิ้นสุดต้องมากกว่าหรือเท่ากับนาทีเริ่ม");

            if (rule.Amount < 0)
                throw new InvalidOperationException("จำนวนเงินต้องไม่ติดลบ");

            if (rule.BillingStepMinutes.HasValue && rule.BillingStepMinutes.Value <= 0)
                throw new InvalidOperationException("ช่วงการคิดเงินต้องมากกว่า 0");

            var sequenceExists = await db.ParkingRateRules.AnyAsync(
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
