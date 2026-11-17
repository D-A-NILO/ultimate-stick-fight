using System;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// One rectangle of an item's footprint, in cell offsets from the anchor cell. An item definition
    /// combines one or more of these, so shapes like "1x1 plus a track" can be built in the Inspector.
    /// </summary>
    [Serializable]
    public struct FootprintRect
    {
        /// <summary>Smallest allowed width or height in cells.</summary>
        public const int MinSide = 1;

        [SerializeField] private Vector2Int min;
        [SerializeField] private Vector2Int size;

        /// <summary>Creates a rectangle. Sizes below <see cref="MinSide"/> are clamped.</summary>
        public FootprintRect(Vector2Int min, Vector2Int size)
        {
            this.min = min;
            this.size = ClampSize(size);
        }

        /// <summary>Offset of the bottom-left cell from the anchor cell.</summary>
        public Vector2Int Min => min;

        /// <summary>Width (x) and height (y) in cells.</summary>
        public Vector2Int Size => size;

        /// <summary>Returns <paramref name="size"/> with both sides at least <see cref="MinSide"/>.</summary>
        public static Vector2Int ClampSize(Vector2Int size)
        {
            return new Vector2Int(Mathf.Max(MinSide, size.x), Mathf.Max(MinSide, size.y));
        }
    }
}
