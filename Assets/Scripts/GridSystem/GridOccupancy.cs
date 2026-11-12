using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// What is in each grid cell and whether an item can be placed. Data is keyed by (column, row),
    /// so a grid resize keeps everything that still fits.
    /// </summary>
    public sealed class GridOccupancy
    {
        /// <summary>Item id meaning "no item".</summary>
        public const int NoItem = 0;

        /// <summary>Owner index meaning "no owner".</summary>
        public const int NoOwner = -1;

        private readonly HashSet<Vector2Int> permanentCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> spawnPoints = new HashSet<Vector2Int>();
        private readonly Dictionary<int, PlacedItem> items = new Dictionary<int, PlacedItem>();
        private readonly Dictionary<Vector2Int, int> itemIdByCell = new Dictionary<Vector2Int, int>();
        private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>();
        private readonly List<Vector2Int> offsetBuffer = new List<Vector2Int>();
        private int spawnZoneRadius;
        private int nextItemId = NoItem + 1;

        /// <summary>Creates an empty occupancy for the given grid.</summary>
        public GridOccupancy(GridModel model, int spawnZoneRadius)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            SpawnZoneRadius = spawnZoneRadius;
        }

        /// <summary>Raised after an item is placed.</summary>
        public event Action<PlacedItem> ItemPlaced;

        /// <summary>Raised after an item is removed.</summary>
        public event Action<PlacedItem, RemovalReason> ItemRemoved;

        /// <summary>Raised after a permanent cell is removed.</summary>
        public event Action<Vector2Int, RemovalReason> PermanentCellRemoved;

        /// <summary>Raised after a spawn point is removed.</summary>
        public event Action<Vector2Int, RemovalReason> SpawnPointRemoved;

        /// <summary>The grid this occupancy is checked against.</summary>
        public GridModel Model { get; private set; }

        /// <summary>Cells protected around each spawn point in every direction (a square). At least 0.</summary>
        public int SpawnZoneRadius
        {
            get => spawnZoneRadius;
            set => spawnZoneRadius = Mathf.Max(PlacementConfig.MinSpawnZoneRadius, value);
        }

        /// <summary>All permanent cells.</summary>
        public IReadOnlyCollection<Vector2Int> PermanentCells => permanentCells;

        /// <summary>All spawn points.</summary>
        public IReadOnlyCollection<Vector2Int> SpawnPoints => spawnPoints;

        /// <summary>Number of placed items.</summary>
        public int ItemCount => items.Count;

        /// <summary>
        /// Switches to a new grid. Placed items with any cell outside it, permanent cells and spawn points
        /// that no longer fit are removed with <see cref="RemovalReason.Resized"/> and one logged warning.
        /// </summary>
        public void ApplyModel(GridModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            bool shrank = model.Columns < Model.Columns || model.Rows < Model.Rows;
            Model = model;
            if (!shrank)
            {
                return;
            }

            var removedItemIds = new List<int>();
            foreach (PlacedItem item in items.Values)
            {
                if (!AllInBounds(item.Cells))
                {
                    removedItemIds.Add(item.Id);
                }
            }

            removedItemIds.Sort();
            List<Vector2Int> removedPermanent = CollectOutOfBounds(permanentCells);
            List<Vector2Int> removedSpawns = CollectOutOfBounds(spawnPoints);

            if (removedItemIds.Count == 0 && removedPermanent.Count == 0 && removedSpawns.Count == 0)
            {
                return;
            }

            foreach (int id in removedItemIds)
            {
                RemoveItem(id, RemovalReason.Resized);
            }

            foreach (Vector2Int cell in removedPermanent)
            {
                permanentCells.Remove(cell);
                PermanentCellRemoved?.Invoke(cell, RemovalReason.Resized);
            }

            foreach (Vector2Int cell in removedSpawns)
            {
                spawnPoints.Remove(cell);
                SpawnPointRemoved?.Invoke(cell, RemovalReason.Resized);
            }

            Debug.LogWarning(
                $"Grid shrank to {model.Columns}x{model.Rows}: removed {removedItemIds.Count} placed item(s)" +
                $" [{string.Join(", ", removedItemIds)}], {removedPermanent.Count} permanent cell(s) and" +
                $" {removedSpawns.Count} spawn point(s) that no longer fit.");
        }

        /// <summary>
        /// Marks a cell as a permanent platform or trap. Returns false if the cell is out of bounds,
        /// holds a placed item or a spawn point, or is already permanent. Spawn zones are allowed.
        /// </summary>
        public bool MarkPermanent(Vector2Int cell)
        {
            if (!Model.IsInBounds(cell) || itemIdByCell.ContainsKey(cell) || spawnPoints.Contains(cell))
            {
                return false;
            }

            return permanentCells.Add(cell);
        }

        /// <summary>Removes a permanent cell. Returns false if the cell was not permanent.</summary>
        public bool UnmarkPermanent(Vector2Int cell)
        {
            if (!permanentCells.Remove(cell))
            {
                return false;
            }

            PermanentCellRemoved?.Invoke(cell, RemovalReason.Direct);
            return true;
        }

        /// <summary>True if the cell is a permanent platform or trap.</summary>
        public bool IsPermanent(Vector2Int cell)
        {
            return permanentCells.Contains(cell);
        }

        /// <summary>
        /// Adds a spawn point. Returns false if the cell is out of bounds, permanent, holds a placed item,
        /// or is already a spawn point. Another spawn's zone is allowed, and items already inside the new
        /// zone are kept.
        /// </summary>
        public bool AddSpawnPoint(Vector2Int cell)
        {
            if (!Model.IsInBounds(cell) || permanentCells.Contains(cell) || itemIdByCell.ContainsKey(cell))
            {
                return false;
            }

            return spawnPoints.Add(cell);
        }

        /// <summary>Removes a spawn point. Returns false if there was none at the cell.</summary>
        public bool RemoveSpawnPoint(Vector2Int cell)
        {
            if (!spawnPoints.Remove(cell))
            {
                return false;
            }

            SpawnPointRemoved?.Invoke(cell, RemovalReason.Direct);
            return true;
        }

        /// <summary>
        /// True if the in-bounds cell is within <see cref="SpawnZoneRadius"/> cells of any spawn point
        /// on both axes.
        /// </summary>
        public bool IsInSpawnZone(Vector2Int cell)
        {
            if (!Model.IsInBounds(cell))
            {
                return false;
            }

            foreach (Vector2Int spawn in spawnPoints)
            {
                if (Mathf.Abs(cell.x - spawn.x) <= spawnZoneRadius && Mathf.Abs(cell.y - spawn.y) <= spawnZoneRadius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>State of one cell. Cells outside the grid report Empty.</summary>
        public CellInfo GetCell(Vector2Int cell)
        {
            bool inSpawnZone = IsInSpawnZone(cell);

            if (itemIdByCell.TryGetValue(cell, out int itemId))
            {
                return new CellInfo(CellState.Placed, itemId, items[itemId].OwnerIndex, inSpawnZone);
            }

            if (permanentCells.Contains(cell))
            {
                return new CellInfo(CellState.Permanent, NoItem, NoOwner, inSpawnZone);
            }

            CellState state = inSpawnZone ? CellState.SpawnZone : CellState.Empty;
            return new CellInfo(state, NoItem, NoOwner, inSpawnZone);
        }

        /// <summary>
        /// Checks whether the footprint fits with its anchor at <paramref name="anchor"/>. When cells fail
        /// for different reasons, the highest-priority one is returned (see <see cref="PlacementResult"/>).
        /// </summary>
        public PlacementResult CheckPlacement(Footprint footprint, Vector2Int anchor, int rotation)
        {
            ValidateFootprint(footprint);
            footprint.GetCells(anchor, rotation, cellBuffer);
            return Evaluate(cellBuffer);
        }

        /// <summary>
        /// Places the footprint if <see cref="CheckPlacement"/> returns Ok. <paramref name="itemId"/> is the new
        /// item's id on success and <see cref="NoItem"/> otherwise.
        /// </summary>
        public PlacementResult TryPlace(Footprint footprint, Vector2Int anchor, int rotation, int ownerIndex, out int itemId)
        {
            if (ownerIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerIndex), ownerIndex, "Owner index must be 0 or more.");
            }

            PlacementResult result = CheckPlacement(footprint, anchor, rotation);
            if (result != PlacementResult.Ok)
            {
                itemId = NoItem;
                return result;
            }

            itemId = nextItemId++;
            var item = new PlacedItem(itemId, ownerIndex, anchor, Footprint.NormalizeRotation(rotation), cellBuffer.ToArray());
            items.Add(itemId, item);
            foreach (Vector2Int cell in item.Cells)
            {
                itemIdByCell.Add(cell, itemId);
            }

            ItemPlaced?.Invoke(item);
            return result;
        }

        /// <summary>Removes a placed item. Returns false if no item has that id.</summary>
        public bool Remove(int itemId)
        {
            return RemoveItem(itemId, RemovalReason.Direct);
        }

        /// <summary>Gets a placed item by id.</summary>
        public bool TryGetItem(int itemId, out PlacedItem item)
        {
            return items.TryGetValue(itemId, out item);
        }

        /// <summary>
        /// Finds the valid anchor closest to <paramref name="desiredAnchor"/> for the same rotation, searching
        /// square rings outward. Closest means straight-line distance; ties go to the lower row, then the
        /// lower column. Returns false if no valid anchor exists.
        /// </summary>
        public bool TryFindNearestValid(Footprint footprint, int rotation, Vector2Int desiredAnchor, out Vector2Int anchor)
        {
            ValidateFootprint(footprint);
            footprint.GetCells(Vector2Int.zero, rotation, offsetBuffer);

            Vector2Int minOffset = offsetBuffer[0];
            Vector2Int maxOffset = offsetBuffer[0];
            foreach (Vector2Int offset in offsetBuffer)
            {
                minOffset = Vector2Int.Min(minOffset, offset);
                maxOffset = Vector2Int.Max(maxOffset, offset);
            }

            // Only anchors inside this box keep every footprint cell inside the grid.
            int minX = -minOffset.x;
            int maxX = Model.Columns - 1 - maxOffset.x;
            int minY = -minOffset.y;
            int maxY = Model.Rows - 1 - maxOffset.y;

            anchor = default;
            if (minX > maxX || minY > maxY)
            {
                return false;
            }

            int maxRing = Mathf.Max(
                Mathf.Max(Mathf.Abs(desiredAnchor.x - minX), Mathf.Abs(desiredAnchor.x - maxX)),
                Mathf.Max(Mathf.Abs(desiredAnchor.y - minY), Mathf.Abs(desiredAnchor.y - maxY)));

            bool found = false;
            int bestSqrDistance = 0;
            for (int ring = 0; ring <= maxRing; ring++)
            {
                // Every anchor on this ring is at least `ring` cells away, so it cannot beat a closer best.
                if (found && bestSqrDistance < ring * ring)
                {
                    break;
                }

                for (int dy = -ring; dy <= ring; dy++)
                {
                    int y = desiredAnchor.y + dy;
                    if (y < minY || y > maxY)
                    {
                        continue;
                    }

                    // Top and bottom rows of the ring are full; the rows between only have their two ends.
                    int step = Mathf.Abs(dy) == ring ? 1 : 2 * ring;
                    for (int dx = -ring; dx <= ring; dx += step)
                    {
                        int x = desiredAnchor.x + dx;
                        if (x < minX || x > maxX)
                        {
                            continue;
                        }

                        var candidate = new Vector2Int(x, y);
                        int sqrDistance = dx * dx + dy * dy;
                        if (found && !IsCloser(candidate, sqrDistance, anchor, bestSqrDistance))
                        {
                            continue;
                        }

                        if (IsValidAnchor(candidate))
                        {
                            found = true;
                            anchor = candidate;
                            bestSqrDistance = sqrDistance;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Removes every placed item with any cell inside the bomb area and returns their ids in ascending
        /// order. The area is <paramref name="size"/> cells starting at centerCell - (size - 1) / 2, so even
        /// sizes extend one extra cell toward +x/+y. Permanent cells and spawn points are never affected.
        /// Does nothing if <paramref name="centerCell"/> is out of bounds.
        /// </summary>
        public IReadOnlyList<int> Bomb(Vector2Int centerCell, Vector2Int size)
        {
            var removedIds = new List<int>();
            if (!Model.IsInBounds(centerCell))
            {
                return removedIds;
            }

            int width = Mathf.Max(PlacementConfig.MinBombSide, size.x);
            int height = Mathf.Max(PlacementConfig.MinBombSide, size.y);
            Vector2Int min = centerCell - new Vector2Int((width - 1) / 2, (height - 1) / 2);

            for (int y = min.y; y < min.y + height; y++)
            {
                for (int x = min.x; x < min.x + width; x++)
                {
                    if (itemIdByCell.TryGetValue(new Vector2Int(x, y), out int itemId) && !removedIds.Contains(itemId))
                    {
                        removedIds.Add(itemId);
                    }
                }
            }

            removedIds.Sort();
            foreach (int itemId in removedIds)
            {
                RemoveItem(itemId, RemovalReason.Bomb);
            }

            return removedIds;
        }

        /// <summary>Removes every placed item, in ascending id order. Permanent cells and spawn points stay.</summary>
        public void ClearPlaced()
        {
            var itemIds = new List<int>(items.Keys);
            itemIds.Sort();
            foreach (int itemId in itemIds)
            {
                RemoveItem(itemId, RemovalReason.Cleared);
            }
        }

        private PlacementResult Evaluate(List<Vector2Int> cells)
        {
            PlacementResult result = PlacementResult.Ok;
            foreach (Vector2Int cell in cells)
            {
                if (!Model.IsInBounds(cell))
                {
                    return PlacementResult.OutOfBounds;
                }

                PlacementResult cellResult = PlacementResult.Ok;
                if (itemIdByCell.ContainsKey(cell))
                {
                    cellResult = PlacementResult.BlockedByPlaced;
                }
                else if (permanentCells.Contains(cell))
                {
                    cellResult = PlacementResult.BlockedByPermanent;
                }
                else if (IsInSpawnZone(cell))
                {
                    cellResult = PlacementResult.InSpawnZone;
                }

                if (cellResult != PlacementResult.Ok && (result == PlacementResult.Ok || cellResult < result))
                {
                    result = cellResult;
                }
            }

            return result;
        }

        private bool IsValidAnchor(Vector2Int candidate)
        {
            cellBuffer.Clear();
            foreach (Vector2Int offset in offsetBuffer)
            {
                cellBuffer.Add(candidate + offset);
            }

            return Evaluate(cellBuffer) == PlacementResult.Ok;
        }

        private static bool IsCloser(Vector2Int candidate, int sqrDistance, Vector2Int best, int bestSqrDistance)
        {
            if (sqrDistance != bestSqrDistance)
            {
                return sqrDistance < bestSqrDistance;
            }

            return candidate.y != best.y ? candidate.y < best.y : candidate.x < best.x;
        }

        private bool RemoveItem(int itemId, RemovalReason reason)
        {
            if (!items.TryGetValue(itemId, out PlacedItem item))
            {
                return false;
            }

            items.Remove(itemId);
            foreach (Vector2Int cell in item.Cells)
            {
                itemIdByCell.Remove(cell);
            }

            ItemRemoved?.Invoke(item, reason);
            return true;
        }

        private bool AllInBounds(IReadOnlyList<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                if (!Model.IsInBounds(cell))
                {
                    return false;
                }
            }

            return true;
        }

        private List<Vector2Int> CollectOutOfBounds(HashSet<Vector2Int> cells)
        {
            var outOfBounds = new List<Vector2Int>();
            foreach (Vector2Int cell in cells)
            {
                if (!Model.IsInBounds(cell))
                {
                    outOfBounds.Add(cell);
                }
            }

            outOfBounds.Sort(CompareByRowThenColumn);
            return outOfBounds;
        }

        private static int CompareByRowThenColumn(Vector2Int a, Vector2Int b)
        {
            return a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
        }

        private static void ValidateFootprint(Footprint footprint)
        {
            if (footprint == null)
            {
                throw new ArgumentNullException(nameof(footprint));
            }

            if (footprint.CellCount == 0)
            {
                throw new ArgumentException("A footprint needs at least one cell.", nameof(footprint));
            }
        }
    }
}
