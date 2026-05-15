using CarPark.Data;
using CarPark.Models;
using CarPark.Shared;
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
                .AsNoTracking()
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
                BillingStartTime = lot.BillingStartTime,
                BillingEndTime = lot.BillingEndTime,
                HasOvernightPenalty = lot.HasOvernightPenalty,
                OvernightPenaltyAmount = lot.OvernightPenaltyAmount,
                IsActive = lot.IsActive,
            };
            entity.SetCreated(currentUserContext.CurrentUserId);

            db.ParkingLots.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(ParkingLot lot, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == lot.Id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบลานจอดรถ");

            existing.LotCode = lot.LotCode.Trim();
            existing.LotName = lot.LotName.Trim();
            existing.IsAllDay = lot.IsAllDay;
            existing.OpenTime = lot.OpenTime;
            existing.CloseTime = lot.CloseTime;
            existing.BillingStartTime = lot.BillingStartTime;
            existing.BillingEndTime = lot.BillingEndTime;
            existing.HasOvernightPenalty = lot.HasOvernightPenalty;
            existing.OvernightPenaltyAmount = lot.OvernightPenaltyAmount;
            existing.IsActive = lot.IsActive;
            existing.SetUpdated(currentUserContext.CurrentUserId);

            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// คัดลอกการตั้งค่าลาน + ตารางเวลา + อัตราค่าบริการทั้งหมดจากลานต้นทางไปยังลานปลายทาง
        /// ข้อมูลเดิมของลานปลายทาง (schedules + rules) จะถูก soft-delete แล้วแทนที่ด้วยของต้นทาง
        /// </summary>
        public async Task CopyFromLotAsync(Guid sourceLotId, Guid targetLotId, CancellationToken cancellationToken = default)
        {
            if (sourceLotId == targetLotId)
                throw new InvalidOperationException("ต้นทางและปลายทางต้องไม่ใช่ลานเดียวกัน");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var source = await db.ParkingLots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sourceLotId, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบลานต้นทาง");
            var target = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == targetLotId, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบลานปลายทาง");

            var userId = currentUserContext.CurrentUserId;

            // 1. คัดลอกการตั้งค่าลาน
            target.IsAllDay = source.IsAllDay;
            target.OpenTime = source.OpenTime;
            target.CloseTime = source.CloseTime;
            target.BillingStartTime = source.BillingStartTime;
            target.BillingEndTime = source.BillingEndTime;
            target.HasOvernightPenalty = source.HasOvernightPenalty;
            target.OvernightPenaltyAmount = source.OvernightPenaltyAmount;
            target.SetUpdated(userId);

            // 2. Soft-delete schedules + rules เดิมของปลายทาง (โหลดครั้งเดียว)
            var targetSchedules = await db.ParkingLotSchedules
                .Where(x => x.ParkingLotId == targetLotId)
                .ToListAsync(cancellationToken);
            var targetScheduleIds = targetSchedules.Select(s => s.Id).ToList();

            var targetRules = await db.ParkingRateRules
                .Where(x => x.ParkingLotId == targetLotId
                    && (x.ParkingScheduleId == null || targetScheduleIds.Contains(x.ParkingScheduleId.Value)))
                .ToListAsync(cancellationToken);

            foreach (var r in targetRules) r.SetDeleted(userId);
            foreach (var s in targetSchedules) s.SetDeleted(userId);

            // 3. คัดลอก schedules + rules จากต้นทาง (โหลดครั้งเดียว ไม่มี N+1)
            var sourceSchedules = await db.ParkingLotSchedules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == sourceLotId)
                .ToListAsync(cancellationToken);
            var sourceScheduleIds = sourceSchedules.Select(s => s.Id).ToList();

            var allSourceRules = await db.ParkingRateRules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == sourceLotId
                    && (x.ParkingScheduleId == null || sourceScheduleIds.Contains(x.ParkingScheduleId.Value)))
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);

            var rulesBySchedule = allSourceRules.ToLookup(x => x.ParkingScheduleId);

            foreach (var srcSchedule in sourceSchedules)
            {
                var newSchedule = new ParkingLotSchedule
                {
                    ParkingLotId = targetLotId,
                    ScheduleName = srcSchedule.ScheduleName,
                    DaysOfWeek = srcSchedule.DaysOfWeek,
                    IsAllDay = srcSchedule.IsAllDay,
                    OpenTime = srcSchedule.OpenTime,
                    CloseTime = srcSchedule.CloseTime,
                    BillingStartTime = srcSchedule.BillingStartTime,
                    BillingEndTime = srcSchedule.BillingEndTime,
                    IsActive = srcSchedule.IsActive,
                };
                newSchedule.SetCreated(userId);
                db.ParkingLotSchedules.Add(newSchedule);

                foreach (var srcRule in rulesBySchedule[srcSchedule.Id])
                {
                    var r = CloneRule(srcRule, targetLotId, userId);
                    r.ParkingSchedule = newSchedule;
                    db.ParkingRateRules.Add(r);
                }
            }

            foreach (var srcRule in rulesBySchedule[null])
            {
                db.ParkingRateRules.Add(CloneRule(srcRule, targetLotId, userId));
            }

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(targetLotId);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบลานจอดรถ");

            existing.SetDeleted(currentUserContext.CurrentUserId);
            await db.SaveChangesAsync(cancellationToken);
        }

        private static ParkingRateRule CloneRule(ParkingRateRule src, Guid targetLotId, Guid? userId)
        {
            var rule = new ParkingRateRule
            {
                ParkingLotId = targetLotId,
                RuleName = src.RuleName,
                Sequence = src.Sequence,
                StartMinute = src.StartMinute,
                EndMinute = src.EndMinute,
                CalculationType = src.CalculationType,
                Amount = src.Amount,
                BillingStepMinutes = src.BillingStepMinutes,
                IsActive = src.IsActive,
            };
            rule.SetCreated(userId);
            return rule;
        }
    }
}
