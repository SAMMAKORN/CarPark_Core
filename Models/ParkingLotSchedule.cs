using CarPark.Shared;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPark.Models
{
    public class ParkingLotSchedule : BaseEntity
    {
        [ForeignKey(nameof(ParkingLot))]
        public Guid ParkingLotId { get; set; }

        [MaxLength(100)]
        public string ScheduleName { get; set; } = string.Empty;

        /// <summary>วันทำการ — flags ของ WeekDays</summary>
        public int DaysOfWeek { get; set; }

        public bool IsAllDay { get; set; } = false;

        [Column(TypeName = "time")]
        public TimeSpan OpenTime { get; set; } = TimeSpan.FromHours(6);

        [Column(TypeName = "time")]
        public TimeSpan CloseTime { get; set; } = TimeSpan.FromHours(22);

        /// <summary>ถ้า null ใช้ OpenTime/CloseTime แทน</summary>
        [Column(TypeName = "time")]
        public TimeSpan? BillingStartTime { get; set; }

        /// <summary>ถ้า null ใช้ OpenTime/CloseTime แทน</summary>
        [Column(TypeName = "time")]
        public TimeSpan? BillingEndTime { get; set; }

        public ParkingLot? ParkingLot { get; set; }

        public ICollection<ParkingRateRule> RateRules { get; set; } = new List<ParkingRateRule>();
    }
}