using System;
using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>Places sprites so they cover an exact world rectangle.</summary>
    public static class SpriteSizing
    {
        /// <summary>
        /// Moves and scales <paramref name="target"/> so <paramref name="sprite"/> covers a world rectangle of
        /// <paramref name="worldSize"/> centered on <paramref name="worldCenter"/>, whatever the sprite's
        /// pixels per unit or pivot. Rotation is reset. The target must be a root object or the child of an
        /// unrotated, unscaled parent.
        /// </summary>
        public static void SetWorldSize(Transform target, Sprite sprite, Vector3 worldCenter, Vector2 worldSize)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            Bounds bounds = sprite.bounds;
            float scaleX = bounds.size.x > 0f ? worldSize.x / bounds.size.x : 0f;
            float scaleY = bounds.size.y > 0f ? worldSize.y / bounds.size.y : 0f;
            var scale = new Vector3(scaleX, scaleY, 1f);

            // Offset by the scaled sprite center so off-center pivots still land in the middle of the rectangle.
            Vector3 pivotOffset = Vector3.Scale(bounds.center, scale);
            var position = new Vector3(worldCenter.x - pivotOffset.x, worldCenter.y - pivotOffset.y, worldCenter.z);

            target.SetPositionAndRotation(position, Quaternion.identity);
            target.localScale = scale;
        }
    }
}
