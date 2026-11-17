using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wreckyard.Core
{
    /// <summary>
    /// TEMPORARY manual test rig (steps 2 and 3). Serialized test inputs plus context-menu actions that drive a
    /// <see cref="GridOccupancy"/>. With Use Definition on, the footprint, allowed rotation and definition id
    /// come from the selected <see cref="ItemDefinition"/> in the catalog; with it off, the raw footprint cells
    /// are used and items are placed without a definition. Draws the probe footprint green when valid and red
    /// when not, and logs every occupancy event. Works in Edit mode and Play mode. Remove once real placement exists.
    /// </summary>
    [ExecuteAlways]
    public class PlacementProbe : MonoBehaviour
    {
        [SerializeField] private GridOccupancyBehaviour occupancyBehaviour;
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private bool useDefinition = true;
        [SerializeField, Min(0)] private int definitionIndex;
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

        [ContextMenu("Select Next Definition")]
        private void SelectNextDefinition()
        {
            StepDefinition(1);
        }

        [ContextMenu("Select Previous Definition")]
        private void SelectPreviousDefinition()
        {
            StepDefinition(-1);
        }

        [ContextMenu("Check")]
        private void Check()
        {
            if (TryGetContext(out GridOccupancy occupancy, out Footprint footprint, out int placeRotation, out ItemDefinition definition))
            {
                Log($"Check {Describe(definition, placeRotation)}: {occupancy.CheckPlacement(footprint, anchor, placeRotation)}");
            }
        }

        [ContextMenu("Place")]
        private void Place()
        {
            if (!TryGetContext(out GridOccupancy occupancy, out Footprint footprint, out int placeRotation, out ItemDefinition definition))
            {
                return;
            }

            string definitionId = definition != null ? definition.Id : null;
            PlacementResult result = occupancy.TryPlace(footprint, anchor, placeRotation, ownerIndex, definitionId, out int itemId);
            if (result == PlacementResult.Ok)
            {
                placedIds.Add(itemId);
                Log($"Place {Describe(definition, placeRotation)}: Ok, id {itemId}");
            }
            else
            {
                Log($"Place {Describe(definition, placeRotation)}: {result}");
            }
        }

        [ContextMenu("Remove Last")]
        private void RemoveLast()
        {
            if (!TryGetOccupancy(out GridOccupancy occupancy))
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
            if (!TryGetContext(out GridOccupancy occupancy, out Footprint footprint, out int placeRotation, out _))
            {
                return;
            }

            if (!occupancy.TryFindNearestValid(footprint, placeRotation, anchor, out Vector2Int found))
            {
                Log($"Find Nearest Valid from {anchor}, rotation {placeRotation}: none found.");
                return;
            }

            Log($"Find Nearest Valid from {anchor}, rotation {placeRotation}: {found}");
            RecordUndo("Find Nearest Valid");
            anchor = found;
        }

        [ContextMenu("Add Permanent Cell")]
        private void AddPermanentCell()
        {
            if (TryGetOccupancy(out GridOccupancy occupancy))
            {
                bool added = occupancy.MarkPermanent(anchor);
                Log($"Add Permanent Cell at {anchor}: " +
                    (added ? "added" : "rejected (out of bounds, placed item, spawn point or already permanent)"));
            }
        }

        [ContextMenu("Remove Permanent Cell")]
        private void RemovePermanentCell()
        {
            if (TryGetOccupancy(out GridOccupancy occupancy))
            {
                Log($"Remove Permanent Cell at {anchor}: " + (occupancy.UnmarkPermanent(anchor) ? "removed" : "not permanent"));
            }
        }

        [ContextMenu("Add Spawn Point")]
        private void AddSpawnPoint()
        {
            if (TryGetOccupancy(out GridOccupancy occupancy))
            {
                bool added = occupancy.AddSpawnPoint(anchor);
                Log($"Add Spawn Point at {anchor}: " +
                    (added ? "added" : "rejected (out of bounds, permanent, placed item or already a spawn point)"));
            }
        }

        [ContextMenu("Remove Spawn Point")]
        private void RemoveSpawnPoint()
        {
            if (TryGetOccupancy(out GridOccupancy occupancy))
            {
                Log($"Remove Spawn Point at {anchor}: " + (occupancy.RemoveSpawnPoint(anchor) ? "removed" : "no spawn point there"));
            }
        }

        [ContextMenu("Bomb At Anchor")]
        private void BombAtAnchor()
        {
            if (!TryGetOccupancy(out GridOccupancy occupancy))
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
            if (TryGetOccupancy(out GridOccupancy occupancy))
            {
                occupancy.ClearPlaced();
                placedIds.Clear();
                Log("Clear Placed: done.");
            }
        }

        private void OnDrawGizmos()
        {
            GridOccupancy occupancy = occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;
            if (occupancy == null)
            {
                return;
            }

            EnsureSubscribed();

            if (!TryGetShape(out Footprint footprint, out int placeRotation, out ItemDefinition definition, out _))
            {
                return;
            }

            PlacementResult result = occupancy.CheckPlacement(footprint, anchor, placeRotation);
            footprint.GetCells(anchor, placeRotation, cellBuffer);

            GridModel model = occupancy.Model;
            Vector3 cellSize = new Vector3(model.CellSize, model.CellSize, 0f);

            Gizmos.color = result == PlacementResult.Ok ? validColor : invalidColor;
            foreach (Vector2Int cell in cellBuffer)
            {
                Gizmos.DrawCube(model.CellToWorldCenter(cell), cellSize);
            }

            Gizmos.DrawWireCube(model.CellToWorldCenter(anchor), cellSize);

#if UNITY_EDITOR
            string label = definition != null ? $"{definition.DisplayName}: {result}" : result.ToString();
            Handles.Label(model.CellToWorldCorner(anchor + Vector2Int.up), label);
#endif
        }

        private void StepDefinition(int step)
        {
            if (catalog == null || catalog.Count == 0)
            {
                LogWarning("Select Definition: assign a catalog with at least one entry.");
                return;
            }

            RecordUndo("Select Definition");
            int count = catalog.Count;
            definitionIndex = ((definitionIndex + step) % count + count) % count;

            ItemDefinition definition = catalog.Items[definitionIndex];
            Log(definition != null
                ? $"Selected definition {definitionIndex}: {definition.DisplayName} ({definition.Id})"
                : $"Selected definition {definitionIndex}: empty catalog slot");
        }

        private bool TryGetOccupancy(out GridOccupancy occupancy)
        {
            occupancy = occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;
            if (occupancy == null)
            {
                LogWarning("No occupancy yet: assign a GridOccupancyBehaviour whose grid and config are set.");
                return false;
            }

            EnsureSubscribed();
            return true;
        }

        private bool TryGetContext(out GridOccupancy occupancy, out Footprint footprint, out int placeRotation, out ItemDefinition definition)
        {
            footprint = null;
            placeRotation = 0;
            definition = null;

            if (!TryGetOccupancy(out occupancy))
            {
                return false;
            }

            if (!TryGetShape(out footprint, out placeRotation, out definition, out string problem))
            {
                LogWarning(problem);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Footprint, rotation and definition the probe would place right now. Definition is null when
        /// Use Definition is off.
        /// </summary>
        private bool TryGetShape(out Footprint footprint, out int placeRotation, out ItemDefinition definition, out string problem)
        {
            footprint = null;
            placeRotation = Footprint.NormalizeRotation(rotation);
            definition = null;
            problem = null;

            if (!useDefinition)
            {
                if (footprintCells.Count == 0)
                {
                    problem = "The probe footprint has no cells.";
                    return false;
                }

                footprint = new Footprint(footprintCells);
                return true;
            }

            if (catalog == null || catalog.Count == 0)
            {
                problem = "Use Definition is on, but no catalog (or an empty one) is assigned.";
                return false;
            }

            if (definitionIndex >= catalog.Count)
            {
                problem = $"Definition Index {definitionIndex} is outside the catalog (0 to {catalog.Count - 1}).";
                return false;
            }

            definition = catalog.Items[definitionIndex];
            if (definition == null)
            {
                problem = $"Catalog slot {definitionIndex} is empty.";
                return false;
            }

            if (!definition.TryCreateFootprint(out footprint))
            {
                problem = $"Definition '{definition.name}' has a footprint with no cells.";
                definition = null;
                return false;
            }

            placeRotation = definition.GetAllowedRotation(rotation);
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
            Log($"Event ItemPlaced: id {item.Id}, definition {item.DefinitionId ?? "none"}, owner {item.OwnerIndex}, anchor {item.Anchor}, rotation {item.Rotation}, {item.Cells.Count} cell(s)");
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

        private string Describe(ItemDefinition definition, int placeRotation)
        {
            string what = definition != null ? $"{definition.Id} at " : string.Empty;
            string forced = placeRotation != Footprint.NormalizeRotation(rotation) ? " (forced: definition cannot rotate)" : string.Empty;
            return $"{what}anchor {anchor}, rotation {placeRotation}{forced}";
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
