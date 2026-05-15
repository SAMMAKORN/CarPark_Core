using CarPark.Shared;
using CarPark.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPark.Models
{
    public class ParkingRateRule : BaseEntity
    {
        [ForeignKey(nameof(ParkingLot))]
        public Guid ParkingLotId { get; set; }

        public string RuleName { get; set; } = string.Empty;

        public int Sequence { get; set; }

        public int StartMinute { get; set; }

        public int? EndMinute { get; set; }

        public ParkingRateCalculationType CalculationType { get; set; }

        public decimal Amount { get; set; }

        public int? BillingStepMinutes { get; set; }

        /// <summary>null = กฎทั่วไปของลาน, มีค่า = กฎเฉพาะ schedule นั้น</summary>
        [ForeignKey(nameof(ParkingSchedule))]
        public Guid? ParkingScheduleId { get; set; }

        public ParkingLot? ParkingLot { get; set; }

        public ParkingLotSchedule? ParkingSchedule { get; set; }
    }
}