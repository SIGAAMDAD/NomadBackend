using UnityEngine;
using Nomad.Core.Events;
using Samples.Events;

namespace Samples.Unity.Subscriber
{
    /// <summary>
    /// Example subscriber that listens for <c>Player/TakeDamage</c> events
    /// and reduces the player's health.
    /// </summary>
    /// <remarks>
    /// Demonstrates subscription in <see cref="Awake"/> and unsubscription in <see cref="OnDestroy"/>.
    /// </remarks>
    public sealed class Player : MonoBehaviour
    {
        [SerializeField, Min(0)]
        private int _health = 100;

        // Backing field for the event instance.
        private IGameEvent<DamagePlayerEventArgs> _takeDamage;

        /// <summary>
        /// Retrieve and subscribe to the global event.
        /// </summary>
        private void Awake()
        {
            //! [get_event]
            _takeDamage = GameEventRegistry.GetEvent<DamagePlayerEventArgs>("TakeDamage", "Player");

            if (_takeDamage == null)
            {
                Debug.LogError("[Player] TakeDamage event not found in registry (namespace 'Player').");
                enabled = false;
                return;
            }
            //! [get_event]

            //! [subscribe]
            // Subscribe so OnTakeDamage is invoked whenever the event is published.
            _takeDamage.Subscribe(this, OnTakeDamage);
            //! [subscribe]
        }

        /// <summary>
        /// Always unsubscribe to avoid leaks and dangling references.
        /// </summary>
        private void OnDestroy()
        {
            if (_takeDamage != null)
            {
                _takeDamage.Unsubscribe(this, OnTakeDamage);
            }
        }

        /// <summary>
        /// Handles the damage event.
        /// </summary>
        /// <param name="args">Damage payload (passed by <c>in</c> to avoid copying).</param>
        private void OnTakeDamage(in DamagePlayerEventArgs args)
        {
            _health = Mathf.Max(0, _health - args.Amount);
            Debug.Log($"[Player] Took {args.Amount} damage. Health now: {_health}.");
        }
    }
}