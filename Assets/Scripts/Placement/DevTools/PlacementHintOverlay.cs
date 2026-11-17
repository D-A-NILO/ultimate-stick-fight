using System.Text;
using UnityEngine;
using Wreckyard.Core;
using Wreckyard.Gameplay;

namespace Wreckyard.UI
{
    /// <summary>
    /// TEMPORARY controls hint (step 3b). An IMGUI label in the top-left corner listing the keyboard bindings,
    /// the catalog slots and the selected item. Scales with the window height so it stays readable in any
    /// window size. Remove once real UI exists.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacementHintOverlay : MonoBehaviour
    {
        [SerializeField] private PlacementCursor cursor;
        [SerializeField] private KeyboardPlacementInput keyboard;
        [SerializeField, Min(1)] private int fontSize = 18;
        [SerializeField, Min(0f), Tooltip("Distance from the window corner, in reference pixels.")] private float margin = 12f;
        [SerializeField, Min(0f), Tooltip("Space between the text and the edge of its background, in reference pixels.")] private float padding = 8f;
        [SerializeField, Min(1f), Tooltip("Window height at which the hint is drawn at scale 1.")] private float referenceHeight = 1080f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        private readonly StringBuilder textBuilder = new StringBuilder();
        private readonly GUIContent content = new GUIContent();
        private GUIStyle style;
        private int builtFrame = -1;

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { wordWrap = false, richText = false };
            }

            style.fontSize = fontSize;
            style.normal.textColor = textColor;

            if (builtFrame != Time.frameCount)
            {
                builtFrame = Time.frameCount;
                content.text = BuildText();
            }

            float scale = Screen.height / referenceHeight;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Vector2 textSize = style.CalcSize(content);
            var background = new Rect(margin, margin, textSize.x + padding * 2f, textSize.y + padding * 2f);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, backgroundColor, 0f, 0f);
            GUI.Label(new Rect(margin + padding, margin + padding, textSize.x, textSize.y), content, style);

            GUI.matrix = previousMatrix;
        }

        private string BuildText()
        {
            textBuilder.Clear();

            if (keyboard != null)
            {
                textBuilder.AppendLine(keyboard.DescribeBindings());
            }
            else
            {
                textBuilder.AppendLine("(no keyboard input assigned)");
            }

            ItemCatalog catalog = cursor != null ? cursor.Catalog : null;
            if (catalog != null)
            {
                textBuilder.AppendLine();
                for (int i = 0; i < catalog.Count; i++)
                {
                    textBuilder.Append("Slot ").Append(i + 1).Append(": ").AppendLine(NameOf(catalog.Items[i]));
                }
            }

            textBuilder.AppendLine();
            textBuilder.Append("Selected: ");
            if (cursor != null && cursor.TryGetSelectedDefinition(out ItemDefinition selected))
            {
                textBuilder.Append(NameOf(selected));
            }
            else
            {
                textBuilder.Append("none");
            }

            return textBuilder.ToString();
        }

        private static string NameOf(ItemDefinition definition)
        {
            if (definition == null)
            {
                return "(empty)";
            }

            return string.IsNullOrEmpty(definition.DisplayName) ? definition.Id : definition.DisplayName;
        }
    }
}
