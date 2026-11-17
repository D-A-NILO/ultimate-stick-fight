using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Owns the scene's <see cref="GridOccupancy"/>. Creates it once the grid has a model, then keeps it in
    /// sync with GridBehaviour.Resized and PlacementConfig.Changed. The same instance is kept for the
    /// component's lifetime, so other systems can hold on to it.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GridOccupancyBehaviour : MonoBehaviour
    {
        [SerializeField] private GridBehaviour grid;
        [SerializeField] private PlacementConfig config;

        private GridBehaviour subscribedGrid;
        private PlacementConfig subscribedConfig;

        /// <summary>The occupancy. Null until both a grid with a model and a config are assigned.</summary>
        public GridOccupancy Occupancy { get; private set; }

        /// <summary>The placement config this occupancy reads from.</summary>
        public PlacementConfig Config => config;

        private void OnEnable()
        {
            SubscribeToGrid(grid);
            SubscribeToConfig(config);
            Sync();
        }

        private void OnDisable()
        {
            SubscribeToGrid(null);
            SubscribeToConfig(null);
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (grid != subscribedGrid)
            {
                SubscribeToGrid(grid);
            }

            if (config != subscribedConfig)
            {
                SubscribeToConfig(config);
            }

            Sync();
        }

        private void HandleGridResized(GridModel model)
        {
            Sync();
        }

        private void HandleConfigChanged()
        {
            Sync();
        }

        private void Sync()
        {
            if (grid == null || config == null)
            {
                Debug.LogWarning($"{nameof(GridOccupancyBehaviour)} on '{name}' needs a {nameof(GridBehaviour)} and a {nameof(PlacementConfig)}.", this);
                return;
            }

            GridModel model = grid.Model;
            if (model == null)
            {
                return;
            }

            if (Occupancy == null)
            {
                Occupancy = new GridOccupancy(model, config.SpawnZoneRadius);
                return;
            }

            Occupancy.SpawnZoneRadius = config.SpawnZoneRadius;
            if (Occupancy.Model != model)
            {
                Occupancy.ApplyModel(model);
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

        private void SubscribeToConfig(PlacementConfig target)
        {
            if (subscribedConfig is not null)
            {
                subscribedConfig.Changed -= HandleConfigChanged;
            }

            subscribedConfig = target;

            if (subscribedConfig is not null)
            {
                subscribedConfig.Changed += HandleConfigChanged;
            }
        }
    }
}
