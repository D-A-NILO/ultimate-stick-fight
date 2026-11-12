namespace Wreckyard.Core
{
    /// <summary>Item families from the GDD item list.</summary>
    public enum ItemCategory
    {
        /// <summary>Blocks and platforms players stand on.</summary>
        Platform,

        /// <summary>Items that push, launch or change how players move.</summary>
        Physics,

        /// <summary>Items that damage players.</summary>
        Trap,

        /// <summary>Weapon spawners.</summary>
        Spawner,

        /// <summary>Special items such as the bomb.</summary>
        Wildcard
    }
}
