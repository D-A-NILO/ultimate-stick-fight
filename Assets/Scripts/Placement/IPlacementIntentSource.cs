namespace Wreckyard.Core
{
    /// <summary>
    /// Something that turns a device (keyboard, mouse, gamepad) into a <see cref="PlacementIntent"/>.
    /// </summary>
    public interface IPlacementIntentSource
    {
        /// <summary>
        /// Returns this frame's intent. The consumer calls it exactly once per frame, so sources may advance
        /// their own timers here.
        /// </summary>
        PlacementIntent ReadIntent();
    }
}
