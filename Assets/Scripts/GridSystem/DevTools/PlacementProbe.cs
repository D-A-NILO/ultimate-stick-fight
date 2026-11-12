using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wreckyard.Core
{
    /// <summary>
    /// TEMPORARY manual test rig (step 2). Serialized test inputs plus context-menu actions that drive a
    /// <see cref="GridOccupancy"/>. Draws the probe footprint green when valid and red when not, and logs
    /// every occupancy event. Works in Edit mode and Play mode. Remove once real placement exists.
    /// </summary>
    [ExecuteAlways]
    public class PlacementProbe : MonoBehaviour
    {
        [SerializeField] private GridOccupancyBehaviour occupancyBehaviour;
        [SerializeField] private List<Vector2Int> footprintCells = new List<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(2, 0)
        };
        [SerializeField] private Vector2Int anchor = new Vector2Int(5, 5);
        [SerializeField, Range(0, Footprint.RotationSteps - 1)] private int rotation;
        [SerializeField, Min(0)] private int ownerIndex;
        [SerializeField] private Color validColor = new Color(0.2f, 0.9f, 0.3f, 0.6f);
        [SerializeField] private Color invalidColor = new Color(0.95f, 0.2f, 0.2f, 0.6f);

        private readonly List<int> placedIds = new List<int>();
        private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>();
        private GridOccupancy subscribedOccupancy;

        private void OnEnable()
        {
            EnsureSubscribed();
        }

        private void OnDisable()
        {
            SubscribeTo(null);
        }

        [ContextMenu("Check")]
        private void Check()
        {
            if (TryGetContext(out GridOccupancy occupancy, out Footprint footprint))
            {
                Log($"Check {Describe()}: {occupancy.CheckPlacement(footprint, anchor, rotation)}");
            }
        }

        [ContextMenu("Place")]
        private void Place()
        {
            if (!TryGetContext(out GridOccupancy occupancy, out Footprint footprint))
            {
                return;
            }

            PlacementResult result = occupancy.TryPlace(footprint, anchor, rotation, ownerIndex, out int itemId);
            if (result == PlacementResult.Ok)
            {
                placedIds.Add(itemId);
                Log($"Place {Describe()}: Ok, id {itemId}");
            }
            else
            {
                Log($"Place {Describe()}: {result}");
            }
        }

        [ContextMenu("Remove Last")]
        private void RemoveLast()
        {
            if (!TryGetContext(out GridOccupancy occupancy, out _))
            {
                return;
            }

            while (placedIds.Count > 0)
            {
                int itemId = placedIds[placedIds.Count - 1];
                placedIds.RemoveAt(placedIds.Count - 1);
                if (occupancy.Remove(itemId))
                {
                    Log($"Remove Last: removed id {itemId}");
                    return;
                }
            }

            Log("Remove Last: no item placed by this probe is left.");
        }

        [ContextMenu("Find Nearest Valid")]
        private void FindNearestValid()
        {
            if (!TryGetContext(out GridOccupancy occupancy, out Footprint footprint))
            {
                return;
            }

            if (!occupancy.TryFindNearestValid(footprint, rotation, anchor, out Vector2Int found))
            {
                Log($"Find Nearest Valid from {anchor}, rotation {rotation}: none found.");
                return;
            }

            Log($"Find Nearest Valid from {anchor}, rotation {rotation}: {found}");
            RecordUndo("Find Nearest Valid");
            anchor = found;
        }

        [ContextMenu("Add Permanent Cell")]
        private void AddPermanentCell()
        {
            if (TryGetContext(out GridOccupancy occupancy, out _))
            {
                bool added = occupancy.MarkPermanent(anchor);
                Log($"Add Permanent Cell at {anchor}: " +
                    (added ? "added" : "rejected (out of bounds, placed item, spawn point or already permanent)"));
            }
        }

        [ContextMenu("Remove Permanent Cell")]
        private void RemovePermanentCell()
        {
            if (TryGetContext(out GridOccupancy occupancy, out _))
            {
                Log($"Remove Permanent Cell at {anchor}: " + (occupancy.UnmarkPermanent(anchor) ? "removed" : "not permanent"));
            }
        }

        [ContextMenu("Add Spawn Point")]
        private void AddSpawnPoint()
        {
            if (TryGetContext(out GridOccupancy occupancy, out _))
            {
                bool added = occupancy.AddSpawnPoint(anchor);
                Log($"Add Spawn Point at {anchor}: " +
                    (added ? "added" : "rejected (out of bounds, permanent, placed item or already a spawn point)"));
            }
        }

        [ContextMenu("Remove Spawn Point")]
        private void RemoveSpawnPoint()
        {
            if (TryGetContext(out GridOccupancy occupancy, out _))
            {
                Log($"Remove Spawn Point at {anchor}: " + (occupancy.RemoveSpawnPoint(anchor) ? "removed" : "no spawn point there"));
            }
        }

        [ContextMenu("Bomb At Anchor")]
        private void BombAtAnchor()
        {
            if (!TryGetContext(out GridOccupancy occupancy, out _))
            {
                return;
            }

            PlacementConfig config = occupancyBehaviour.Config;
            if (config == null)
            {
                LogWarning("Bomb At Anchor: the GridOccupancyBehaviour has no PlacementConfig.");
                return;
            }

            if (!occupancy.Model.IsInBounds(anchor))
            {
                Log($"Bomb At Anchor {anchor}: out of bounds, nothing happens.");
                return;
            }

            IReadOnlyList<int> removedIds = occupancy.Bomb(anchor, config.BombSize);
            Log($"Bomb At Anchor {anchor}, size {config.BombSize}: removed [{string.Join(", ", removedIds)}]");
        }

        [ContextMenu("Clear Placed")]
        private void ClearPlaced()
        {
            if (TryGetContext(out GridOccupancy occupancy, out _))
            {
                occupancy.ClearPlaced();
                placedIds.Clear();
                Log("Clear Placed: done.");
            }
        }

        private void OnDrawGizmos()
        {
            GridOccupancy occupancy = occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;
            if (occupancy == null || footprintCells.Count == 0)
            {
                return;
            }

            EnsureSubscribed();

            var footprint = new Footprint(footprintCells);
            PlacementResult result = occupancy.CheckPlacement(footprint, anchor, rotation);
            footprint.GetCells(anchor, rotation, cellBuffer);

            GridModel model = occupancy.Model;
            Vector3 cellSize = new Vector3(model.CellSize, model.CellSize, 0f);

            Gizmos.color = result == PlacementResult.Ok ? validColor : invalidColor;
            foreach (Vector2Int cell in cellBuffer)
            {
                Gizmos.DrawCube(model.CellToWorldCenter(cell), cellSize);
            }

            Gizmos.DrawWireCube(model.CellToWorldCenter(anchor), cellSize);

#if UNITY_EDITOR
            Handles.Label(model.CellToWorldCorner(anchor + Vector2Int.up), result.ToString());
#endif
        }

        private bool TryGetContext(out GridOccupancy occupancy, out Footprint footprint)
        {
            occupancy = occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;
            footprint = null;

            if (occupancy == null)
            {
                LogWarning("No occupancy yet: assign a GridOccupancyBehaviour whose grid and config are set.");
                return false;
            }

            if (footprintCells.Count == 0)
            {
                LogWarning("The probe footprint has no cells.");
                return false;
            }

            EnsureSubscribed();
            footprint = new Footprint(footprintCells);
            return true;
        }

        private void EnsureSubscribed()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            GridOccupancy current = occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;
            if (current != subscribedOccupancy)
            {
                SubscribeTo(current);
            }
        }

        private void SubscribeTo(GridOccupancy target)
        {
            if (subscribedOccupancy != null)
            {
                subscribedOccupancy.ItemPlaced -= HandleItemPlaced;
                subscribedOccupancy.ItemRemoved -= HandleItemRemoved;
                subscribedOccupancy.PermanentCellRemoved -= HandlePermanentCellRemoved;
                subscribedOccupancy.SpawnPointRemoved -= HandleSpawnPointRemoved;
            }

            subscribedOccupancy = target;

            if (subscribedOccupancy != null)
            {
                subscribedOccupancy.ItemPlaced += HandleItemPlaced;
                subscribedOccupancy.ItemRemoved += HandleItemRemoved;
                subscribedOccupancy.PermanentCellRemoved += HandlePermanentCellRemoved;
                subscribedOccupancy.SpawnPointRemoved += HandleSpawnPointRemoved;
            }
        }

        private void HandleItemPlaced(PlacedItem item)
        {
            Log($"Event ItemPlaced: id {item.Id}, owner {item.OwnerIndex}, anchor {item.Anchor}, rotation {item.Rotation}, {item.Cells.Count} cell(s)");
        }

        private void HandleItemRemoved(PlacedItem item, RemovalReason reason)
        {
            Log($"Event ItemRemoved: id {item.Id} ({reason})");
        }

        private void HandlePermanentCellRemoved(Vector2Int cell, RemovalReason reason)
        {
            Log($"Event PermanentCellRemoved: {cell} ({reason})");
        }

        private void HandleSpawnPointRemoved(Vector2Int cell, RemovalReason reason)
        {
            Log($"Event SpawnPointRemoved: {cell} ({reason})");
        }

        private string Describe()
        {
            return $"anchor {anchor}, rotation {rotation}";
        }

        private void RecordUndo(string actionName)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, actionName);
#endif
        }

        private void Log(string message)
        {
            Debug.Log($"[{nameof(PlacementProbe)}] {message}", this);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[{nameof(PlacementProbe)}] {message}", this);
        }
    }
}
