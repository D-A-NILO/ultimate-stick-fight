using UnityEngine;

namespace Wreckyard.Core
{
    /// <summary>
    /// Turns a held direction into discrete steps: one step as soon as the direction changes, then one after
    /// an initial delay, then one at a fixed rate while it stays held. At most one step per call, so a long
    /// frame never jumps several cells. Plain C#, so any input source can reuse it.
    /// </summary>
    public sealed class HoldRepeat
    {
        private Vector2Int heldDirection;
        private float timeUntilRepeat;

        /// <summary>
        /// Advances by <paramref name="deltaTime"/> and returns the step to take now, or zero.
        /// A <paramref name="repeatRate"/> (steps per second) of 0 or less disables repeating.
        /// </summary>
        public Vector2Int Tick(Vector2Int direction, float deltaTime, float initialDelay, float repeatRate)
        {
            if (direction == Vector2Int.zero)
            {
                Reset();
                return Vector2Int.zero;
            }

            if (direction != heldDirection)
            {
                heldDirection = direction;
                timeUntilRepeat = Mathf.Max(0f, initialDelay);
                return direction;
            }

            if (repeatRate <= 0f)
            {
                // Repeating is off: one step per press only.
                return Vector2Int.zero;
            }

            timeUntilRepeat -= deltaTime;
            if (timeUntilRepeat > 0f)
            {
                return Vector2Int.zero;
            }

            // Keep the leftover time for an even rhythm, but never build up a backlog of steps.
            timeUntilRepeat = Mathf.Max(0f, timeUntilRepeat + 1f / repeatRate);
            return direction;
        }

        /// <summary>Forgets the held direction, so the next non-zero direction steps immediately.</summary>
        public void Reset()
        {
            heldDirection = Vector2Int.zero;
            timeUntilRepeat = 0f;
        }
    }
}
