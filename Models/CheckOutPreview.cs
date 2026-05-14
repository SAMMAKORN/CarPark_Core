namespace CarPark.Models
{
    public record CheckOutPreview(
        ParkingTransaction Transaction,
        List<ParkingRateCondition> ApplicableConditions,
        decimal NormalCharge
    );
}