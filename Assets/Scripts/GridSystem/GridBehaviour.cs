using System;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Scene grid. Owns the current <see cref="GridModel"/>, uses this transform's position as the origin,
    /// and rebuilds when the config is edited, when <see cref="Resize"/> is called, or when the object moves.
    /// Code-driven resizes never write to the config asset. The latest change wins: editing the config
    /// clears any active code override.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class GridBehaviour : MonoBehaviour
    {
        [SerializeField] private GridConfig config;

        private GridConfig subscribedConfig;
        private bool hasResizeOverride;
        private int overrideColumns;
        private int overrideRows;
        private Vector3 lastOrigin;

        /// <summary>Raised after every rebuild with the new model.</summary>
        public event Action<GridModel> Resized;

        /// <summary>The config asset this grid reads from.</summary>
        public GridConfig Config => config;

        /// <summary>The current grid. Null only when no config is assigned.</summary>
        public GridModel Model { get; private set; }

        /// <summary>
        /// Overrides columns and rows from code (clamped to at least 1). Cell size still comes from the config.
        /// The config asset is not modified.
        /// </summary>
        public void Resize(int columns, int rows)
        {
            hasResizeOverride = true;
            overrideColumns = Mathf.Max(GridConfig.MinCellCount, columns);
            overrideRows = Mathf.Max(GridConfig.MinCellCount, rows);
            Rebuild();
        }

        /// <summary>Drops any code override and rebuilds from the config values.</summary>
        public void ClearResizeOverride()
        {
            hasResizeOverride = false;
            Rebuild();
        }

        private void OnEnable()
        {
            SubscribeTo(config);
            Rebuild();
        }

        private void OnDisable()
        {
            SubscribeTo(null);
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled || config == subscribedConfig)
            {
                return;
            }

            SubscribeTo(config);
            hasResizeOverride = false;
            Rebuild();
        }

        private void Update()
        {
            if (config != null && transform.position != lastOrigin)
            {
                Rebuild();
            }
        }

        private void HandleConfigChanged()
        {
            hasResizeOverride = false;
            Rebuild();
        }

        private void SubscribeTo(GridConfig target)
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

        private void Rebuild()
        {
            lastOrigin = transform.position;

            if (config == null)
            {
                Model = null;
                Debug.LogWarning($"{nameof(GridBehaviour)} on '{name}' has no {nameof(GridConfig)} assigned.", this);
                return;
            }

            int columns = hasResizeOverride ? overrideColumns : config.Columns;
            int rows = hasResizeOverride ? overrideRows : config.Rows;
            Model = new GridModel(columns, rows, config.CellSize, lastOrigin);
            Resized?.Invoke(Model);
        }
    }
}
