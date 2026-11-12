namespace Wreckyard.Core
{
    /// <summary>Why something was removed from the grid occupancy.</summary>
    public enum RemovalReason
    {
        /// <summary>Removed by a direct call (Remove, UnmarkPermanent, RemoveSpawnPoint).</summary>
        Direct,

        /// <summary>Destroyed by a bomb.</summary>
        Bomb,

        /// <summary>Removed by ClearPlaced.</summary>
        Cleared,

        /// <summary>No longer fits after the grid shrank.</summary>
        Resized
    }
}
