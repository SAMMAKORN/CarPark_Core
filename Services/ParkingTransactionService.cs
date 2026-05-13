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
                .OrderBy(x => x.LotCode)
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

        public async Task UpdateAsync(
            Guid id,
            string ticketNo,
            string plateNo,
            DateTime inAtLocal,
            DateTime? outAtLocal,
            decimal? totalAmount,
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

        public async Task<ParkingTransaction> CheckOutAsync(
            string ticketNo,
            string plateNo,
            Guid? parkingLotId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedPlateNo = NormalizeRequired(plateNo, "Plate number is required.");
            return await CheckOutCoreAsync(ticketNo, normalizedPlateNo, parkingLotId, cancellationToken);
        }

        public async Task<ParkingTransaction> CheckOutByTicketAsync(
            string ticketNo,
            Guid? parkingLotId = null,
            CancellationToken cancellationToken = default)
        {
            return await CheckOutCoreAsync(ticketNo, null, parkingLotId, cancellationToken);
        }

        private async Task<ParkingTransaction> CheckOutCoreAsync(
            string ticketNo,
            string? expectedPlateNo,
            Guid? parkingLotId = null,
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

            var chargeResult = CalculateCharge(activeRules, existing.InAt, outAt, lot, matchedSchedule);

            existing.OutAt = outAt;
            existing.OutGateId = currentUserContext.CurrentGate?.Id;
            existing.TotalMinutes = chargeResult.TotalMinutes;
            existing.TotalAmount = chargeResult.TotalAmount;
            existing.IsOvernight = chargeResult.IsOvernight;
            existing.Status = TransactionType.OUT;
            existing.UpdateBy = currentUserContext.CurrentUserId;
            existing.UpdateAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            return await db.ParkingTransactions
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .Include(x => x.InGate)
                .Include(x => x.OutGate)
                .FirstAsync(x => x.Id == existing.Id, cancellationToken);
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
            ParkingLot? lot = null,
            ParkingLotSchedule? matchedSchedule = null)
        {
            var totalMinutes = (int)Math.Ceiling((outAtUtc - inAtUtc).TotalMinutes);
            if (totalMinutes < 0) totalMinutes = 0;

            // กำหนดเวลาทำการและกฎที่ใช้ตาม schedule (ถ้ามี) หรือ lot (fallback)
            bool isAllDay;
            TimeSpan openTime, closeTime;
            List<ParkingRateRule> rules;

            if (matchedSchedule is not null)
            {
                isAllDay = matchedSchedule.IsAllDay;
                openTime = matchedSchedule.OpenTime;
                closeTime = matchedSchedule.CloseTime;
                rules = allRules.Where(r => r.ParkingScheduleId == matchedSchedule.Id).ToList();
            }
            else
            {
                isAllDay = lot?.IsAllDay ?? true;
                openTime = lot?.OpenTime ?? default;
                closeTime = lot?.CloseTime ?? default;
                rules = allRules.Where(r => r.ParkingScheduleId == null).ToList();
            }

            // ถ้าเวลาเข้าอยู่นอกเวลาทำการ → จอดฟรี
            if (!isAllDay)
            {
                var inAtLocal = inAtUtc.ToLocalTime();
                var inTime = TimeOnly.FromTimeSpan(inAtLocal.TimeOfDay);
                var open = TimeOnly.FromTimeSpan(openTime);
                var close = TimeOnly.FromTimeSpan(closeTime);

                var isOutside = open <= close
                    ? inTime < open || inTime >= close
                    : inTime < open && inTime >= close;

                if (isOutside)
                    return new ChargeResult(totalMinutes, 0m, inAtUtc.Date != outAtUtc.Date);
            }

            var regularRules = rules.OrderBy(x => x.Sequence).ToList();
            var matchedRule = regularRules.FirstOrDefault(
                x => totalMinutes >= x.StartMinute
                     && (!x.EndMinute.HasValue || totalMinutes <= x.EndMinute.Value));

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

            var isOvernight = inAtUtc.Date != outAtUtc.Date;
            if (!isOvernight)
            {
                return new ChargeResult(totalMinutes, baseAmount, false);
            }

            // ค่าปรับข้ามคืนกำหนดที่ระดับ ParkingLot
            if (lot is not null && lot.HasOvernightPenalty && lot.OvernightPenaltyAmount > 0)
            {
                var totalAmount = Math.Max(baseAmount, lot.OvernightPenaltyAmount);
                return new ChargeResult(totalMinutes, totalAmount, true);
            }

            return new ChargeResult(totalMinutes, baseAmount, true);
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