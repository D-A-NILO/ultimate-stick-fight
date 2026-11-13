using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Wreckyard.Core;

namespace Wreckyard.Gameplay
{
    /// <summary>
    /// Keyboard source for a placement cursor, read from <see cref="Keyboard.current"/>. Keys are physical key
    /// positions and can be rebound in the Inspector; set a key to None to unbind it. Move keys step once on
    /// press and repeat while held, using the timing in <see cref="PlacementCursorConfig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeyboardPlacementInput : MonoBehaviour, IPlacementIntentSource
    {
        [SerializeField] private PlacementCursorConfig config;

        [Header("Move")]
        [SerializeField] private Key moveUp = Key.W;
        [SerializeField] private Key moveDown = Key.S;
        [SerializeField] private Key moveLeft = Key.A;
        [SerializeField] private Key moveRight = Key.D;

        [Header("Rotate")]
        [SerializeField] private Key rotateCounterClockwise = Key.Q;
        [SerializeField] private Key rotateClockwise = Key.E;

        [Header("Place (first key = first catalog entry)")]
        [SerializeField] private Key[] placeSlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

        [Header("Extras")]
        [SerializeField] private Key bomb = Key.B;
        [SerializeField] private Key clear = Key.Backspace;

        private readonly HoldRepeat moveRepeat = new HoldRepeat();
        private readonly StringBuilder textBuilder = new StringBuilder();

        /// <inheritdoc/>
        public PlacementIntent ReadIntent()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveRepeat.Reset();
                return PlacementIntent.None;
            }

            var held = new Vector2Int(
                Axis(keyboard, moveLeft, moveRight),
                Axis(keyboard, moveDown, moveUp));

            float delay = config != null ? config.InitialRepeatDelay : 0f;
            float rate = config != null ? config.RepeatRate : 0f;
            Vector2Int step = moveRepeat.Tick(held, Time.unscaledDeltaTime, delay, rate);

            int rotate = (WasPressed(keyboard, rotateCounterClockwise) ? 1 : 0) - (WasPressed(keyboard, rotateClockwise) ? 1 : 0);

            int slot = PlacementIntent.NoSlot;
            if (placeSlotKeys != null)
            {
                for (int i = 0; i < placeSlotKeys.Length; i++)
                {
                    if (WasPressed(keyboard, placeSlotKeys[i]))
                    {
                        slot = i;
                        break;
                    }
                }
            }

            return new PlacementIntent(step, rotate, slot, WasPressed(keyboard, bomb), WasPressed(keyboard, clear));
        }

        /// <summary>
        /// Multi-line description of the current bindings, using the active keyboard layout's key names.
        /// </summary>
        public string DescribeBindings()
        {
            Keyboard keyboard = Keyboard.current;
            textBuilder.Clear();
            textBuilder.Append(Label(keyboard, moveUp)).Append(' ')
                .Append(Label(keyboard, moveLeft)).Append(' ')
                .Append(Label(keyboard, moveDown)).Append(' ')
                .Append(Label(keyboard, moveRight)).AppendLine("   Move (hold to repeat)");
            textBuilder.Append(Label(keyboard, rotateCounterClockwise)).Append(" / ")
                .Append(Label(keyboard, rotateClockwise)).AppendLine("   Rotate counter-clockwise / clockwise");

            int slotCount = placeSlotKeys != null ? placeSlotKeys.Length : 0;
            for (int i = 0; i < slotCount; i++)
            {
                if (i > 0)
                {
                    textBuilder.Append(' ');
                }

                textBuilder.Append(Label(keyboard, placeSlotKeys[i]));
            }

            textBuilder.Append("   Place slot 1 to ").Append(slotCount).AppendLine();
            textBuilder.Append(Label(keyboard, bomb)).AppendLine("   Bomb around the cursor");
            textBuilder.Append(Label(keyboard, clear)).Append("   Clear all placed items");
            return textBuilder.ToString();
        }

        private void OnEnable()
        {
            if (config == null)
            {
                Debug.LogWarning($"{nameof(KeyboardPlacementInput)} on '{name}' has no {nameof(PlacementCursorConfig)}; held keys will not repeat.", this);
            }
        }

        private void OnDisable()
        {
            moveRepeat.Reset();
        }

        private static int Axis(Keyboard keyboard, Key negative, Key positive)
        {
            return (IsPressed(keyboard, positive) ? 1 : 0) - (IsPressed(keyboard, negative) ? 1 : 0);
        }

        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            KeyControl control = GetControl(keyboard, key);
            return control != null && control.isPressed;
        }

        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            KeyControl control = GetControl(keyboard, key);
            return control != null && control.wasPressedThisFrame;
        }

        private static string Label(Keyboard keyboard, Key key)
        {
            if (key == Key.None)
            {
                return "-";
            }

            KeyControl control = GetControl(keyboard, key);
            return control != null && !string.IsNullOrEmpty(control.displayName) ? control.displayName : key.ToString();
        }

        /// <summary>The key's control, or null for None, unknown keys or no keyboard (the indexer would throw).</summary>
        private static KeyControl GetControl(Keyboard keyboard, Key key)
        {
            if (keyboard == null || key == Key.None)
            {
                return null;
            }

            int index = (int)key - 1;
            return index >= 0 && index < keyboard.allKeys.Count ? keyboard[key] : null;
        }
    }
}
