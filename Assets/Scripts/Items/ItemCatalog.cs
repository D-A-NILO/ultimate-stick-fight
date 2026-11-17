using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// The list of item definitions the game can use, with lookup by <see cref="ItemDefinition.Id"/>.
    /// Lookup is a linear search on purpose: nothing is cached, so renaming a definition's id is picked up
    /// immediately, even with domain reload disabled.
    /// </summary>
    [CreateAssetMenu(menuName = "Wreckyard/Item Catalog", fileName = "ItemCatalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        /// <summary>All entries in Inspector order. Entries may be null if a slot is left empty.</summary>
        public IReadOnlyList<ItemDefinition> Items => items;

        /// <summary>Number of entries, including empty slots.</summary>
        public int Count => items == null ? 0 : items.Count;

        /// <summary>
        /// Finds the first definition whose id matches exactly (case-sensitive). Returns false for a null or
        /// empty id, or when no definition matches.
        /// </summary>
        public bool TryGet(string id, out ItemDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(id) || items == null)
            {
                return false;
            }

            foreach (ItemDefinition candidate in items)
            {
                if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            if (items == null)
            {
                return;
            }

            var seenAssets = new HashSet<ItemDefinition>();
            var firstIndexById = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition = items[i];
                if (definition == null)
                {
                    Debug.LogWarning($"{nameof(ItemCatalog)} '{name}': slot {i} is empty.", this);
                    continue;
                }

                if (!seenAssets.Add(definition))
                {
                    Debug.LogWarning($"{nameof(ItemCatalog)} '{name}': '{definition.name}' is listed more than once (slot {i}).", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    Debug.LogWarning($"{nameof(ItemCatalog)} '{name}': '{definition.name}' (slot {i}) has an empty id.", this);
                    continue;
                }

                if (firstIndexById.TryGetValue(definition.Id, out int firstIndex))
                {
                    Debug.LogWarning(
                        $"{nameof(ItemCatalog)} '{name}': id '{definition.Id}' is used by '{items[firstIndex].name}' (slot {firstIndex})" +
                        $" and '{definition.name}' (slot {i}). Lookups return the first one.", this);
                    continue;
                }

                firstIndexById.Add(definition.Id, i);
            }
        }
    }
}
