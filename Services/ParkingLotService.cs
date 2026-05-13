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
                HasOvernightPenalty = lot.HasOvernightPenalty,
                OvernightPenaltyAmount = lot.OvernightPenaltyAmount,
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
            existing.HasOvernightPenalty = lot.HasOvernightPenalty;
            existing.OvernightPenaltyAmount = lot.OvernightPenaltyAmount;
            existing.IsActive = lot.IsActive;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// คัดลอกการตั้งค่าลาน + ตารางเวลา + อัตราค่าบริการ (global + schedule) ทั้งหมดจากลานต้นทางไปยังลานปลายทาง
        /// ข้อมูลเดิมของลานปลายทาง (schedules + rules) จะถูก soft-delete แล้วแทนที่ด้วยของต้นทาง
        /// </summary>
        public async Task CopyFromLotAsync(Guid sourceLotId, Guid targetLotId, CancellationToken cancellationToken = default)
        {
            if (sourceLotId == targetLotId)
                throw new InvalidOperationException("ต้นทางและปลายทางต้องไม่ใช่ลานเดียวกัน");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var source = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == sourceLotId, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบลานต้นทาง");

            var target = await db.ParkingLots.FirstOrDefaultAsync(x => x.Id == targetLotId, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบลานปลายทาง");

            var now = DateTime.UtcNow;
            var userId = currentUserContext.CurrentUserId;

            // 1. คัดลอกการตั้งค่าลาน
            target.IsAllDay = source.IsAllDay;
            target.OpenTime = source.OpenTime;
            target.CloseTime = source.CloseTime;
            target.HasOvernightPenalty = source.HasOvernightPenalty;
            target.OvernightPenaltyAmount = source.OvernightPenaltyAmount;
            target.UpdateBy = userId;
            target.UpdateAt = now;

            // 2. Soft-delete schedules + rules เดิมของปลายทาง
            var targetSchedules = await db.ParkingLotSchedules
                .Where(x => x.ParkingLotId == targetLotId)
                .ToListAsync(cancellationToken);

            foreach (var s in targetSchedules)
            {
                var scheduleRules = await db.ParkingRateRules
                    .Where(x => x.ParkingScheduleId == s.Id)
                    .ToListAsync(cancellationToken);
                foreach (var r in scheduleRules)
                {
                    r.IsDeleted = true; r.IsActive = false;
                    r.DeletedBy = userId; r.DeleteAt = now;
                    r.UpdateBy = userId; r.UpdateAt = now;
                }
                s.IsDeleted = true; s.IsActive = false;
                s.DeletedBy = userId; s.DeleteAt = now;
                s.UpdateBy = userId; s.UpdateAt = now;
            }

            // 3. Soft-delete global rules เดิมของปลายทาง
            var targetGlobalRules = await db.ParkingRateRules
                .Where(x => x.ParkingLotId == targetLotId && x.ParkingScheduleId == null)
                .ToListAsync(cancellationToken);
            foreach (var r in targetGlobalRules)
            {
                r.IsDeleted = true; r.IsActive = false;
                r.DeletedBy = userId; r.DeleteAt = now;
                r.UpdateBy = userId; r.UpdateAt = now;
            }

            // 4. คัดลอก schedules + rules จากต้นทาง
            var sourceSchedules = await db.ParkingLotSchedules
                .Where(x => x.ParkingLotId == sourceLotId)
                .ToListAsync(cancellationToken);

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
                    IsActive = srcSchedule.IsActive,
                    CreateBy = userId,
                    CreateAt = now
                };
                db.ParkingLotSchedules.Add(newSchedule);

                var srcScheduleRules = await db.ParkingRateRules
                    .Where(x => x.ParkingScheduleId == srcSchedule.Id)
                    .OrderBy(x => x.Sequence)
                    .ToListAsync(cancellationToken);

                foreach (var srcRule in srcScheduleRules)
                {
                    db.ParkingRateRules.Add(new ParkingRateRule
                    {
                        ParkingLotId = targetLotId,
                        ParkingSchedule = newSchedule,
                        RuleName = srcRule.RuleName,
                        Sequence = srcRule.Sequence,
                        StartMinute = srcRule.StartMinute,
                        EndMinute = srcRule.EndMinute,
                        CalculationType = srcRule.CalculationType,
                        Amount = srcRule.Amount,
                        BillingStepMinutes = srcRule.BillingStepMinutes,
                        IsActive = srcRule.IsActive,
                        CreateBy = userId,
                        CreateAt = now
                    });
                }
            }

            // 5. คัดลอก global rules จากต้นทาง
            var sourceGlobalRules = await db.ParkingRateRules
                .Where(x => x.ParkingLotId == sourceLotId && x.ParkingScheduleId == null)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);

            foreach (var srcRule in sourceGlobalRules)
            {
                db.ParkingRateRules.Add(new ParkingRateRule
                {
                    ParkingLotId = targetLotId,
                    ParkingScheduleId = null,
                    RuleName = srcRule.RuleName,
                    Sequence = srcRule.Sequence,
                    StartMinute = srcRule.StartMinute,
                    EndMinute = srcRule.EndMinute,
                    CalculationType = srcRule.CalculationType,
                    Amount = srcRule.Amount,
                    BillingStepMinutes = srcRule.BillingStepMinutes,
                    IsActive = srcRule.IsActive,
                    CreateBy = userId,
                    CreateAt = now
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            currentUserContext.InvalidateCachedRules(targetLotId);
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