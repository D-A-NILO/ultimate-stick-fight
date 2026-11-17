using System;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Tunable placement rules. Edited only in the Inspector; code never writes to this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Wreckyard/Placement Config", fileName = "PlacementConfig")]
    public class PlacementConfig : ScriptableObject
    {
        /// <summary>Smallest allowed spawn zone radius in cells.</summary>
        public const int MinSpawnZoneRadius = 0;

        /// <summary>Smallest allowed bomb width or height in cells.</summary>
        public const int MinBombSide = 1;

        [SerializeField, Min(MinSpawnZoneRadius)] private int spawnZoneRadius = 3;
        [SerializeField] private Vector2Int bombSize = new Vector2Int(3, 3);

        /// <summary>Raised after any value is edited in the Inspector (including undo and redo).</summary>
        public event Action Changed;

        /// <summary>Cells protected around each spawn point in every direction (a square).</summary>
        public int SpawnZoneRadius => spawnZoneRadius;

        /// <summary>Width and height of the bomb's blast area in cells.</summary>
        public Vector2Int BombSize => bombSize;

        private void OnValidate()
        {
            spawnZoneRadius = Mathf.Max(MinSpawnZoneRadius, spawnZoneRadius);
            bombSize = new Vector2Int(Mathf.Max(MinBombSide, bombSize.x), Mathf.Max(MinBombSide, bombSize.y));
            Changed?.Invoke();
        }
    }
}
