using CarPark.Data;
using CarPark.Models;
using CarPark.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingTransactionService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<ParkingLot>> GetActiveParkingLotsAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLots
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.LotName)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ParkingTransaction>> GetByDateRangeAsync(
            DateTime dateFromLocal,
            DateTime dateToLocal,
            CancellationToken cancellationToken = default)
        {
            var utcStart = dateFromLocal.Date.ToUniversalTime();
            var utcEnd = dateToLocal.Date.AddDays(1).ToUniversalTime();

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .Include(x => x.InGate)
                .Include(x => x.OutGate)
                .Where(x => x.InAt >= utcStart && x.InAt < utcEnd)
                .OrderBy(x => x.InAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ParkingTransaction>> GetTodayTransactionsAsync(CancellationToken cancellationToken = default)
        {
            var utcStart = DateTime.Today.ToUniversalTime();
            var utcEnd = utcStart.AddDays(1);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .Include(x => x.InGate)
                .Include(x => x.OutGate)
                .Include(x => x.ParkingCondition)
                .Where(x => (x.InAt >= utcStart && x.InAt < utcEnd) || !x.OutAt.HasValue)
                .OrderByDescending(x => x.InAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ParkingTransaction>> GetRecentTransactionsAsync(int take = 50, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .Include(x => x.InGate)
                .Include(x => x.OutGate)
                .OrderByDescending(x => x.InAt)
                .Take(Math.Clamp(take, 1, 200))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ParkingTransaction>> GetOpenTransactionsAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .Include(x => x.InGate)
                .Include(x => x.OutGate)
                .Where(x => x.OutAt == null)
                .OrderByDescending(x => x.InAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ParkingTransaction?> GetOpenByTicketNoAsync(string ticketNo, Guid? parkingLotId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketNo))
            {
                return null;
            }

            var normalizedTicketNo = ticketNo.Trim();
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingTransactions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .FirstOrDefaultAsync(
                    x => x.TicketNo == normalizedTicketNo
                         && x.OutAt == null
                         && (!parkingLotId.HasValue || x.ParkingLotId == parkingLotId.Value),
                    cancellationToken);
        }

        public async Task<List<ParkingCondition>> GetApplicableConditionsAsync(
            Guid parkingLotId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await db.ParkingConditions
                .AsNoTracking()
                .Where(x => x.IsActive && x.ConditionLots.Any(cl => cl.ParkingLotId == parkingLotId))
                .OrderBy(x => x.ConditionName)
                .ToListAsync(cancellationToken);
        }

        public async Task CorrectConditionAsync(
            Guid transactionId,
            Guid? conditionId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var tx = await db.ParkingTransactions
                .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบรายการจอดรถ");

            if (!tx.OutAt.HasValue)
                throw new InvalidOperationException("ยังไม่ได้คืนบัตร ไม่สามารถแก้ไขเงื่อนไขได้");

            var lot = await db.ParkingLots.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tx.ParkingLotId, cancellationToken);

            var schedules = await db.ParkingLotSchedules.AsNoTracking()
                .Where(x => x.ParkingLotId == tx.ParkingLotId && x.IsActive)
                .ToListAsync(cancellationToken);

            var inDayBit = ParkingLotScheduleService.DayOfWeekToBit(tx.InAt.ToLocalTime().DayOfWeek);
            var matchedSchedule = schedules.FirstOrDefault(s => (s.DaysOfWeek & inDayBit) != 0);

            var activeRules = await db.ParkingRateRules.AsNoTracking()
                .Where(x => x.ParkingLotId == tx.ParkingLotId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);

            ParkingCondition? condition = null;
            if (conditionId.HasValue)
                condition = await db.ParkingConditions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == conditionId.Value, cancellationToken);

            var charge = CalculateCharge(activeRules, tx.InAt, tx.OutAt.Value, condition, lot, matchedSchedule);

            tx.ParkingConditionId = condition?.Id;
            tx.TotalMinutes = charge.TotalMinutes;
            tx.TotalAmount = charge.TotalAmount;
            tx.IsOvernight = charge.IsOvernight;
            tx.Remark = condition?.ConditionName;
            tx.UpdateBy = currentUserContext.CurrentUserId;
            tx.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(
            Guid id,
            string ticketNo,
            string plateNo,
            DateTime inAtLocal,
            DateTime? outAtLocal,
            decimal? totalAmount,
            string? remark = null,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingTransactions
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบรายการจอดรถ");

            var inAtUtc = inAtLocal.ToUniversalTime();
            var outAtUtc = outAtLocal?.ToUniversalTime();

            int? totalMinutes = null;
            if (outAtUtc.HasValue)
            {
                totalMinutes = (int)Math.Ceiling((outAtUtc.Value - inAtUtc).TotalMinutes);
                if (totalMinutes < 0) totalMinutes = 0;
            }

            existing.TicketNo = NormalizeRequired(ticketNo, "กรุณากรอกหมายเลขบัตร");
            existing.PlateNo = NormalizeRequired(plateNo, "กรุณากรอกทะเบียนรถ");
            existing.InAt = inAtUtc;
            existing.OutAt = outAtUtc;
            existing.TotalMinutes = totalMinutes;
            existing.TotalAmount = totalAmount;
            existing.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
            existing.Status = outAtUtc.HasValue ? TransactionType.OUT : TransactionType.IN;
            existing.IsOvernight = outAtUtc.HasValue && inAtUtc.Date != outAtUtc.Value.Date;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<ParkingTransaction> CheckInAsync(
            Guid parkingLotId,
            string ticketNo,
            string plateNo,
            Guid? inGateId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedTicketNo = NormalizeRequired(ticketNo, "Ticket number is required.");
            var normalizedPlateNo = NormalizeRequired(plateNo, "Plate number is required.");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var lotExists = await db.ParkingLots.AnyAsync(x => x.Id == parkingLotId && x.IsActive, cancellationToken);
            if (!lotExists)
            {
                throw new InvalidOperationException("Parking lot not found or inactive.");
            }

            var hasOpenTicket = await db.ParkingTransactions
                .AnyAsync(
                    x => x.TicketNo == normalizedTicketNo
                         && x.OutAt == null,
                    cancellationToken);
            if (hasOpenTicket)
            {
                throw new InvalidOperationException("This ticket is already checked in and not checked out yet.");
            }

            var entity = new ParkingTransaction
            {
                ParkingLotId = parkingLotId,
                InGateId = inGateId,
                TicketNo = normalizedTicketNo,
                PlateNo = normalizedPlateNo,
                InAt = DateTime.UtcNow,
                Status = TransactionType.IN,
                CreateBy = currentUserContext.CurrentUserId,
                CreateAt = DateTime.UtcNow
            };

            db.ParkingTransactions.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task<CheckOutPreview> GetCheckOutPreviewAsync(
            string ticketNo,
            Guid? parkingLotId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedTicketNo = NormalizeRequired(ticketNo, "Ticket number is required.");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TicketNo == normalizedTicketNo
                         && x.OutAt == null
                         && (!parkingLotId.HasValue || x.ParkingLotId == parkingLotId.Value),
                    cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบบัตรที่เปิดอยู่สำหรับลานจอดรถนี้");

            var now = DateTime.UtcNow;

            var lot = await db.ParkingLots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == existing.ParkingLotId, cancellationToken);

            var schedules = await db.ParkingLotSchedules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == existing.ParkingLotId && x.IsActive)
                .ToListAsync(cancellationToken);

            var inAtLocal = existing.InAt.ToLocalTime();
            var nowLocal = now.ToLocalTime();
            var inDayBit = ParkingLotScheduleService.DayOfWeekToBit(inAtLocal.DayOfWeek);
            var matchedSchedule = schedules.FirstOrDefault(s => (s.DaysOfWeek & inDayBit) != 0);

            // คำนวณนาทีที่เรียกเก็บได้สำหรับ matching conditions
            bool previewIsAllDay = matchedSchedule?.IsAllDay ?? lot?.IsAllDay ?? true;
            int totalMinutes;
            if (previewIsAllDay)
            {
                totalMinutes = (int)Math.Ceiling((now - existing.InAt).TotalMinutes);
                if (totalMinutes < 0) totalMinutes = 0;
            }
            else
            {
                TimeSpan billingStart, billingEnd;
                if (matchedSchedule is not null)
                {
                    billingStart = matchedSchedule.BillingStartTime ?? matchedSchedule.OpenTime;
                    billingEnd = matchedSchedule.BillingEndTime ?? matchedSchedule.CloseTime;
                }
                else
                {
                    billingStart = lot?.BillingStartTime ?? lot?.OpenTime ?? default;
                    billingEnd = lot?.BillingEndTime ?? lot?.CloseTime ?? default;
                }
                totalMinutes = CalculateBillableMinutes(inAtLocal, nowLocal, billingStart, billingEnd);
            }

            var activeRules = await db.ParkingRateRules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == existing.ParkingLotId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);

            var applicableConditions = await db.ParkingConditions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ConditionLots)
                .Where(x => x.IsActive && x.ConditionLots.Any(cl => cl.ParkingLotId == existing.ParkingLotId))
                .OrderBy(x => x.ConditionName)
                .ToListAsync(cancellationToken);

            var chargeResult = CalculateCharge(activeRules, existing.InAt, now, condition: null, lot, matchedSchedule);

            // ── ตรวจสอบเงื่อนไขที่ใช้ไม่ได้ ──
            var disabledReasons = new Dictionary<Guid, string>();

            var lotOpenTime = matchedSchedule?.OpenTime ?? lot?.OpenTime ?? default;
            var nowLocalForQuota = DateTime.Now;
            var todayOpen = nowLocalForQuota.Date + lotOpenTime;
            var operatingDayStartUtc = (todayOpen > nowLocalForQuota ? todayOpen.AddDays(-1) : todayOpen).ToUniversalTime();

            foreach (var cond in applicableConditions)
            {
                if (cond.ConditionType == Shared.Enums.ParkingConditionType.FreeFirstMinutes
                    && cond.FreeMinutes.HasValue
                    && totalMinutes > cond.FreeMinutes.Value)
                {
                    disabledReasons[cond.Id] = $"จอดเกิน {cond.FreeMinutes} นาทีแรกแล้ว";
                }
                else if (cond.ConditionType == Shared.Enums.ParkingConditionType.QuotaFree
                         && cond.QuotaPerDay.HasValue)
                {
                    var usedToday = await db.ParkingTransactions
                        .AsNoTracking()
                        .CountAsync(x =>
                            x.ParkingConditionId == cond.Id
                            && x.ParkingLotId == existing.ParkingLotId
                            && x.OutAt.HasValue
                            && x.OutAt >= operatingDayStartUtc,
                            cancellationToken);

                    if (usedToday >= cond.QuotaPerDay.Value)
                        disabledReasons[cond.Id] = $"โควต้าเต็ม ({usedToday}/{cond.QuotaPerDay} คัน/วัน)";
                }
            }

            return new CheckOutPreview(existing, applicableConditions, chargeResult.TotalAmount, disabledReasons);
        }

        public async Task<CheckOutResult> CheckOutAsync(
            string ticketNo,
            string plateNo,
            Guid? parkingLotId = null,
            Guid? parkingConditionId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedPlateNo = NormalizeRequired(plateNo, "Plate number is required.");
            return await CheckOutCoreAsync(ticketNo, normalizedPlateNo, parkingLotId, parkingConditionId, cancellationToken);
        }

        public async Task<CheckOutResult> CheckOutByTicketAsync(
            string ticketNo,
            Guid? parkingLotId = null,
            Guid? parkingConditionId = null,
            CancellationToken cancellationToken = default)
        {
            return await CheckOutCoreAsync(ticketNo, null, parkingLotId, parkingConditionId, cancellationToken);
        }

        private async Task<CheckOutResult> CheckOutCoreAsync(
            string ticketNo,
            string? expectedPlateNo,
            Guid? parkingLotId = null,
            Guid? parkingConditionId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedTicketNo = NormalizeRequired(ticketNo, "Ticket number is required.");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var existing = await db.ParkingTransactions
                .FirstOrDefaultAsync(
                    x => x.TicketNo == normalizedTicketNo
                         && x.OutAt == null
                         && (!parkingLotId.HasValue || x.ParkingLotId == parkingLotId.Value),
                    cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบบัตรที่เปิดอยู่สำหรับลานจอดรถนี้");

            if (!string.IsNullOrWhiteSpace(expectedPlateNo)
                && !string.Equals(existing.PlateNo, expectedPlateNo, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Plate number does not match this ticket.");
            }

            var outAt = DateTime.UtcNow;
            if (outAt < existing.InAt)
            {
                throw new InvalidOperationException("Invalid checkout time.");
            }

            var lot = await db.ParkingLots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == existing.ParkingLotId, cancellationToken);

            var schedules = await db.ParkingLotSchedules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == existing.ParkingLotId && x.IsActive)
                .ToListAsync(cancellationToken);

            var inAtLocal = existing.InAt.ToLocalTime();
            var inDayBit = ParkingLotScheduleService.DayOfWeekToBit(inAtLocal.DayOfWeek);
            var matchedSchedule = schedules.FirstOrDefault(s => (s.DaysOfWeek & inDayBit) != 0);

            var cachedRules = currentUserContext.GetCachedRules(existing.ParkingLotId);
            var activeRules = cachedRules is not null
                ? [.. cachedRules]
                : await db.ParkingRateRules
                    .Where(x => x.ParkingLotId == existing.ParkingLotId && x.IsActive)
                    .OrderBy(x => x.Sequence)
                    .ToListAsync(cancellationToken);

            // โหลดและตรวจสอบเงื่อนไข
            ParkingCondition? condition = null;
            bool quotaExceeded = false;
            string? quotaMessage = null;

            if (parkingConditionId.HasValue)
            {
                condition = await db.ParkingConditions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == parkingConditionId.Value, cancellationToken);

                if (condition is not null && condition.ConditionType == Shared.Enums.ParkingConditionType.QuotaFree
                    && condition.QuotaPerDay.HasValue)
                {
                    var lotOpenTime = matchedSchedule?.OpenTime ?? lot?.OpenTime ?? default;
                    var nowLocal = DateTime.Now;
                    var todayOpen = nowLocal.Date + lotOpenTime;
                    var operatingDayStart = todayOpen > nowLocal ? todayOpen.AddDays(-1) : todayOpen;
                    var operatingDayStartUtc = operatingDayStart.ToUniversalTime();

                    var usedToday = await db.ParkingTransactions
                        .AsNoTracking()
                        .CountAsync(x =>
                            x.ParkingConditionId == condition.Id
                            && x.ParkingLotId == existing.ParkingLotId
                            && x.OutAt.HasValue
                            && x.OutAt >= operatingDayStartUtc,
                            cancellationToken);

                    if (usedToday >= condition.QuotaPerDay.Value)
                    {
                        quotaExceeded = true;
                        quotaMessage = $"โควต้าเต็ม: {condition.ConditionName} ({condition.QuotaPerDay} คัน/วัน) — คิดราคาปกติ";
                        condition = null;
                    }
                }
            }

            var chargeResult = CalculateCharge(activeRules, existing.InAt, outAt, condition, lot, matchedSchedule);

            existing.OutAt = outAt;
            existing.OutGateId = currentUserContext.CurrentGate?.Id;
            existing.TotalMinutes = chargeResult.TotalMinutes;
            existing.TotalAmount = chargeResult.TotalAmount;
            existing.IsOvernight = chargeResult.IsOvernight;
            existing.ParkingConditionId = condition?.Id;
            existing.Remark = condition is not null ? condition.ConditionName : null;
            existing.Status = TransactionType.OUT;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            var saved = await db.ParkingTransactions
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .Include(x => x.InGate)
                .Include(x => x.OutGate)
                .FirstAsync(x => x.Id == existing.Id, cancellationToken);

            return new CheckOutResult(saved, quotaExceeded, quotaMessage);
        }

        private static string NormalizeRequired(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(message);
            }

            return value.Trim();
        }

        private static ChargeResult CalculateCharge(
            List<ParkingRateRule> allRules,
            DateTime inAtUtc,
            DateTime outAtUtc,
            ParkingCondition? condition = null,
            ParkingLot? lot = null,
            ParkingLotSchedule? matchedSchedule = null)
        {
            var inAtLocal = inAtUtc.ToLocalTime();
            var outAtLocal = outAtUtc.ToLocalTime();

            // กำหนดเวลาทำการ + ช่วงเวลาเก็บค่า จาก schedule (ถ้ามี) หรือ lot (fallback)
            bool isAllDay;
            TimeSpan openTime, closeTime, billingStart, billingEnd;
            List<ParkingRateRule> rules;

            if (matchedSchedule is not null)
            {
                isAllDay = matchedSchedule.IsAllDay;
                openTime = matchedSchedule.OpenTime;
                closeTime = matchedSchedule.CloseTime;
                billingStart = matchedSchedule.BillingStartTime ?? matchedSchedule.OpenTime;
                billingEnd = matchedSchedule.BillingEndTime ?? matchedSchedule.CloseTime;
                rules = allRules.Where(r => r.ParkingScheduleId == matchedSchedule.Id).ToList();
            }
            else
            {
                isAllDay = lot?.IsAllDay ?? true;
                openTime = lot?.OpenTime ?? default;
                closeTime = lot?.CloseTime ?? default;
                billingStart = lot?.BillingStartTime ?? openTime;
                billingEnd = lot?.BillingEndTime ?? closeTime;
                rules = allRules.Where(r => r.ParkingScheduleId == null).ToList();
            }

            // ถ้าเวลาเข้าอยู่นอกเวลาทำการ → จอดฟรี ไม่ถือว่าค้างคืน
            if (!isAllDay)
            {
                var inTime = TimeOnly.FromTimeSpan(inAtLocal.TimeOfDay);
                var open = TimeOnly.FromTimeSpan(openTime);
                var close = TimeOnly.FromTimeSpan(closeTime);

                var isOutside = open <= close
                    ? inTime < open || inTime >= close
                    : inTime < open && inTime >= close;

                if (isOutside)
                {
                    var rawMinutes = (int)Math.Ceiling((outAtUtc - inAtUtc).TotalMinutes);
                    return new ChargeResult(rawMinutes < 0 ? 0 : rawMinutes, 0m, false);
                }
            }

            // ค้างคืนตรวจจากเวลาจริงที่รถอยู่ (ไม่หักเวลาฟรี)
            int nightCount = isAllDay
                ? (outAtLocal.Date - inAtLocal.Date).Days
                : CalculateOvernightNights(inAtLocal, outAtLocal, closeTime);
            var isOvernight = nightCount > 0;

            // FullyFree / QuotaFree → ฟรี ±ค่าปรับค้างคืน
            if (condition is not null
                && (condition.ConditionType == Shared.Enums.ParkingConditionType.FullyFree
                    || condition.ConditionType == Shared.Enums.ParkingConditionType.QuotaFree))
            {
                var rawMin = (int)Math.Ceiling((outAtUtc - inAtUtc).TotalMinutes);
                if (rawMin < 0) rawMin = 0;
                if (!isOvernight || condition.WaiveOvernightPenalty)
                    return new ChargeResult(rawMin, 0m, isOvernight);
                if (lot is not null && lot.HasOvernightPenalty && lot.OvernightPenaltyAmount > 0)
                    return new ChargeResult(rawMin, lot.OvernightPenaltyAmount * nightCount, true);
                return new ChargeResult(rawMin, 0m, true);
            }

            // FreeFirstMinutes → เลื่อน inAt ไปข้างหน้าตามนาทีที่จอดฟรี แล้วค่อยคำนวณ billable
            if (condition is not null && condition.ConditionType == Shared.Enums.ParkingConditionType.FreeFirstMinutes
                && condition.FreeMinutes.HasValue)
            {
                var originalInAtUtc = inAtUtc;
                inAtLocal = inAtLocal.AddMinutes(condition.FreeMinutes.Value);
                inAtUtc = inAtUtc.AddMinutes(condition.FreeMinutes.Value);
                if (inAtLocal >= outAtLocal)
                {
                    // ออกภายในช่วงเวลาฟรี → ไม่คิดเงิน
                    var rawMin = (int)Math.Ceiling((outAtUtc - originalInAtUtc).TotalMinutes);
                    return new ChargeResult(rawMin < 0 ? 0 : rawMin, 0m, isOvernight);
                }
            }

            // คำนวณนาทีที่เรียกเก็บได้ นับจาก effective inAt (หลังหักเวลาฟรีแล้ว)
            int totalMinutes;
            if (isAllDay)
            {
                totalMinutes = (int)Math.Ceiling((outAtUtc - inAtUtc).TotalMinutes);
                if (totalMinutes < 0) totalMinutes = 0;
            }
            else
            {
                totalMinutes = CalculateBillableMinutes(inAtLocal, outAtLocal, billingStart, billingEnd);
            }

            var regularRules = rules.OrderBy(x => x.Sequence).ToList();
            var matchedRule = regularRules.FirstOrDefault(
                x => totalMinutes >= x.StartMinute
                     && (!x.EndMinute.HasValue || totalMinutes <= x.EndMinute.Value)
                     );

            decimal baseAmount = 0m;
            if (matchedRule is not null)
            {
                baseAmount = matchedRule.CalculationType switch
                {
                    ParkingRateCalculationType.Free => 0m,
                    ParkingRateCalculationType.FlatAmount => matchedRule.Amount,
                    ParkingRateCalculationType.PerHour => CalculatePerStepAmount(totalMinutes, matchedRule),
                    _ => 0m
                };
            }

            if (!isOvernight)
                return new ChargeResult(totalMinutes, baseAmount, false);

            // ค่าปรับข้ามคืนกำหนดที่ระดับ ParkingLot คูณตามจำนวนคืน
            if (lot is not null && lot.HasOvernightPenalty && lot.OvernightPenaltyAmount > 0)
            {
                var totalPenalty = lot.OvernightPenaltyAmount * nightCount;
                return new ChargeResult(totalMinutes, totalPenalty, true);
            }

            return new ChargeResult(totalMinutes, baseAmount, true);
        }

        // คำนวณนาทีที่เรียกเก็บได้ โดยนับเฉพาะเวลาในช่วงเก็บค่า (billingStart–billingEnd) ของแต่ละวัน
        private static int CalculateBillableMinutes(DateTime inAtLocal, DateTime outAtLocal, TimeSpan openTime, TimeSpan closeTime)
        {
            int billable = 0;
            var currentDate = inAtLocal.Date;

            while (currentDate <= outAtLocal.Date)
            {
                var dayOpen = currentDate + openTime;
                var dayClose = currentDate + closeTime;

                var windowStart = currentDate == inAtLocal.Date
                    ? (inAtLocal > dayOpen ? inAtLocal : dayOpen)
                    : dayOpen;
                var windowEnd = currentDate == outAtLocal.Date
                    ? (outAtLocal < dayClose ? outAtLocal : dayClose)
                    : dayClose;

                if (windowEnd > windowStart)
                    billable += (int)Math.Ceiling((windowEnd - windowStart).TotalMinutes);

                currentDate = currentDate.AddDays(1);
            }

            return Math.Max(0, billable);
        }

        // นับจำนวนคืนที่ค้างคืน: นับทุกวันที่รถยังอยู่หลังเวลาปิด
        private static int CalculateOvernightNights(DateTime inAtLocal, DateTime outAtLocal, TimeSpan closeTime)
        {
            int nights = 0;
            var currentDate = inAtLocal.Date;

            while (currentDate <= outAtLocal.Date)
            {
                var dayClose = currentDate + closeTime;

                // ข้ามวัน หรือออกหลังเวลาปิดในวันนั้น → ค้างคืน
                if (currentDate < outAtLocal.Date || outAtLocal > dayClose)
                    nights++;

                currentDate = currentDate.AddDays(1);
            }

            return nights;
        }

        private static decimal CalculatePerStepAmount(int totalMinutes, ParkingRateRule rule)
        {
            if (totalMinutes <= 0)
            {
                return 0m;
            }

            var step = rule.BillingStepMinutes.GetValueOrDefault(60);
            if (step <= 0)
            {
                step = 60;
            }

            var units = (int)Math.Ceiling((decimal)totalMinutes / step);
            return units * rule.Amount;
        }

        private readonly record struct ChargeResult(int TotalMinutes, decimal TotalAmount, bool IsOvernight);
    }
}