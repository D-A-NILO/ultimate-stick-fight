using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// TEMPORARY manual test rig (step 2). Colors Permanent, Placed (tinted by owner) and SpawnZone cells
    /// in the Scene view. Remove once real placement visuals exist.
    /// </summary>
    [RequireComponent(typeof(GridOccupancyBehaviour))]
    public class GridOccupancyGizmoDrawer : MonoBehaviour
    {
        [SerializeField] private bool showOccupancy = true;
        [SerializeField] private Color permanentColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color spawnZoneColor = new Color(1f, 0.85f, 0.2f, 0.25f);
        [SerializeField] private Color spawnPointColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color[] ownerColors =
        {
            new Color(0.9f, 0.25f, 0.25f, 0.7f),
            new Color(0.25f, 0.5f, 0.95f, 0.7f),
            new Color(0.3f, 0.85f, 0.35f, 0.7f),
            new Color(0.85f, 0.35f, 0.85f, 0.7f)
        };
        [SerializeField, Range(0f, 1f)] private float cellFill = 0.9f;

        private GridOccupancyBehaviour occupancyBehaviour;

        private void OnDrawGizmos()
        {
            if (!showOccupancy)
            {
                return;
            }

            if (occupancyBehaviour == null)
            {
                occupancyBehaviour = GetComponent<GridOccupancyBehaviour>();
            }

            GridOccupancy occupancy = occupancyBehaviour.Occupancy;
            if (occupancy == null)
            {
                return;
            }

            GridModel model = occupancy.Model;
            Vector3 cubeSize = new Vector3(model.CellSize, model.CellSize, 0f) * cellFill;

            for (int row = 0; row < model.Rows; row++)
            {
                for (int column = 0; column < model.Columns; column++)
                {
                    var cell = new Vector2Int(column, row);
                    CellInfo info = occupancy.GetCell(cell);
                    if (info.State == CellState.Empty)
                    {
                        continue;
                    }

                    Gizmos.color = ColorFor(info);
                    Gizmos.DrawCube(model.CellToWorldCenter(cell), cubeSize);
                }
            }

            Gizmos.color = spawnPointColor;
            foreach (Vector2Int spawnPoint in occupancy.SpawnPoints)
            {
                Gizmos.DrawWireSphere(model.CellToWorldCenter(spawnPoint), model.CellSize * 0.5f);
            }
        }

        private Color ColorFor(CellInfo info)
        {
            switch (info.State)
            {
                case CellState.Placed:
                    return ownerColors.Length == 0 ? Color.white : ownerColors[info.OwnerIndex % ownerColors.Length];
                case CellState.Permanent:
                    return permanentColor;
                default:
                    return spawnZoneColor;
            }
        }
    }
}
