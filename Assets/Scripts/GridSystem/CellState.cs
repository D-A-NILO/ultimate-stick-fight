namespace Wreckyard.Core
{
    /// <summary>What occupies a grid cell, reported in priority order Placed, Permanent, SpawnZone.</summary>
    public enum CellState
    {
        /// <summary>Nothing blocks the cell.</summary>
        Empty,

        /// <summary>A starter-map platform or trap.</summary>
        Permanent,

        /// <summary>An item placed by a player.</summary>
        Placed,

        /// <summary>Inside a spawn point's no-place zone.</summary>
        SpawnZone
    }
}
