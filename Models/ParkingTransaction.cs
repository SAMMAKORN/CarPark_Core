using CarPark.Shared;
using CarPark.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPark.Models
{
    public class ParkingTransaction : BaseEntity
    {
        [ForeignKey(nameof(ParkingLot))]
        public Guid ParkingLotId { get; set; }

        public string TicketNo { get; set; } = string.Empty;

        public string PlateNo { get; set; } = string.Empty;

        public DateTime InAt { get; set; }

        public DateTime? OutAt { get; set; }

        public int? TotalMinutes { get; set; }

        public decimal? TotalAmount { get; set; }

        public bool IsOvernight { get; set; }

        public TransactionType Status { get; set; } = TransactionType.IN;

        [ForeignKey(nameof(InGate))]
        public Guid? InGateId { get; set; }

        [ForeignKey(nameof(OutGate))]
        public Guid? OutGateId { get; set; }

        public string? Remark { get; set; }

        [ForeignKey(nameof(ParkingCondition))]
        public Guid? ParkingConditionId { get; set; }

        public ParkingLot? ParkingLot { get; set; }

        public ParkingGate? InGate { get; set; }

        public ParkingGate? OutGate { get; set; }

        public ParkingCondition? ParkingCondition { get; set; }
    }
}