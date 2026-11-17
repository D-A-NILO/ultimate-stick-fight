using System;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Tunable grid dimensions. Edited only in the Inspector; code never writes to this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Wreckyard/Grid Config", fileName = "GridConfig")]
    public class GridConfig : ScriptableObject
    {
        /// <summary>Smallest allowed number of columns or rows.</summary>
        public const int MinCellCount = 1;

        /// <summary>Smallest allowed cell size in world units.</summary>
        public const float MinCellSize = 0.1f;

        [SerializeField, Min(MinCellCount)] private int columns = 35;
        [SerializeField, Min(MinCellCount)] private int rows = 19;
        [SerializeField, Min(MinCellSize)] private float cellSize = 1f;

        /// <summary>Raised after any value is edited in the Inspector (including undo and redo).</summary>
        public event Action Changed;

        /// <summary>Number of cells along x.</summary>
        public int Columns => columns;

        /// <summary>Number of cells along y.</summary>
        public int Rows => rows;

        /// <summary>Width and height of one cell in world units.</summary>
        public float CellSize => cellSize;

        private void OnValidate()
        {
            columns = Mathf.Max(MinCellCount, columns);
            rows = Mathf.Max(MinCellCount, rows);
            cellSize = Mathf.Max(MinCellSize, cellSize);
            Changed?.Invoke();
        }
    }
}
