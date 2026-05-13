using CarPark.Shared;
using CarPark.Shared.Enums;

namespace CarPark.Models
{
    public class ParkingTransaction : BaseEntity
    {
        public Guid ParkingLotId { get; set; }

        public string TicketNo { get; set; } = string.Empty;

        public string PlateNo { get; set; } = string.Empty;

        public DateTime InAt { get; set; }

        public DateTime? OutAt { get; set; }

        public int? TotalMinutes { get; set; }

        public decimal? TotalAmount { get; set; }

        public bool IsOvernight { get; set; }

        public TransactionType Status { get; set; } = TransactionType.IN;

        public Guid? InGateId { get; set; }

        public Guid? OutGateId { get; set; }

        public ParkingLot? ParkingLot { get; set; }

        public ParkingGate? InGate { get; set; }

        public ParkingGate? OutGate { get; set; }
    }
}