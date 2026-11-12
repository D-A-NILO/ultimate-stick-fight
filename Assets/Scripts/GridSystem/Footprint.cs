using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// An item's blocking cells as offsets from its anchor cell: the hitbox plus any reserved travel path.
    /// Effect areas (wind, flames, blast radius) are not part of a footprint.
    /// Each rotation step turns the offsets 90 degrees counter-clockwise around the anchor, which never moves.
    /// </summary>
    [Serializable]
    public class Footprint
    {
        /// <summary>Number of distinct rotation steps (0 to 3).</summary>
        public const int RotationSteps = 4;

        [SerializeField] private List<Vector2Int> offsets = new List<Vector2Int>();

        /// <summary>Creates a footprint. Duplicate offsets are dropped; at least one offset is required.</summary>
        public Footprint(IEnumerable<Vector2Int> offsets)
        {
            if (offsets == null)
            {
                throw new ArgumentNullException(nameof(offsets));
            }

            foreach (Vector2Int offset in offsets)
            {
                if (!this.offsets.Contains(offset))
                {
                    this.offsets.Add(offset);
                }
            }

            if (this.offsets.Count == 0)
            {
                throw new ArgumentException("A footprint needs at least one cell.", nameof(offsets));
            }
        }

        /// <summary>Unrotated offsets from the anchor cell.</summary>
        public IReadOnlyList<Vector2Int> Offsets => offsets;

        /// <summary>Number of offsets.</summary>
        public int CellCount => offsets == null ? 0 : offsets.Count;

        /// <summary>A width x height rectangle whose bottom-left cell is the anchor, for example 3x1.</summary>
        public static Footprint Rectangle(int width, int height)
        {
            return Rectangle(Vector2Int.zero, width, height);
        }

        /// <summary>A width x height rectangle whose bottom-left cell sits at <paramref name="min"/> from the anchor.</summary>
        public static Footprint Rectangle(Vector2Int min, int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be at least 1.");
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be at least 1.");
            }

            var cells = new List<Vector2Int>(width * height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells.Add(min + new Vector2Int(x, y));
                }
            }

            return new Footprint(cells);
        }

        /// <summary>
        /// Fills <paramref name="results"/> with the grid cells covered when the anchor sits at
        /// <paramref name="anchor"/> with the given rotation. The list is cleared first; order follows the offsets.
        /// </summary>
        public void GetCells(Vector2Int anchor, int rotation, List<Vector2Int> results)
        {
            results.Clear();
            if (offsets == null)
            {
                return;
            }

            int steps = NormalizeRotation(rotation);
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int cell = anchor + Rotate(offset, steps);
                if (!results.Contains(cell))
                {
                    results.Add(cell);
                }
            }
        }

        /// <summary>Rotates an offset around the anchor by 90 degrees counter-clockwise per step.</summary>
        public static Vector2Int Rotate(Vector2Int offset, int rotation)
        {
            switch (NormalizeRotation(rotation))
            {
                case 1:
                    return new Vector2Int(-offset.y, offset.x);
                case 2:
                    return new Vector2Int(-offset.x, -offset.y);
                case 3:
                    return new Vector2Int(offset.y, -offset.x);
                default:
                    return offset;
            }
        }

        /// <summary>Wraps any rotation into 0 to 3, so 5 becomes 1 and -1 becomes 3.</summary>
        public static int NormalizeRotation(int rotation)
        {
            return ((rotation % RotationSteps) + RotationSteps) % RotationSteps;
        }
    }
}
