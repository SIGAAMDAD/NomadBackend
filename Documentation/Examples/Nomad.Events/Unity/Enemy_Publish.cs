using UnityEngine;
using Nomad.Core.Events;
using Samples.Events;

namespace Samples.Unity.Publisher
{
    /// <summary>
    /// Example publisher that fires the <c>Player/TakeDamage</c> event.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        // Event handle retrieved from the global registry.
        //! [declare]
        private IGameEvent<DamagePlayerEventArgs> _damagePlayer;
        //! [declare]

        [Header("Attack Settings")]
        [SerializeField, Min(1)]
        private int _damageAmount = 10;

        private void Awake()
        {
            //! [get_event]
            _damagePlayer = GameEventRegistry.GetEvent<DamagePlayerEventArgs>("TakeDamage", "Player");

            if (_damagePlayer == null)
            {
                Debug.LogError("[Enemy] TakeDamage event not found in registry (namespace 'Player').");
                enabled = false;
            }
            //! [get_event]
        }

        /// <summary>
        /// Simulates an attack—publishes an event that subscribers will handle.
        /// </summary>
        public void AttackPlayer()
        {
            if (_damagePlayer == null) return;

            //! [publish]
            var payload = new DamagePlayerEventArgs(_damageAmount);
            _damagePlayer.Publish(payload);
            //! [publish]

            Debug.Log($"[Enemy] Hit the player for {_damageAmount} damage!");
        }
    }
}