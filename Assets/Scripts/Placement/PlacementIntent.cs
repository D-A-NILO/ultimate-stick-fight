using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// What a player wants the placement cursor to do this frame. Produced by an
    /// <see cref="IPlacementIntentSource"/>, so cursor logic never reads a device.
    /// <c>default(PlacementIntent)</c> is the same as <see cref="None"/>.
    /// </summary>
    public readonly struct PlacementIntent
    {
        /// <summary>Slot index meaning "no slot".</summary>
        public const int NoSlot = -1;

        // Stored as slot + 1 so that default(PlacementIntent) means "no slot" rather than slot 0.
        private readonly int placeSlotPlusOne;

        /// <summary>
        /// Creates an intent. Each move axis is clamped to -1..1, the rotate direction to -1..1, and any
        /// negative slot becomes <see cref="NoSlot"/>.
        /// </summary>
        public PlacementIntent(Vector2Int moveStep, int rotateDirection, int placeSlot, bool bombRequested, bool clearRequested)
        {
            MoveStep = new Vector2Int(Mathf.Clamp(moveStep.x, -1, 1), Mathf.Clamp(moveStep.y, -1, 1));
            RotateDirection = Mathf.Clamp(rotateDirection, -1, 1);
            placeSlotPlusOne = placeSlot < 0 ? 0 : placeSlot + 1;
            BombRequested = bombRequested;
            ClearRequested = clearRequested;
        }

        /// <summary>An intent that does nothing.</summary>
        public static PlacementIntent None => default;

        /// <summary>Cells to move the cursor this frame; each axis is -1, 0 or 1.</summary>
        public Vector2Int MoveStep { get; }

        /// <summary>+1 rotates counter-clockwise (the <see cref="Footprint"/> direction), -1 clockwise, 0 not at all.</summary>
        public int RotateDirection { get; }

        /// <summary>0-based catalog slot to place this frame, or <see cref="NoSlot"/>.</summary>
        public int PlaceSlot => placeSlotPlusOne - 1;

        /// <summary>True if <see cref="PlaceSlot"/> is a slot.</summary>
        public bool HasPlaceSlot => placeSlotPlusOne > 0;

        /// <summary>True if the player wants to bomb the area around the cursor.</summary>
        public bool BombRequested { get; }

        /// <summary>True if the player wants to remove every placed item.</summary>
        public bool ClearRequested { get; }
    }
}
