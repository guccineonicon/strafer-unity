using System;
using UnityEngine;

namespace Strafer.Characters
{
    /// <summary>
    /// Tracks a character's health and announces damage and death.
    ///
    /// This component only does bookkeeping. Whether a hit should be blocked,
    /// deflected, or ignored is decided by <see cref="StraferCharacter"/> before
    /// <see cref="ApplyDamage"/> is called.
    /// </summary>
    public class StraferHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)]
        private float maxHealth = 100f;

        /// <summary>Raised with the new health value and the change (negative for damage).</summary>
        public event Action<float, float> HealthChanged;

        /// <summary>Raised once when health reaches zero, with the character responsible (may be null).</summary>
        public event Action<StraferCharacter> Died;

        public float CurrentHealth { get; private set; }
        public float MaxHealth { get { return maxHealth; } }
        public bool IsDead { get { return CurrentHealth <= 0f; } }

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        /// <summary>Reduces health by <paramref name="amount"/>. Raises <see cref="Died"/> when health reaches zero.</summary>
        public void ApplyDamage(float amount, StraferCharacter instigator)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            float previous = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            if (HealthChanged != null)
            {
                HealthChanged(CurrentHealth, CurrentHealth - previous);
            }

            if (IsDead && Died != null)
            {
                Died(instigator);
            }
        }

        /// <summary>Restores health to <see cref="MaxHealth"/>. Used when respawning.</summary>
        public void ResetHealth()
        {
            float previous = CurrentHealth;
            CurrentHealth = maxHealth;
            if (HealthChanged != null)
            {
                HealthChanged(CurrentHealth, CurrentHealth - previous);
            }
        }
    }
}
