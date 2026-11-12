using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Immutable grid math. Origin is the world position of the bottom-left corner of cell (0,0),
    /// x grows to the right and y grows up. Cells are Vector2Int (column, row).
    /// A resize produces a new instance.
    /// </summary>
    public sealed class GridModel
    {
        /// <summary>Creates a grid. Columns and rows are clamped to at least 1, cell size to at least 0.1.</summary>
        public GridModel(int columns, int rows, float cellSize, Vector3 origin)
        {
            Columns = Mathf.Max(GridConfig.MinCellCount, columns);
            Rows = Mathf.Max(GridConfig.MinCellCount, rows);
            CellSize = Mathf.Max(GridConfig.MinCellSize, cellSize);
            Origin = origin;
        }

        /// <summary>Number of cells along x.</summary>
        public int Columns { get; }

        /// <summary>Number of cells along y.</summary>
        public int Rows { get; }

        /// <summary>Width and height of one cell in world units.</summary>
        public float CellSize { get; }

        /// <summary>World position of the bottom-left corner of cell (0,0).</summary>
        public Vector3 Origin { get; }

        /// <summary>Total width and height of the grid in world units.</summary>
        public Vector2 WorldSize => new Vector2(Columns * CellSize, Rows * CellSize);

        /// <summary>True if the cell lies inside the grid.</summary>
        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
        }

        /// <summary>World position of the cell's bottom-left corner. Works for out-of-bounds cells too.</summary>
        public Vector3 CellToWorldCorner(Vector2Int cell)
        {
            return new Vector3(Origin.x + cell.x * CellSize, Origin.y + cell.y * CellSize, Origin.z);
        }

        /// <summary>World position of the cell's center. Works for out-of-bounds cells too.</summary>
        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            float halfCell = CellSize * 0.5f;
            return CellToWorldCorner(cell) + new Vector3(halfCell, halfCell, 0f);
        }

        /// <summary>
        /// Cell that contains the world point (z is ignored). The result may be out of bounds;
        /// check it with <see cref="IsInBounds"/>.
        /// </summary>
        public Vector2Int WorldToCell(Vector3 worldPoint)
        {
            return new Vector2Int(
                Mathf.FloorToInt((worldPoint.x - Origin.x) / CellSize),
                Mathf.FloorToInt((worldPoint.y - Origin.y) / CellSize));
        }

        /// <summary>Center of the cell that contains the world point. Not clamped to the grid.</summary>
        public Vector3 SnapToCellCenter(Vector3 worldPoint)
        {
            return CellToWorldCenter(WorldToCell(worldPoint));
        }
    }
}
