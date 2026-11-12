using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>Immutable record of an item placed on the grid.</summary>
    public sealed class PlacedItem
    {
        internal PlacedItem(int id, int ownerIndex, Vector2Int anchor, int rotation, Vector2Int[] cells)
        {
            Id = id;
            OwnerIndex = ownerIndex;
            Anchor = anchor;
            Rotation = rotation;
            Cells = Array.AsReadOnly(cells);
        }

        /// <summary>Unique id, never reused by the same occupancy.</summary>
        public int Id { get; }

        /// <summary>Index of the player who placed the item.</summary>
        public int OwnerIndex { get; }

        /// <summary>Anchor cell the footprint was placed at.</summary>
        public Vector2Int Anchor { get; }

        /// <summary>Rotation step, 0 to 3.</summary>
        public int Rotation { get; }

        /// <summary>Grid cells the item blocks.</summary>
        public IReadOnlyList<Vector2Int> Cells { get; }
    }
}
