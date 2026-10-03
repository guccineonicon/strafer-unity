using System;
using Strafer.Abilities;
using Strafer.Core;
using Strafer.UI;
using Strafer.Visuals;
using Strafer.Weapons;
using UnityEngine;

namespace Strafer.Characters
{
    /// <summary>Outcome of a hitscan shot reaching a character.</summary>
    public enum HitResponse
    {
        /// <summary>The shot dealt damage to the target.</summary>
        Damaged,
        /// <summary>The target's deflect window was open, so the shot was sent back to the shooter.</summary>
        Deflected,
        /// <summary>The shot had no effect because the target was invulnerable or already dead.</summary>
        Ignored
    }

    /// <summary>
    /// First-person Strafer character, used both for the local player and for practice dummies.
    ///
    /// Assembles every component the character needs at startup, so an empty
    /// GameObject with this component is a complete character. Components already
    /// on the GameObject (for example on a prefab) are kept, which is how their
    /// tuning values can be customized.
    ///
    /// Also acts as the single entry point for incoming hitscan hits so
    /// invulnerability and deflection are resolved in one place.
    ///
    /// The transform's position is at the character's feet, and its yaw is the
    /// direction the character faces. View pitch lives on the <see cref="Eye"/> child.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class StraferCharacter : MonoBehaviour
    {
        // Capsule sized for a slim, average-height character (about 1.76 m tall, 0.6 m wide).
        private const float CapsuleRadius = 0.30f;
        private const float CapsuleHeight = 1.76f;

        // Puts the camera about 1.6 m off the ground.
        private const float EyeHeight = 1.60f;

        /// <summary>Raised whenever any character dies, with the victim and the killer (may be null).</summary>
        public static event Action<StraferCharacter, StraferCharacter> AnyCharacterDied;

        [Tooltip("Controlled by the local player's mouse and keyboard. Off for practice dummies.")]
        [SerializeField]
        private bool playerControlled;

        [Tooltip("Base ground speed in Source/Quake units per second.")]
        [SerializeField, Min(0f)]
        private float walkSpeedUps = 360f;

        [Tooltip("Horizontal field of view of the player camera, in degrees.")]
        [SerializeField, Range(60f, 130f)]
        private float horizontalFieldOfView = 90f;

        private CharacterController characterController;
        private float pitch;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        public bool PlayerControlled
        {
            get { return playerControlled; }
            set { playerControlled = value; }
        }

        public StraferHealth Health { get; private set; }
        public StraferAbilities Abilities { get; private set; }
        public HandCannon HandCannon { get; private set; }
        public StraferMotor Motor { get; private set; }

        /// <summary>Pivot at eye height. Its forward direction is where the character is looking.</summary>
        public Transform Eye { get; private set; }

        /// <summary>The player's camera. Null for practice dummies.</summary>
        public Camera ViewCamera { get; private set; }

        /// <summary>Kills scored by this character.</summary>
        public int Score { get; set; }

        /// <summary>Combined movement input from the held direction keys (x = right, y = forward).</summary>
        public Vector2 MoveInput { get; set; }

        /// <summary>Walk speed converted to meters per second.</summary>
        public float WalkSpeed { get { return StraferUnits.UpsToMetersPerSecond(walkSpeedUps); } }

        public float HorizontalFieldOfView { get { return horizontalFieldOfView; } }

        /// <summary>True when the character is alive and not stunned. Gates movement, firing, and abilities.</summary>
        public bool CanAct
        {
            get { return !Health.IsDead && !Abilities.IsStunned; }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.radius = CapsuleRadius;
            characterController.height = CapsuleHeight;
            characterController.center = new Vector3(0f, CapsuleHeight * 0.5f, 0f);
            characterController.skinWidth = 0.03f;

            // Matches Unreal's default 45 cm step height and 44.77 degree walkable slope.
            characterController.stepOffset = 0.45f;
            characterController.slopeLimit = 44.77f;

            Eye = transform.Find("Eye");
            if (Eye == null)
            {
                Eye = new GameObject("Eye").transform;
                Eye.SetParent(transform, false);
            }
            Eye.localPosition = new Vector3(0f, EyeHeight, 0f);
            Eye.localRotation = Quaternion.identity;

            Health = GetOrAdd<StraferHealth>();
            Abilities = GetOrAdd<StraferAbilities>();
            HandCannon = GetOrAdd<HandCannon>();
            Motor = GetOrAdd<StraferMotor>();
            GetOrAdd<StraferBody>();

            if (playerControlled)
            {
                SetUpPlayerView();
            }

            Health.Died += HandleDeath;
            HandCannon.ReloadStarted += HandleReloadStarted;
        }

        private void Start()
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void OnDestroy()
        {
            Health.Died -= HandleDeath;
            HandCannon.ReloadStarted -= HandleReloadStarted;
        }

        private void Update()
        {
            if (ViewCamera != null)
            {
                // Unity cameras take a vertical field of view, so convert from the
                // horizontal value every frame in case the window was resized.
                ViewCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(horizontalFieldOfView, ViewCamera.aspect);
            }
        }

        /// <summary>
        /// Resolves a hitscan hit against this character.
        /// </summary>
        /// <param name="shooter">Character that fired the shot.</param>
        /// <param name="damage">Damage the shot deals if it lands.</param>
        /// <param name="canBeDeflected">False for shots that were already deflected, preventing endless reflection.</param>
        public HitResponse ReceiveHitscanHit(StraferCharacter shooter, float damage, bool canBeDeflected)
        {
            if (Health.IsDead || Abilities.IsInvulnerable)
            {
                return HitResponse.Ignored;
            }

            if (canBeDeflected && Abilities.IsDeflecting && shooter != null && shooter != this)
            {
                // The reflected shot cannot be deflected again, so two deflecting players cannot loop forever.
                shooter.ReceiveHitscanHit(this, damage, false);
                return HitResponse.Deflected;
            }

            Health.ApplyDamage(damage, shooter);
            return HitResponse.Damaged;
        }

        /// <summary>
        /// Rotates the view. Yaw turns the whole character; pitch tilts only the eye
        /// and is clamped so the player cannot look past straight up or down.
        /// </summary>
        public void AddLook(float yawDegrees, float pitchDegrees)
        {
            transform.Rotate(0f, yawDegrees, 0f, Space.World);
            pitch = Mathf.Clamp(pitch + pitchDegrees, -89f, 89f);

            // Unity's X rotation tilts the view down for positive angles, so pitch is negated.
            Eye.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
        }

        /// <summary>
        /// World-space horizontal direction of the current movement input.
        /// Falls back to the facing direction when there is no input.
        /// </summary>
        public Vector3 GetDesiredMoveDirection()
        {
            Vector3 direction = transform.forward * MoveInput.y + transform.right * MoveInput.x;
            direction.y = 0f;
            return direction.sqrMagnitude < 1e-6f ? FlatForward() : direction.normalized;
        }

        /// <summary>World-space movement input scaled by how far the stick or keys are pushed (zero to one).</summary>
        public Vector3 GetMoveInputWorld()
        {
            Vector3 direction = transform.forward * MoveInput.y + transform.right * MoveInput.x;
            direction.y = 0f;
            return Vector3.ClampMagnitude(direction, 1f);
        }

        /// <summary>Brings a dead character back at its original spawn point. Used for practice dummies.</summary>
        public void Revive()
        {
            Revive(spawnPosition, spawnRotation);
        }

        /// <summary>Brings a dead character back at the given spot with full health, ammo, and abilities.</summary>
        public void Revive(Vector3 position, Quaternion rotation)
        {
            // A CharacterController overrides direct transform changes while enabled.
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            pitch = 0f;
            Eye.localRotation = Quaternion.identity;
            characterController.enabled = true;

            SetVisible(true);
            Motor.ResetMotion();
            Health.ResetHealth();
            HandCannon.RefillMagazine();
            Abilities.ResetAllCooldowns();
        }

        private void HandleDeath(StraferCharacter killer)
        {
            Abilities.CancelActiveEffects();
            HandCannon.CancelReload();
            Motor.ResetMotion();
            MoveInput = Vector2.zero;

            // Disabling the controller also removes its collider, so shots pass through.
            characterController.enabled = false;
            SetVisible(false);

            if (AnyCharacterDied != null)
            {
                AnyCharacterDied(this, killer);
            }
        }

        private void HandleReloadStarted()
        {
            // Design rule: starting a reload refreshes every ability so it can be used during the reload.
            Abilities.ResetAllCooldowns();
        }

        private void SetVisible(bool visible)
        {
            StraferBody body = GetComponent<StraferBody>();
            if (body != null)
            {
                body.SetVisible(visible);
            }

            StraferViewmodel viewmodel = GetComponent<StraferViewmodel>();
            if (viewmodel != null)
            {
                viewmodel.SetVisible(visible);
            }
        }

        private void SetUpPlayerView()
        {
            ViewCamera = Eye.GetComponentInChildren<Camera>();
            if (ViewCamera == null)
            {
                GameObject cameraObject = new GameObject("PlayerCamera");
                cameraObject.transform.SetParent(Eye, false);
                cameraObject.tag = "MainCamera";
                ViewCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            // The first-person weapons sit about 30 cm from the eye, inside the default 30 cm near plane.
            ViewCamera.nearClipPlane = 0.01f;

            GetOrAdd<StraferPlayerInput>();
            GetOrAdd<StraferViewmodel>();
            GetOrAdd<StraferSpeedEffect>();
            GetOrAdd<StraferHud>();
            GetOrAdd<StraferSettingsMenu>();
        }

        private Vector3 FlatForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < 1e-6f ? Vector3.forward : forward.normalized;
        }

        private T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            AnyCharacterDied = null;
        }
    }
}
