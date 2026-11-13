using UnityEngine;
using Wreckyard.Core;

namespace Wreckyard.Gameplay
{
    /// <summary>
    /// Runtime sprite on the cursor's anchor cell. After a failed placement it shows the fail color and fades
    /// back to the normal cursor color. The sprite lives on its own root object, created on demand and destroyed
    /// on disable. Play mode only.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacementCursorHighlight : MonoBehaviour
    {
        private const string ObjectName = "Placement Cursor Highlight";

        [SerializeField] private PlacementCursor cursor;

        private PlacementCursor subscribedCursor;
        private SpriteRenderer highlight;
        private float flashRemaining;
        private bool warnedMissingSprite;

        private void OnEnable()
        {
            SubscribeTo(cursor);
        }

        private void OnDisable()
        {
            SubscribeTo(null);
            flashRemaining = 0f;

            if (highlight != null)
            {
                Destroy(highlight.gameObject);
            }

            highlight = null;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled && cursor != subscribedCursor)
            {
                SubscribeTo(cursor);
            }
        }

        private void LateUpdate()
        {
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);

            PlacementCursorConfig config = cursor != null ? cursor.Config : null;
            if (cursor == null || !cursor.isActiveAndEnabled || config == null || !cursor.IsReady)
            {
                SetVisible(false);
                return;
            }

            Sprite sprite = config.CellSprite;
            if (sprite == null)
            {
                if (!warnedMissingSprite)
                {
                    warnedMissingSprite = true;
                    Debug.LogWarning($"{nameof(PlacementCursorHighlight)} on '{name}': the {nameof(PlacementCursorConfig)} has no cell sprite.", this);
                }

                SetVisible(false);
                return;
            }

            warnedMissingSprite = false;
            GridModel model = cursor.Model;
            SpriteRenderer target = GetOrCreateRenderer();
            target.enabled = true;
            target.sprite = sprite;
            target.sortingOrder = config.CursorSortingOrder;

            float duration = config.FailFlashDuration;
            target.color = flashRemaining > 0f && duration > 0f
                ? Color.Lerp(config.CursorColor, config.FailFlashColor, flashRemaining / duration)
                : config.CursorColor;

            SpriteSizing.SetWorldSize(target.transform, sprite, model.CellToWorldCenter(cursor.Cell), Vector2.one * model.CellSize);
        }

        private void HandlePlacementFailed()
        {
            PlacementCursorConfig config = cursor != null ? cursor.Config : null;
            flashRemaining = config != null ? config.FailFlashDuration : 0f;
        }

        private void SetVisible(bool visible)
        {
            if (highlight != null)
            {
                highlight.enabled = visible;
            }
        }

        private SpriteRenderer GetOrCreateRenderer()
        {
            if (highlight == null)
            {
                highlight = new GameObject(ObjectName).AddComponent<SpriteRenderer>();
            }

            return highlight;
        }

        private void SubscribeTo(PlacementCursor target)
        {
            if (subscribedCursor is not null)
            {
                subscribedCursor.PlacementFailed -= HandlePlacementFailed;
            }

            subscribedCursor = target;

            if (subscribedCursor is not null)
            {
                subscribedCursor.PlacementFailed += HandlePlacementFailed;
            }
        }
    }
}
