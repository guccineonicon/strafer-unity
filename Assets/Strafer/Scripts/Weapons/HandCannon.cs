using System;
using Strafer.Characters;
using Strafer.Core;
using Strafer.Visuals;
using UnityEngine;

namespace Strafer.Weapons
{
    /// <summary>
    /// Semi-automatic hitscan pistol.
    ///
    /// Fires a single ray from the character's eye along the view direction.
    /// Hits on a Strafer character are routed through
    /// <see cref="StraferCharacter.ReceiveHitscanHit"/> so deflection and invulnerability apply.
    /// </summary>
    public class HandCannon : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int magazineSize = 8;

        [SerializeField, Min(0.1f)]
        private float shotsPerSecond = 2.5f;

        [SerializeField, Min(0f), Tooltip("Seconds")]
        private float reloadDuration = 1.75f;

        [SerializeField, Min(0f)]
        private float damage = 50f;

        [SerializeField, Min(0f), Tooltip("Meters")]
        private float maxRange = 100f;

        [Header("Debug")]
        [SerializeField, Tooltip("Draws shot traces in the world. Useful until weapon effects exist.")]
        private bool drawDebugTraces = true;

        /// <summary>Raised with the current ammo and magazine size whenever ammo changes.</summary>
        public event Action<int, int> AmmoChanged;

        /// <summary>Raised after each shot is fired.</summary>
        public event Action Fired;

        public event Action ReloadStarted;
        public event Action ReloadFinished;

        private StraferCharacter character;
        private float nextShotTime;
        private float reloadStartTime;

        public int CurrentAmmo { get; private set; }
        public int MagazineSize { get { return magazineSize; } }
        public bool IsReloading { get; private set; }

        /// <summary>Reload completion from 0 to 1, or 0 when not reloading.</summary>
        public float ReloadProgress
        {
            get
            {
                if (!IsReloading || reloadDuration <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration);
            }
        }

        private void Awake()
        {
            character = GetComponent<StraferCharacter>();
            SetAmmo(magazineSize);
        }

        private void Update()
        {
            if (IsReloading && Time.time >= reloadStartTime + reloadDuration)
            {
                FinishReload();
            }
        }

        /// <summary>Fires one shot if the weapon is ready. Starts a reload when the magazine is empty.</summary>
        public bool TryFire()
        {
            if (character == null || !character.CanAct || IsReloading)
            {
                return false;
            }

            if (CurrentAmmo <= 0)
            {
                TryReload();
                return false;
            }

            if (Time.time < nextShotTime)
            {
                return false;
            }

            nextShotTime = Time.time + 1f / shotsPerSecond;
            SetAmmo(CurrentAmmo - 1);

            Transform eye = character.Eye;
            Vector3 origin = eye.position;
            Vector3 direction = eye.forward;

            RaycastHit hit;
            bool didHit = StraferPhysics.Raycast(origin, direction, maxRange, transform, out hit);

            if (didHit)
            {
                StraferCharacter target = hit.collider.GetComponentInParent<StraferCharacter>();
                if (target != null)
                {
                    target.ReceiveHitscanHit(character, damage, true);
                }
            }

            if (drawDebugTraces)
            {
                Vector3 impactPoint = didHit ? hit.point : origin + direction * maxRange;

                // Start the line slightly below the eye so it is visible from first person.
                StraferDebugDraw.Line(origin - eye.up * 0.05f, impactPoint, new Color(1f, 0.55f, 0f), 0.5f);
                if (didHit)
                {
                    StraferDebugDraw.Point(impactPoint, Color.red, 0.5f);
                }
            }

            if (Fired != null)
            {
                Fired();
            }

            if (CurrentAmmo <= 0)
            {
                TryReload();
            }

            return true;
        }

        /// <summary>Starts a reload if the magazine is not full and no reload is in progress.</summary>
        public bool TryReload()
        {
            if (character == null || !character.CanAct || IsReloading || CurrentAmmo >= magazineSize)
            {
                return false;
            }

            IsReloading = true;
            reloadStartTime = Time.time;
            if (ReloadStarted != null)
            {
                ReloadStarted();
            }

            return true;
        }

        /// <summary>Stops an in-progress reload without refilling the magazine.</summary>
        public void CancelReload()
        {
            IsReloading = false;
        }

        /// <summary>Fills the magazine instantly. Used when respawning.</summary>
        public void RefillMagazine()
        {
            CancelReload();
            SetAmmo(magazineSize);
        }

        private void FinishReload()
        {
            IsReloading = false;
            SetAmmo(magazineSize);
            if (ReloadFinished != null)
            {
                ReloadFinished();
            }
        }

        private void SetAmmo(int newAmmo)
        {
            CurrentAmmo = Mathf.Clamp(newAmmo, 0, magazineSize);
            if (AmmoChanged != null)
            {
                AmmoChanged(CurrentAmmo, magazineSize);
            }
        }
    }
}
