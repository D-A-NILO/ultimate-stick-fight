using System.Collections.Generic;
using UnityEngine;
using Wreckyard.Core;

namespace Wreckyard.Gameplay
{
    /// <summary>
    /// Runtime ghost of the cursor's selected item: one sprite per footprint cell at the cursor cell and allowed
    /// rotation, tinted with the valid color when the placement check is Ok and the invalid color otherwise.
    /// Re-checked every frame, so it is never stale (some occupancy changes raise no event). Sprites live under
    /// their own root object, pooled, and destroyed on disable. Play mode only.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacementGhostView : MonoBehaviour
    {
        private const string ContainerName = "Placement Ghost";

        [SerializeField] private PlacementCursor cursor;

        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>();
        private Transform container;
        private bool warnedMissingSprite;

        private void OnDisable()
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }

            container = null;
            renderers.Clear();
        }

        private void LateUpdate()
        {
            int shown = 0;
            PlacementCursorConfig config = cursor != null ? cursor.Config : null;

            if (cursor != null && cursor.isActiveAndEnabled && config != null
                && cursor.TryGetPreview(cellBuffer, out PlacementResult result))
            {
                Sprite sprite = config.CellSprite;
                if (sprite == null)
                {
                    if (!warnedMissingSprite)
                    {
                        warnedMissingSprite = true;
                        Debug.LogWarning($"{nameof(PlacementGhostView)} on '{name}': the {nameof(PlacementCursorConfig)} has no cell sprite.", this);
                    }
                }
                else
                {
                    warnedMissingSprite = false;
                    GridModel model = cursor.Model;
                    Vector2 cellSize = Vector2.one * model.CellSize;
                    Color color = result == PlacementResult.Ok ? config.GhostValidColor : config.GhostInvalidColor;

                    foreach (Vector2Int cell in cellBuffer)
                    {
                        SpriteRenderer target = GetOrCreateRenderer(shown++);
                        target.enabled = true;
                        target.sprite = sprite;
                        target.color = color;
                        target.sortingOrder = config.GhostSortingOrder;
                        SpriteSizing.SetWorldSize(target.transform, sprite, model.CellToWorldCenter(cell), cellSize);
                    }
                }
            }

            for (int i = shown; i < renderers.Count; i++)
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
                // The old container (and its cells) may have been destroyed from outside.
                renderers.Clear();
                container = new GameObject(ContainerName).transform;
            }

            while (renderers.Count <= index)
            {
                var cellObject = new GameObject($"Ghost Cell {renderers.Count}");
                cellObject.transform.SetParent(container, false);
                renderers.Add(cellObject.AddComponent<SpriteRenderer>());
            }

            return renderers[index];
        }
    }
}
