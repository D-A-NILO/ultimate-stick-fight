using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wreckyard.Core
{
    /// <summary>
    /// Draws the grid's cell lines and a thicker outer border in the Scene view.
    /// </summary>
    [RequireComponent(typeof(GridBehaviour))]
    public class GridGizmoDrawer : MonoBehaviour
    {
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color lineColor = Color.white;
        // Only read by editor-only Handles code, so player builds would flag it as unused.
#pragma warning disable CS0414
        [SerializeField, Min(0f)] private float borderThickness = 3f;
#pragma warning restore CS0414

        private GridBehaviour grid;

        private void OnDrawGizmos()
        {
            if (!showGrid)
            {
                return;
            }

            if (grid == null)
            {
                grid = GetComponent<GridBehaviour>();
            }

            GridModel model = grid.Model;
            if (model == null)
            {
                return;
            }

            Vector2 size = model.WorldSize;
            Vector3 up = Vector3.up * size.y;
            Vector3 right = Vector3.right * size.x;

            Gizmos.color = lineColor;
            for (int column = 1; column < model.Columns; column++)
            {
                Vector3 bottom = model.CellToWorldCorner(new Vector2Int(column, 0));
                Gizmos.DrawLine(bottom, bottom + up);
            }

            for (int row = 1; row < model.Rows; row++)
            {
                Vector3 left = model.CellToWorldCorner(new Vector2Int(0, row));
                Gizmos.DrawLine(left, left + right);
            }

            DrawBorder(model.Origin, right, up);
        }

        private void DrawBorder(Vector3 bottomLeft, Vector3 right, Vector3 up)
        {
            Vector3 bottomRight = bottomLeft + right;
            Vector3 topLeft = bottomLeft + up;
            Vector3 topRight = bottomRight + up;

#if UNITY_EDITOR
            using (new Handles.DrawingScope(lineColor))
            {
                Handles.DrawLine(bottomLeft, bottomRight, borderThickness);
                Handles.DrawLine(bottomRight, topRight, borderThickness);
                Handles.DrawLine(topRight, topLeft, borderThickness);
                Handles.DrawLine(topLeft, bottomLeft, borderThickness);
            }
#else
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
#endif
        }
    }
}
