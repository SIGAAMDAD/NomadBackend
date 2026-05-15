using System;

namespace Samples.Unity.Events
{
    /// <summary>
    /// Payload describing damage dealt to the player.
    /// </summary>
    /// <remarks>
    /// This is a small immutable value type to keep GC pressure low when raised frequently.
    /// Requires C# 7.2+ for <c>readonly struct</c>.
    /// </remarks>
    public readonly struct DamagePlayerEventArgs
    {
        /// <summary>
        /// The amount of damage dealt to the player.
        /// </summary>
        public readonly int Amount;

        /// <summary>
        /// Creates a new <see cref="DamagePlayerEventArgs"/>.
        /// </summary>
        /// <param name="amount">The amount of damage dealt to the player.</param>
        public DamagePlayerEventArgs(int amount)
        {
            Amount = amount;
        }
    }
}