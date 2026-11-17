namespace Wreckyard.Core
{
    /// <summary>
    /// Outcome of a placement check. Failures are ordered by priority: when cells fail for different
    /// reasons, the lowest value after Ok is reported.
    /// </summary>
    public enum PlacementResult
    {
        /// <summary>Every footprint cell is free.</summary>
        Ok,

        /// <summary>At least one footprint cell is outside the grid.</summary>
        OutOfBounds,

        /// <summary>A footprint cell overlaps a placed item.</summary>
        BlockedByPlaced,

        /// <summary>A footprint cell overlaps a permanent platform or trap.</summary>
        BlockedByPermanent,

        /// <summary>A footprint cell is inside a spawn zone.</summary>
        InSpawnZone
    }
}
