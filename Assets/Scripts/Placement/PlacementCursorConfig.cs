using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Tunable values for a placement cursor: input repeat timing, owner, starting slot and visuals.
    /// Edited only in the Inspector; code never writes to this asset. Readers pick up edits on their next frame.
    /// </summary>
    [CreateAssetMenu(menuName = "Wreckyard/Placement Cursor Config", fileName = "PlacementCursorConfig")]
    public class PlacementCursorConfig : ScriptableObject
    {
        [Header("Input repeat")]
        [SerializeField, Min(0f), Tooltip("Seconds a move key must be held before it starts repeating.")]
        private float initialRepeatDelay = 0.35f;

        [SerializeField, Min(0f), Tooltip("Steps per second while a move key is held after the delay. 0 disables repeating.")]
        private float repeatRate = 12f;

        [Header("Placement")]
        [SerializeField, Min(0), Tooltip("Owner index recorded on every item this cursor places.")]
        private int ownerIndex;

        [SerializeField, Min(0), Tooltip("Catalog slot selected when the cursor starts (0 = first catalog entry).")]
        private int startingSlot;

        [Header("Visuals")]
        [SerializeField, Tooltip("Sprite used for the cursor cell and the ghost cells. Any size or pivot works.")]
        private Sprite cellSprite;

        [SerializeField] private Color cursorColor = new Color(1f, 0.9f, 0.2f, 0.55f);
        [SerializeField] private Color failFlashColor = new Color(1f, 0.15f, 0.15f, 0.95f);

        [SerializeField, Min(0f), Tooltip("Seconds the cursor takes to fade from the fail color back to its normal color.")]
        private float failFlashDuration = 0.25f;

        [SerializeField] private Color ghostValidColor = new Color(0.2f, 0.9f, 0.3f, 0.5f);
        [SerializeField] private Color ghostInvalidColor = new Color(0.95f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private int ghostSortingOrder = 10;
        [SerializeField] private int cursorSortingOrder = 20;

        /// <summary>Seconds a move key must be held before it repeats.</summary>
        public float InitialRepeatDelay => initialRepeatDelay;

        /// <summary>Steps per second while a move key is held after the delay. 0 disables repeating.</summary>
        public float RepeatRate => repeatRate;

        /// <summary>Owner index recorded on placed items. Never negative.</summary>
        public int OwnerIndex => Mathf.Max(0, ownerIndex);

        /// <summary>Catalog slot selected when the cursor starts (0 = first catalog entry).</summary>
        public int StartingSlot => Mathf.Max(0, startingSlot);

        /// <summary>Sprite for the cursor and ghost cells.</summary>
        public Sprite CellSprite => cellSprite;

        /// <summary>Normal color of the cursor cell.</summary>
        public Color CursorColor => cursorColor;

        /// <summary>Cursor color right after a failed placement.</summary>
        public Color FailFlashColor => failFlashColor;

        /// <summary>Seconds the fail flash takes to fade out.</summary>
        public float FailFlashDuration => failFlashDuration;

        /// <summary>Ghost color when the placement check is Ok.</summary>
        public Color GhostValidColor => ghostValidColor;

        /// <summary>Ghost color when the placement check fails.</summary>
        public Color GhostInvalidColor => ghostInvalidColor;

        /// <summary>Sorting order of the ghost cells (placed blocks use 0).</summary>
        public int GhostSortingOrder => ghostSortingOrder;

        /// <summary>Sorting order of the cursor cell.</summary>
        public int CursorSortingOrder => cursorSortingOrder;
    }
}
