using CarPark.Shared;
using CarPark.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CarPark.Models
{
    public class ParkingCondition : BaseEntity
    {
        [MaxLength(200)]
        public string ConditionName { get; set; } = string.Empty;

        public ParkingConditionType ConditionType { get; set; }

        /// <summary>เฉพาะ FreeFirstMinutes — จำนวนนาทีที่ยกเว้น</summary>
        public int? FreeMinutes { get; set; }

        /// <summary>เฉพาะ QuotaFree — จำกัดกี่คัน/วันทำการ</summary>
        public int? QuotaPerDay { get; set; }

        public bool WaiveOvernightPenalty { get; set; } = false;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public ICollection<ParkingConditionLot> ConditionLots { get; set; } = [];
    }
}