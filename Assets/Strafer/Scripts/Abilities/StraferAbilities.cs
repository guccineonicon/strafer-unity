using System;
using Strafer.Characters;
using Strafer.Core;
using Strafer.Visuals;
using UnityEngine;

namespace Strafer.Abilities
{
    /// <summary>The four abilities every Strafer character has.</summary>
    public enum StraferAbility
    {
        Dash,
        Deflect,
        Stun,
        Roll
    }

    /// <summary>
    /// Runs the character's four abilities and their cooldowns.
    ///
    /// Dash and Roll are "movement bursts": the character travels a fixed distance
    /// at a fixed speed in the current input direction, ignoring normal movement
    /// physics until the distance is covered. Deflect opens a short window in which
    /// incoming hitscan shots are reflected. Stun sweeps forward with the whip and
    /// stuns the first character it hits.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class StraferAbilities : MonoBehaviour
    {
        public const int AbilityCount = 4;

        private enum MovementBurst
        {
            None,
            Dash,
            Roll
        }

        [Header("Dash: fast, invulnerable burst")]
        [SerializeField, Min(0f), Tooltip("Meters")]
        private float dashDistance = 8f;

        [SerializeField, Min(1f), Tooltip("Source/Quake units per second")]
        private float dashSpeedUps = 640f;

        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float dashCooldown = 2f;

        [Header("Roll: shorter, slower burst with no invulnerability")]
        [SerializeField, Min(0f), Tooltip("Meters")]
        private float rollDistance = 6f;

        [SerializeField, Min(1f), Tooltip("Source/Quake units per second")]
        private float rollSpeedUps = 460f;

        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float rollCooldown = 2f;

        [Header("Deflect: hitscan shots landing during the window are reflected to the shooter")]
        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float deflectWindow = 0.5f;

        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float deflectCooldown = 2f;

        [Header("Stun: whip strike in front of the character")]
        [SerializeField, Min(0f), Tooltip("Meters")]
        private float stunRange = 4f;

        [SerializeField, Min(0f), Tooltip("Radius of the whip sweep in meters. Larger values make the whip more forgiving to aim.")]
        private float stunSweepRadius = 0.25f;

        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float stunDuration = 1f;

        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float stunCooldown = 3f;

        [Header("Debug")]
        [SerializeField, Tooltip("Draws the whip sweep in the world. Useful until whip effects exist.")]
        private bool drawDebugTraces = true;

        /// <summary>Raised after an ability successfully activates.</summary>
        public event Action<StraferAbility> AbilityActivated;

        private StraferCharacter character;

        // Time at which each ability comes off cooldown, indexed by StraferAbility.
        private readonly float[] cooldownEndTimes = new float[AbilityCount];

        private MovementBurst activeBurst = MovementBurst.None;
        private Vector3 burstDirection = Vector3.forward;
        private float burstSpeed;
        private float burstDistanceRemaining;

        private float deflectEndTime;
        private float stunEndTime;

        /// <summary>Reach of the whip strike, used by the first-person whip visual.</summary>
        public float StunRange { get { return stunRange; } }

        public bool IsInvulnerable { get { return activeBurst == MovementBurst.Dash; } }
        public bool IsDeflecting { get { return Time.time < deflectEndTime; } }
        public bool IsStunned { get { return Time.time < stunEndTime; } }
        public bool IsMovementBurstActive { get { return activeBurst != MovementBurst.None; } }

        private void Awake()
        {
            character = GetComponent<StraferCharacter>();
        }

        private void Update()
        {
            if (IsMovementBurstActive)
            {
                UpdateMovementBurst(Time.deltaTime);
            }
        }

        /// <summary>Activates <paramref name="ability"/> if it is off cooldown and the character is able to act.</summary>
        public bool TryActivate(StraferAbility ability)
        {
            if (character == null || !character.CanAct || IsOnCooldown(ability))
            {
                return false;
            }

            bool activated;
            switch (ability)
            {
                case StraferAbility.Dash: activated = ActivateDash(); break;
                case StraferAbility.Roll: activated = ActivateRoll(); break;
                case StraferAbility.Deflect: activated = ActivateDeflect(); break;
                case StraferAbility.Stun: activated = ActivateStun(); break;
                default: activated = false; break;
            }

            if (activated)
            {
                cooldownEndTimes[(int)ability] = Time.time + GetCooldownDuration(ability);
                if (AbilityActivated != null)
                {
                    AbilityActivated(ability);
                }
            }

            return activated;
        }

        /// <summary>Clears every cooldown so all abilities are immediately available.</summary>
        public void ResetAllCooldowns()
        {
            for (int i = 0; i < cooldownEndTimes.Length; i++)
            {
                cooldownEndTimes[i] = 0f;
            }
        }

        /// <summary>
        /// Stuns the character for <paramref name="duration"/> seconds, blocking movement, firing, and abilities.
        /// Has no effect while the character is invulnerable or dead.
        /// </summary>
        /// <returns>True if the stun was applied.</returns>
        public bool ApplyStun(float duration)
        {
            if (character == null || IsInvulnerable || character.Health.IsDead)
            {
                return false;
            }

            // A roll in progress is interrupted. Dash cannot reach here because it grants invulnerability.
            if (IsMovementBurstActive)
            {
                EndMovementBurst();
            }

            stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);

            // Stop horizontal movement dead. Vertical speed is kept so an airborne target still falls.
            Vector3 velocity = character.Motor.Velocity;
            character.Motor.Velocity = new Vector3(0f, Mathf.Min(velocity.y, 0f), 0f);
            return true;
        }

