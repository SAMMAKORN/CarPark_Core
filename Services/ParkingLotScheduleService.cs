using CarPark.Data;
using CarPark.Models;
using CarPark.Shared;
using CarPark.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingLotScheduleService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<ParkingLotSchedule>> GetByLotIdAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLotSchedules
                .AsNoTracking()
                .Where(x => x.ParkingLotId == parkingLotId)
                .OrderBy(x => x.DaysOfWeek)
                .ToListAsync(cancellationToken);
        }

        public async Task<ParkingLotSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingLotSchedules
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.ParkingLot)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<ParkingLotSchedule> CreateAsync(ParkingLotSchedule schedule, CancellationToken cancellationToken = default)
        {
            Validate(schedule);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            await CheckDayOverlapAsync(db, schedule, null, cancellationToken);

            var entity = new ParkingLotSchedule
            {
                ParkingLotId = schedule.ParkingLotId,
                ScheduleName = schedule.ScheduleName.Trim(),
                DaysOfWeek = schedule.DaysOfWeek,
                IsAllDay = schedule.IsAllDay,
                OpenTime = schedule.OpenTime,
                CloseTime = schedule.CloseTime,
                BillingStartTime = schedule.BillingStartTime,
                BillingEndTime = schedule.BillingEndTime,
                IsActive = schedule.IsActive,
            };
            entity.SetCreated(currentUserContext.CurrentUserId);

            db.ParkingLotSchedules.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(ParkingLotSchedule schedule, CancellationToken cancellationToken = default)
        {
            Validate(schedule);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingLotSchedules.FirstOrDefaultAsync(x => x.Id == schedule.Id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบตารางเวลา");

            await CheckDayOverlapAsync(db, schedule, schedule.Id, cancellationToken);

            existing.ScheduleName = schedule.ScheduleName.Trim();
            existing.DaysOfWeek = schedule.DaysOfWeek;
            existing.IsAllDay = schedule.IsAllDay;
            existing.OpenTime = schedule.OpenTime;
            existing.CloseTime = schedule.CloseTime;
            existing.BillingStartTime = schedule.BillingStartTime;
            existing.BillingEndTime = schedule.BillingEndTime;
            existing.IsActive = schedule.IsActive;
            existing.SetUpdated(currentUserContext.CurrentUserId);

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingLotSchedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบตารางเวลา");

            existing.SetDeleted(currentUserContext.CurrentUserId);
            await db.SaveChangesAsync(cancellationToken);
        }

        // ─── Helpers ───────────────────────────────────────────────────

        private static void Validate(ParkingLotSchedule schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule.ScheduleName))
                throw new InvalidOperationException("กรุณากรอกชื่อตารางเวลา");

            if (schedule.DaysOfWeek == 0)
                throw new InvalidOperationException("กรุณาเลือกวันอย่างน้อย 1 วัน");

            if (!schedule.IsAllDay && schedule.OpenTime >= schedule.CloseTime)
                throw new InvalidOperationException("เวลาเปิดต้องน้อยกว่าเวลาปิด");

            if (!schedule.IsAllDay && schedule.BillingStartTime.HasValue && schedule.BillingEndTime.HasValue
                && schedule.BillingStartTime.Value >= schedule.BillingEndTime.Value)
                throw new InvalidOperationException("เวลาเริ่มเก็บค่าต้องน้อยกว่าเวลาสิ้นสุดเก็บค่า");
        }

        private static async Task CheckDayOverlapAsync(
            AppDbContext db,
            ParkingLotSchedule schedule,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var others = await db.ParkingLotSchedules
                .Where(x => x.ParkingLotId == schedule.ParkingLotId
                            && x.IsActive
                            && (!excludeId.HasValue || x.Id != excludeId.Value))
                .ToListAsync(cancellationToken);

            foreach (var other in others)
            {
                if ((other.DaysOfWeek & schedule.DaysOfWeek) != 0)
                {
                    var overlap = GetDayNames((WeekDays)(other.DaysOfWeek & schedule.DaysOfWeek));
                    throw new InvalidOperationException($"วัน {overlap} ซ้อนทับกับตารางเวลา '{other.ScheduleName}'");
                }
            }
        }

        public static string GetDayNames(WeekDays days)
        {
            var names = new List<string>();
            if (days.HasFlag(WeekDays.Monday)) names.Add("จันทร์");
            if (days.HasFlag(WeekDays.Tuesday)) names.Add("อังคาร");
            if (days.HasFlag(WeekDays.Wednesday)) names.Add("พุธ");
            if (days.HasFlag(WeekDays.Thursday)) names.Add("พฤหัส");
            if (days.HasFlag(WeekDays.Friday)) names.Add("ศุกร์");
            if (days.HasFlag(WeekDays.Saturday)) names.Add("เสาร์");
            if (days.HasFlag(WeekDays.Sunday)) names.Add("อาทิตย์");
            return string.Join(", ", names);
        }

        public static int DayOfWeekToBit(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => (int)WeekDays.Monday,
            DayOfWeek.Tuesday => (int)WeekDays.Tuesday,
            DayOfWeek.Wednesday => (int)WeekDays.Wednesday,
            DayOfWeek.Thursday => (int)WeekDays.Thursday,
            DayOfWeek.Friday => (int)WeekDays.Friday,
            DayOfWeek.Saturday => (int)WeekDays.Saturday,
            DayOfWeek.Sunday => (int)WeekDays.Sunday,
            _ => 0
        };
    }
}