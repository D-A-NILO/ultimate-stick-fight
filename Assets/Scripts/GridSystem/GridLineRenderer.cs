using System.Collections.Generic;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Runtime grid lines and border, visible in builds (gizmos are not). Each line is a sprite stretched to its
    /// world length and thickness under a "Grid Lines" root object. Rebuilds when the grid raises Resized
    /// (position, columns, rows, cell size) and when the config is edited, in the LateUpdate of the same frame.
    /// Play mode only; the gizmo drawer still covers Edit mode.
    /// </summary>
    [RequireComponent(typeof(GridBehaviour))]
    [DisallowMultipleComponent]
    public class GridLineRenderer : MonoBehaviour
    {
        private const string ContainerName = "Grid Lines";

        [SerializeField] private GridLinesConfig config;

        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        private GridBehaviour grid;
        private GridLinesConfig subscribedConfig;
        private Transform container;
        private bool rebuildRequested;

        private void OnEnable()
        {
            grid = GetComponent<GridBehaviour>();
            grid.Resized += HandleGridResized;
            SubscribeToConfig(config);
            Rebuild();
        }

        private void OnDisable()
        {
            if (grid is not null)
            {
                grid.Resized -= HandleGridResized;
            }

            SubscribeToConfig(null);
            rebuildRequested = false;

            if (container != null)
            {
                Destroy(container.gameObject);
            }

            container = null;
            renderers.Clear();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            if (config != subscribedConfig)
            {
                SubscribeToConfig(config);
            }

            // Creating objects inside OnValidate is not safe, so the rebuild waits for LateUpdate.
            rebuildRequested = true;
        }

        private void LateUpdate()
        {
            if (rebuildRequested)
            {
                Rebuild();
            }
        }

        // Resized and Changed can fire from inside OnValidate, so both only request a rebuild.
        private void HandleGridResized(GridModel model)
        {
            rebuildRequested = true;
        }

        private void HandleConfigChanged()
        {
            rebuildRequested = true;
        }

        private void Rebuild()
        {
            rebuildRequested = false;

            GridModel model = grid != null ? grid.Model : null;
            if (config == null || config.LineSprite == null || model == null)
            {
                if (config == null || config.LineSprite == null)
                {
                    Debug.LogWarning($"{nameof(GridLineRenderer)} on '{name}' needs a {nameof(GridLinesConfig)} with a line sprite.", this);
                }

                HideFrom(0);
                return;
            }

            Sprite sprite = config.LineSprite;
            Vector2 size = model.WorldSize;
            Vector3 origin = model.Origin;
            float lineThickness = config.LineThickness;
            float borderThickness = config.BorderThickness;
            bool showLines = lineThickness > 0f;
            bool showBorder = borderThickness > 0f;
            int index = 0;

            for (int column = 1; column < model.Columns; column++)
            {
                var center = new Vector3(origin.x + column * model.CellSize, origin.y + size.y * 0.5f, origin.z);
                Draw(index++, sprite, center, new Vector2(lineThickness, size.y), config.LineColor, showLines);
            }

            for (int row = 1; row < model.Rows; row++)
            {
                var center = new Vector3(origin.x + size.x * 0.5f, origin.y + row * model.CellSize, origin.z);
                Draw(index++, sprite, center, new Vector2(size.x, lineThickness), config.LineColor, showLines);
            }

            // Border sides are lengthened by one thickness so the corners are filled.
            var horizontal = new Vector2(size.x + borderThickness, borderThickness);
            var vertical = new Vector2(borderThickness, size.y + borderThickness);
            Draw(index++, sprite, origin + new Vector3(size.x * 0.5f, 0f, 0f), horizontal, config.BorderColor, showBorder);
            Draw(index++, sprite, origin + new Vector3(size.x * 0.5f, size.y, 0f), horizontal, config.BorderColor, showBorder);
            Draw(index++, sprite, origin + new Vector3(0f, size.y * 0.5f, 0f), vertical, config.BorderColor, showBorder);
            Draw(index++, sprite, origin + new Vector3(size.x, size.y * 0.5f, 0f), vertical, config.BorderColor, showBorder);

            HideFrom(index);
        }

        private void Draw(int index, Sprite sprite, Vector3 center, Vector2 worldSize, Color color, bool visible)
        {
            SpriteRenderer target = GetOrCreateRenderer(index);
            target.enabled = visible;
            if (!visible)
            {
                return;
            }

            target.sprite = sprite;
            target.color = color;
            target.sortingOrder = config.SortingOrder;
            SpriteSizing.SetWorldSize(target.transform, sprite, center, worldSize);
        }

        private void HideFrom(int index)
        {
            for (int i = index; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        private SpriteRenderer GetOrCreateRenderer(int index)
        {
            if (container == null)
            {
                // The old container (and its lines) may have been destroyed from outside.
                renderers.Clear();
                container = new GameObject(ContainerName).transform;
            }

            while (renderers.Count <= index)
            {
                var lineObject = new GameObject($"Line {renderers.Count}");
                lineObject.transform.SetParent(container, false);
                renderers.Add(lineObject.AddComponent<SpriteRenderer>());
            }

            return renderers[index];
        }

        private void SubscribeToConfig(GridLinesConfig target)
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