        /// <summary>Ends any active burst, deflect window, or stun. Used on death.</summary>
        public void CancelActiveEffects()
        {
            if (IsMovementBurstActive)
            {
                EndMovementBurst();
            }

            deflectEndTime = 0f;
            stunEndTime = 0f;
        }

        public float GetCooldownRemaining(StraferAbility ability)
        {
            return Mathf.Max(0f, cooldownEndTimes[(int)ability] - Time.time);
        }

        public float GetCooldownDuration(StraferAbility ability)
        {
            switch (ability)
            {
                case StraferAbility.Dash: return dashCooldown;
                case StraferAbility.Roll: return rollCooldown;
                case StraferAbility.Deflect: return deflectCooldown;
                case StraferAbility.Stun: return stunCooldown;
                default: return 0f;
            }
        }

        private bool IsOnCooldown(StraferAbility ability)
        {
            return GetCooldownRemaining(ability) > 0f;
        }

        private bool ActivateDash()
        {
            if (IsMovementBurstActive)
            {
                return false;
            }

            StartMovementBurst(MovementBurst.Dash, dashDistance, dashSpeedUps);
            return true;
        }

        private bool ActivateRoll()
        {
            if (IsMovementBurstActive)
            {
                return false;
            }

            StartMovementBurst(MovementBurst.Roll, rollDistance, rollSpeedUps);
            return true;
        }

        private bool ActivateDeflect()
        {
            deflectEndTime = Time.time + deflectWindow;
            return true;
        }

        private bool ActivateStun()
        {
            Transform eye = character.Eye;
            Vector3 origin = eye.position;
            Vector3 direction = eye.forward;

            RaycastHit hit;
            bool didHit = StraferPhysics.SphereCast(origin, stunSweepRadius, direction, stunRange, transform, out hit);

            if (didHit)
            {
                StraferCharacter target = hit.collider.GetComponentInParent<StraferCharacter>();
                if (target != null)
                {
                    target.Abilities.ApplyStun(stunDuration);
                }
            }

            if (drawDebugTraces)
            {
                // A hit that starts inside a collider reports zero distance, so fall back to the sweep origin.
                Vector3 endPoint = didHit ? origin + direction * hit.distance : origin + direction * stunRange;
                StraferDebugDraw.Line(origin + direction * 0.3f, endPoint,
                    didHit ? Color.yellow : Color.gray, 0.4f, stunSweepRadius * 2f);
            }

            // A whiff still uses the ability, so missed stuns carry a cost.
            return true;
        }

        private void StartMovementBurst(MovementBurst type, float distance, float speedUps)
        {
            activeBurst = type;
            burstDirection = character.GetDesiredMoveDirection();
            burstSpeed = StraferUnits.UpsToMetersPerSecond(speedUps);
            burstDistanceRemaining = distance;

            // Suspending the motor stops it applying gravity, friction, or input
            // while this component moves the character directly.
            character.Motor.ResetMotion();
            character.Motor.Suspended = true;

            StraferSpeedEffect speedEffect = GetComponent<StraferSpeedEffect>();
            if (speedEffect != null)
            {
                speedEffect.SetActive(true);
            }
        }

        private void UpdateMovementBurst(float deltaTime)
        {
            float stepDistance = Mathf.Min(burstSpeed * deltaTime, burstDistanceRemaining);

            // The CharacterController slides along walls instead of stopping dead,
            // which keeps bursts usable near cover.
            character.Motor.MoveDirect(burstDirection * stepDistance);

            // Report the burst velocity so other systems see the character moving.
            character.Motor.Velocity = burstDirection * burstSpeed;

            burstDistanceRemaining -= stepDistance;
            if (burstDistanceRemaining <= 1e-4f)
            {
                EndMovementBurst();
            }
        }

        private void EndMovementBurst()
        {
            activeBurst = MovementBurst.None;
            burstDistanceRemaining = 0f;

            // Hand control back to normal physics. Exit speed is capped at walk speed
            // so bursts cannot be chained into runaway momentum.
            character.Motor.Suspended = false;
            character.Motor.Velocity = burstDirection * character.WalkSpeed;

            StraferSpeedEffect speedEffect = GetComponent<StraferSpeedEffect>();
            if (speedEffect != null)
            {
                speedEffect.SetActive(false);
            }
        }
    }
}
