using System.Collections.Generic;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Data for one placeable item. A new block type is a new asset, not new code.
    /// Prefab convention: authored at 1 unit per cell, with the root at the center of the anchor cell and
    /// footprint offset (dx, dy) at local position (dx, dy). The spawner sets the root's position,
    /// rotation and scale, so anything authored on the root transform is ignored.
    /// </summary>
    [CreateAssetMenu(menuName = "Wreckyard/Item Definition", fileName = "ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private ItemCategory category;
        [SerializeField] private List<FootprintRect> footprint = new List<FootprintRect>
        {
            new FootprintRect(Vector2Int.zero, Vector2Int.one)
        };
        [SerializeField] private bool canRotate = true;
        [SerializeField] private GameObject prefab;

        private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>();

        /// <summary>Unique id used to look this item up in an <see cref="ItemCatalog"/>, for example block_3x1.</summary>
        public string Id => id;

        /// <summary>Name shown to players and used for spawned object names.</summary>
        public string DisplayName => displayName;

        /// <summary>Item family.</summary>
        public ItemCategory Category => category;

        /// <summary>Rectangles whose union is the footprint, in cell offsets from the anchor.</summary>
        public IReadOnlyList<FootprintRect> FootprintRects => footprint;

        /// <summary>True if the item may be rotated in 90 degree steps.</summary>
        public bool CanRotate => canRotate;

        /// <summary>Visual and collider prefab, authored at 1 unit per cell.</summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// Builds the footprint from the union of <see cref="FootprintRects"/>. Returns false (and null) when the
        /// list has no rectangles. A new instance is returned each call, so later edits are always picked up.
        /// </summary>
        public bool TryCreateFootprint(out Footprint result)
        {
            cellBuffer.Clear();
            if (footprint != null)
            {
                foreach (FootprintRect rect in footprint)
                {
                    Vector2Int size = FootprintRect.ClampSize(rect.Size);
                    for (int y = 0; y < size.y; y++)
                    {
                        for (int x = 0; x < size.x; x++)
                        {
                            cellBuffer.Add(rect.Min + new Vector2Int(x, y));
                        }
                    }
                }
            }

            result = cellBuffer.Count == 0 ? null : new Footprint(cellBuffer);
            return result != null;
        }

        /// <summary>Rotation to actually use: 0 when the item cannot rotate, otherwise the request wrapped into 0 to 3.</summary>
        public int GetAllowedRotation(int requested)
        {
            return canRotate ? Footprint.NormalizeRotation(requested) : 0;
        }

        private void OnValidate()
        {
            if (footprint != null)
            {
                for (int i = 0; i < footprint.Count; i++)
                {
                    FootprintRect rect = footprint[i];
                    if (rect.Size != FootprintRect.ClampSize(rect.Size))
                    {
                        footprint[i] = new FootprintRect(rect.Min, rect.Size);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"{nameof(ItemDefinition)} '{name}' has an empty id.", this);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(ItemDefinition)} '{name}' has no prefab.", this);
            }

            if (footprint == null || footprint.Count == 0)
            {
                Debug.LogWarning($"{nameof(ItemDefinition)} '{name}' has a footprint with no cells.", this);
            }
        }
    }
}
