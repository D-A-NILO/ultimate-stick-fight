namespace Wreckyard.Core
{
    /// <summary>Snapshot of one grid cell.</summary>
    public readonly struct CellInfo
    {
        internal CellInfo(CellState state, int itemId, int ownerIndex, bool isInSpawnZone)
        {
            State = state;
            ItemId = itemId;
            OwnerIndex = ownerIndex;
            IsInSpawnZone = isInSpawnZone;
        }

        /// <summary>Highest-priority state of the cell: Placed, then Permanent, then SpawnZone.</summary>
        public CellState State { get; }

        /// <summary>Id of the placed item, or <see cref="GridOccupancy.NoItem"/>.</summary>
        public int ItemId { get; }

        /// <summary>Owner of the placed item, or <see cref="GridOccupancy.NoOwner"/>.</summary>
        public int OwnerIndex { get; }

        /// <summary>True if the cell is inside any spawn zone, even when another state wins.</summary>
        public bool IsInSpawnZone { get; }
    }
}
