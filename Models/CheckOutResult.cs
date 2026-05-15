namespace CarPark.Models
{
    public record CheckOutResult(
        ParkingTransaction Transaction,
        bool QuotaExceeded = false,
        string? QuotaMessage = null
    );
}