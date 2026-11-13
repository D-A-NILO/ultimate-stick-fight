using System;
using System.Collections.Generic;
using UnityEngine;
using Wreckyard.Core;

namespace Wreckyard.Gameplay
{
    /// <summary>
    /// One player's placement cursor. Owns the cursor cell (the footprint anchor), the requested rotation and
    /// the selected catalog slot, reads one <see cref="PlacementIntent"/> per frame from its source and applies
    /// it to the scene's <see cref="GridOccupancy"/>. Starts at the grid center with the config's starting
    /// slot selected, and keeps the cell inside the grid when it resizes. Play mode only.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacementCursor : MonoBehaviour
    {
        [SerializeField] private GridBehaviour grid;
        [SerializeField] private GridOccupancyBehaviour occupancyBehaviour;
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private PlacementCursorConfig config;

        [SerializeField, Tooltip("A component that implements IPlacementIntentSource. Leave empty to use the first one on this GameObject.")]
        private MonoBehaviour intentSource;

        private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>();
        private GridBehaviour subscribedGrid;
        private IPlacementIntentSource source;
        private MonoBehaviour sourceBehaviour;
        private bool hasStarted;
        private bool warnedMissingSource;

        /// <summary>Raised after a placement attempt that placed nothing (blocked, out of bounds or a bad slot).</summary>
        public event Action PlacementFailed;

        /// <summary>
        /// True once the grid has a model and the occupancy exists. The first time a model exists the cursor
        /// moves to the grid center and selects the starting slot.
        /// </summary>
        public bool IsReady => EnsureStarted() && Occupancy != null;

        /// <summary>Cursor cell and footprint anchor. Always inside the grid once <see cref="IsReady"/>.</summary>
        public Vector2Int Cell { get; private set; }

        /// <summary>Requested rotation step, 0 to 3. Items that cannot rotate use 0 instead.</summary>
        public int Rotation { get; private set; }

        /// <summary>Selected catalog slot (0-based), or <see cref="PlacementIntent.NoSlot"/>.</summary>
        public int SelectedSlot { get; private set; } = PlacementIntent.NoSlot;

        /// <summary>Catalog the slots index into.</summary>
        public ItemCatalog Catalog => catalog;

        /// <summary>Tunable cursor values.</summary>
        public PlacementCursorConfig Config => config;

        /// <summary>The grid's current model, or null.</summary>
        public GridModel Model => grid != null ? grid.Model : null;

        private GridOccupancy Occupancy => occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;

        /// <summary>
        /// Applies one intent in this order: move, rotate, clear, bomb, place. Does nothing until the grid has a model.
        /// </summary>
        public void Apply(PlacementIntent intent)
        {
            if (!EnsureStarted())
            {
                return;
            }

            if (intent.MoveStep != Vector2Int.zero)
            {
                Cell = ClampToGrid(Cell + intent.MoveStep, Model);
            }

            if (intent.RotateDirection != 0)
            {
                Rotation = Footprint.NormalizeRotation(Rotation + intent.RotateDirection);
            }

            if (intent.ClearRequested)
            {
                Clear();
            }

            if (intent.BombRequested)
            {
                Bomb();
            }

            if (intent.HasPlaceSlot)
            {
                Place(intent.PlaceSlot);
            }
        }

        /// <summary>Gets the definition in the selected slot. False when no slot is selected or the slot is empty.</summary>
        public bool TryGetSelectedDefinition(out ItemDefinition definition)
        {
            return TryGetDefinition(SelectedSlot, out definition, out _);
        }

        /// <summary>
        /// Fills <paramref name="cells"/> with the selected item's footprint at <see cref="Cell"/> and its allowed
        /// rotation, and returns the occupancy's placement check for it. False (with an empty list) when the
        /// cursor is not ready or nothing valid is selected.
        /// </summary>
        public bool TryGetPreview(List<Vector2Int> cells, out PlacementResult result)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            cells.Clear();
            result = PlacementResult.Ok;

            if (!IsReady || !TryGetDefinition(SelectedSlot, out ItemDefinition definition, out _)
                || !definition.TryCreateFootprint(out Footprint footprint))
            {
                return false;
            }

            int rotation = definition.GetAllowedRotation(Rotation);
            footprint.GetCells(Cell, rotation, cells);
            result = Occupancy.CheckPlacement(footprint, Cell, rotation);
            return true;
        }

        private void OnEnable()
        {
            if (grid == null || occupancyBehaviour == null || catalog == null || config == null)
            {
                Debug.LogWarning($"{nameof(PlacementCursor)} on '{name}' needs a {nameof(GridBehaviour)}, a {nameof(GridOccupancyBehaviour)}, an {nameof(ItemCatalog)} and a {nameof(PlacementCursorConfig)}.", this);
            }

            SubscribeToGrid(grid);
            ResolveSource();
            EnsureStarted();
        }

        private void OnDisable()
        {
            SubscribeToGrid(null);
        }

