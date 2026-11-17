using System.Collections.Generic;
using UnityEngine;
using Wreckyard.Core;

namespace Wreckyard.Gameplay
{
    /// <summary>
    /// Creates a visual for every placed item that has a definition id, and destroys it when the item is
    /// removed (directly, by bomb, on clear or on grid resize). Visuals sit under a "Placed Items" root at
    /// the anchor cell center, rotated 90 degrees counter-clockwise per rotation step and scaled by cellSize,
    /// and follow the grid when it resizes, moves or changes cell size. Play mode only.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacedItemSpawner : MonoBehaviour
    {
        private const string ParentName = "Placed Items";
        private const float DegreesPerRotationStep = 360f / Footprint.RotationSteps;

        [SerializeField] private GridBehaviour grid;
        [SerializeField] private GridOccupancyBehaviour occupancyBehaviour;
        [SerializeField] private ItemCatalog catalog;

        private readonly Dictionary<int, Visual> visuals = new Dictionary<int, Visual>();
        private readonly List<PlacedItem> itemBuffer = new List<PlacedItem>();
        private GridBehaviour subscribedGrid;
        private GridOccupancy subscribedOccupancy;
        private Transform parent;
        private bool rebuildRequested;

        /// <summary>
        /// Destroys every visual and spawns them again from the occupancy's current items, in id order.
        /// Only works in Play mode while this component is enabled.
        /// </summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                Debug.LogWarning($"{nameof(PlacedItemSpawner)} on '{name}': placed item visuals exist only in Play mode while enabled.", this);
                return;
            }

            rebuildRequested = false;
            DestroyAllVisuals();

            if (subscribedOccupancy == null)
            {
                return;
            }

            itemBuffer.Clear();
            itemBuffer.AddRange(subscribedOccupancy.Items);
            itemBuffer.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (PlacedItem item in itemBuffer)
            {
                Spawn(item);
            }

            itemBuffer.Clear();
        }

        private void OnEnable()
        {
            if (grid == null || occupancyBehaviour == null)
            {
                Debug.LogWarning($"{nameof(PlacedItemSpawner)} on '{name}' needs a {nameof(GridBehaviour)} and a {nameof(GridOccupancyBehaviour)}.", this);
            }

            SubscribeToGrid(grid);
            EnsureSubscribedToOccupancy();
        }

        private void OnDisable()
        {
            SubscribeToGrid(null);
            SubscribeToOccupancy(null);
            DestroyAllVisuals();
            rebuildRequested = false;

            if (parent != null)
            {
                Destroy(parent.gameObject);
            }

            parent = null;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            if (grid != subscribedGrid)
            {
                SubscribeToGrid(grid);
            }

            // Spawning inside OnValidate is not safe, so the rebuild waits for the next Update.
            rebuildRequested = true;
        }

        private void Update()
        {
            EnsureSubscribedToOccupancy();

            if (rebuildRequested)
            {
                Rebuild();
            }
        }

        private void HandleItemPlaced(PlacedItem item)
        {
            Spawn(item);
        }

        private void HandleItemRemoved(PlacedItem item, RemovalReason reason)
        {
            if (!visuals.TryGetValue(item.Id, out Visual visual))
            {
                return;
            }

            visuals.Remove(item.Id);
            if (visual.Root != null)
            {
                Destroy(visual.Root.gameObject);
            }
        }

        private void HandleGridResized(GridModel model)
        {
            foreach (Visual visual in visuals.Values)
            {
                if (visual.Root != null)
                {
                    ApplyTransform(visual.Root, visual.Item, model);
                }
            }
        }

        private void Spawn(PlacedItem item)
        {
            if (item.DefinitionId == null || visuals.ContainsKey(item.Id))
            {
                return;
            }

            if (catalog == null)
            {
                Debug.LogWarning($"{nameof(PlacedItemSpawner)} on '{name}' has no {nameof(ItemCatalog)}; item {item.Id} gets no visual.", this);
                return;
            }

            if (!catalog.TryGet(item.DefinitionId, out ItemDefinition definition))
            {
                Debug.LogWarning($"{nameof(PlacedItemSpawner)} on '{name}': no definition with id '{item.DefinitionId}' in catalog '{catalog.name}'; item {item.Id} gets no visual.", this);
                return;
            }

            if (definition.Prefab == null)
            {
                Debug.LogWarning($"{nameof(PlacedItemSpawner)} on '{name}': definition '{definition.name}' has no prefab; item {item.Id} gets no visual.", this);
                return;
            }

            GridModel model = CurrentModel();
            if (model == null)
            {
                return;
            }

            GameObject instance = Instantiate(definition.Prefab, GetOrCreateParent());
            string label = string.IsNullOrEmpty(definition.DisplayName) ? definition.Id : definition.DisplayName;
            instance.name = $"{label} #{item.Id}";
            ApplyTransform(instance.transform, item, model);
            visuals.Add(item.Id, new Visual(item, instance.transform));
        }

        private static void ApplyTransform(Transform root, PlacedItem item, GridModel model)
        {
            root.SetPositionAndRotation(
                model.CellToWorldCenter(item.Anchor),
                Quaternion.Euler(0f, 0f, DegreesPerRotationStep * item.Rotation));
            root.localScale = Vector3.one * model.CellSize;
        }

        private GridModel CurrentModel()
        {
            // The grid's model is always current; the occupancy's may lag behind during a Resized event.
            if (grid != null && grid.Model != null)
            {
                return grid.Model;
            }

            return subscribedOccupancy?.Model;
        }

        private Transform GetOrCreateParent()
        {
            if (parent == null)
            {
                parent = new GameObject(ParentName).transform;
            }

            return parent;
        }

        private void DestroyAllVisuals()
        {
            foreach (Visual visual in visuals.Values)
            {
                if (visual.Root != null)
                {
                    Destroy(visual.Root.gameObject);
                }
            }

            visuals.Clear();
        }

        private void EnsureSubscribedToOccupancy()
        {
            GridOccupancy current = occupancyBehaviour != null ? occupancyBehaviour.Occupancy : null;
            if (current == subscribedOccupancy)
            {
                return;
            }

            SubscribeToOccupancy(current);
            Rebuild();
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

        private void SubscribeToOccupancy(GridOccupancy target)
        {
            if (subscribedOccupancy != null)
            {
                subscribedOccupancy.ItemPlaced -= HandleItemPlaced;
                subscribedOccupancy.ItemRemoved -= HandleItemRemoved;
            }

            subscribedOccupancy = target;

            if (subscribedOccupancy != null)
            {
                subscribedOccupancy.ItemPlaced += HandleItemPlaced;
                subscribedOccupancy.ItemRemoved += HandleItemRemoved;
            }
        }

        private readonly struct Visual
        {
            public Visual(PlacedItem item, Transform root)
            {
                Item = item;
                Root = root;
            }

            public PlacedItem Item { get; }

            public Transform Root { get; }
        }
    }
}
