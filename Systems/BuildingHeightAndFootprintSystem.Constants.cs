namespace BuildingHeightAndFootprint.Systems
{
    internal static class Units
    {
        // Meters ↔ Feet
        public const float MetersToFeet = 3.28084f;

        // Square meters per acre (used as divisor: acres = m^2 / SquareMetersPerAcre)
        public const float SquareMetersPerAcre = 4046.8564224f;

        // Size of a zoning cell in meters (used when estimating footprint cells)
        public const float ZoningCellMeters = 8f;
    }
}