        private void OnValidate()
        {
            if (intentSource != null && !(intentSource is IPlacementIntentSource))
            {
                Debug.LogWarning($"{nameof(PlacementCursor)} on '{name}': '{intentSource.GetType().Name}' does not implement {nameof(IPlacementIntentSource)}, so it was cleared.", this);
                intentSource = null;
            }

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            if (grid != subscribedGrid)
            {
                SubscribeToGrid(grid);
            }

            ResolveSource();
        }

        private void Update()
        {
            if (!EnsureStarted())
            {
                return;
            }

            if (sourceBehaviour == null)
            {
                ResolveSource();
                if (sourceBehaviour == null)
                {
                    return;
                }
            }

            if (sourceBehaviour.isActiveAndEnabled)
            {
                Apply(source.ReadIntent());
            }
        }

        private void HandleGridResized(GridModel model)
        {
            if (hasStarted)
            {
                Cell = ClampToGrid(Cell, model);
            }
            else
            {
                EnsureStarted();
            }
        }

        private bool EnsureStarted()
        {
            GridModel model = Model;
            if (model == null)
            {
                return false;
            }

            if (!hasStarted)
            {
                hasStarted = true;
                Cell = new Vector2Int(model.Columns / 2, model.Rows / 2);
                SelectedSlot = config != null ? config.StartingSlot : PlacementIntent.NoSlot;
            }

            return true;
        }

        private void Place(int slot)
        {
            if (!TryGetDefinition(slot, out ItemDefinition definition, out string problem))
            {
                Fail($"Slot {slot + 1}: {problem}");
                return;
            }

            SelectedSlot = slot;

            GridOccupancy occupancy = Occupancy;
            if (occupancy == null || config == null)
            {
                Fail($"Place {definition.name}: needs an occupancy and a {nameof(PlacementCursorConfig)}.");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                Fail($"Place {definition.name}: the definition has an empty id.");
                return;
            }

            if (!definition.TryCreateFootprint(out Footprint footprint))
            {
                Fail($"Place {definition.name}: the definition has a footprint with no cells.");
                return;
            }

            int rotation = definition.GetAllowedRotation(Rotation);
            PlacementResult result = occupancy.TryPlace(footprint, Cell, rotation, config.OwnerIndex, definition.Id, out _);
            if (result != PlacementResult.Ok)
            {
                Fail($"Place {definition.Id} at {Cell}, rotation {rotation}: {result}");
            }
        }

        private void Bomb()
        {
            GridOccupancy occupancy = Occupancy;
            PlacementConfig placementConfig = occupancyBehaviour != null ? occupancyBehaviour.Config : null;
            if (occupancy == null || placementConfig == null)
            {
                Debug.LogWarning($"{nameof(PlacementCursor)} on '{name}': bomb needs an occupancy with a {nameof(PlacementConfig)}.", this);
                return;
            }

            occupancy.Bomb(Cell, placementConfig.BombSize);
        }

        private void Clear()
        {
            GridOccupancy occupancy = Occupancy;
            if (occupancy == null)
            {
                Debug.LogWarning($"{nameof(PlacementCursor)} on '{name}': clear needs an occupancy.", this);
                return;
            }

            occupancy.ClearPlaced();
        }

        private void Fail(string reason)
        {
            Debug.Log($"[{nameof(PlacementCursor)}] {reason}", this);
            PlacementFailed?.Invoke();
        }

        private bool TryGetDefinition(int slot, out ItemDefinition definition, out string problem)
        {
            definition = null;
            if (catalog == null)
            {
                problem = "no catalog assigned.";
                return false;
            }

            if (slot < 0 || slot >= catalog.Count)
            {
                problem = $"outside the catalog (it has {catalog.Count} entries).";
                return false;
            }

            definition = catalog.Items[slot];
            if (definition == null)
            {
                problem = "the catalog slot is empty.";
                return false;
            }

            problem = null;
            return true;
        }

        private static Vector2Int ClampToGrid(Vector2Int cell, GridModel model)
        {
            return new Vector2Int(
                Mathf.Clamp(cell.x, 0, model.Columns - 1),
                Mathf.Clamp(cell.y, 0, model.Rows - 1));
        }

        private void ResolveSource()
        {
            sourceBehaviour = null;
            source = null;

            if (intentSource != null && intentSource is IPlacementIntentSource assigned)
            {
                source = assigned;
                sourceBehaviour = intentSource;
            }
            else if (GetComponent<IPlacementIntentSource>() is MonoBehaviour found)
            {
                source = (IPlacementIntentSource)found;
                sourceBehaviour = found;
            }

            if (sourceBehaviour == null && !warnedMissingSource)
            {
                warnedMissingSource = true;
                Debug.LogWarning($"{nameof(PlacementCursor)} on '{name}' has no {nameof(IPlacementIntentSource)}; it will not react to input.", this);
            }
            else if (sourceBehaviour != null)
            {
                warnedMissingSource = false;
            }
        }

        private void SubscribeToGrid(GridBehaviour target)
        {
            if (subscribedGrid is not null)
            {
                subscribedGrid.Resized -= HandleGridResized;
            }

            subscribedGrid = target;

            if (subscribedGrid is not null)
            {
                subscribedGrid.Resized += HandleGridResized;
            }
        }
    }
}
