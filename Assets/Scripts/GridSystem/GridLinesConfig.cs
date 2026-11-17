using System;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Tunable look of the runtime grid lines and border. Edited only in the Inspector; code never writes to
    /// this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Wreckyard/Grid Lines Config", fileName = "GridLinesConfig")]
    public class GridLinesConfig : ScriptableObject
    {
        /// <summary>Smallest allowed line or border thickness in world units. 0 hides them.</summary>
        public const float MinThickness = 0f;

        [SerializeField, Tooltip("Sprite stretched into each line. Any size or pivot works.")]
        private Sprite lineSprite;

        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.15f);

        [SerializeField, Min(MinThickness), Tooltip("Inner line thickness in world units.")]
        private float lineThickness = 0.03f;

        [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.6f);

        [SerializeField, Min(MinThickness), Tooltip("Border thickness in world units, centered on the grid edge.")]
        private float borderThickness = 0.08f;

        [SerializeField, Tooltip("Sorting order of lines and border. Placed blocks use 0, so a negative value keeps lines behind them.")]
        private int sortingOrder = -10;

        /// <summary>Raised after any value is edited in the Inspector (including undo and redo).</summary>
        public event Action Changed;

        /// <summary>Sprite stretched into each line.</summary>
        public Sprite LineSprite => lineSprite;

        /// <summary>Color of the inner lines.</summary>
        public Color LineColor => lineColor;

        /// <summary>Inner line thickness in world units.</summary>
        public float LineThickness => lineThickness;

        /// <summary>Color of the outer border.</summary>
        public Color BorderColor => borderColor;

        /// <summary>Border thickness in world units.</summary>
        public float BorderThickness => borderThickness;

        /// <summary>Sorting order of lines and border.</summary>
        public int SortingOrder => sortingOrder;

        private void OnValidate()
        {
            lineThickness = Mathf.Max(MinThickness, lineThickness);
            borderThickness = Mathf.Max(MinThickness, borderThickness);
            Changed?.Invoke();
        }
    }
}
