using System.Collections.Generic;
using Strafer.Abilities;
using Strafer.Characters;
using Strafer.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Strafer.Visuals
{
    /// <summary>
    /// First-person weapon visuals, attached to the player camera. Only the local
    /// player has one. Built from basic shapes as placeholder art.
    ///
    /// - Right hand: hand cannon that kicks back when fired and dips while reloading.
    ///   Assign <see cref="gunModel"/> to replace the placeholder shapes.
    /// - Left hand: segmented sword-whip that lashes out to full stun range when
    ///   Stun is used and spins in front of the camera while Deflect is active.
    ///
    /// Offsets are relative to the camera: x = right, y = up, z = forward, in meters.
    /// </summary>
    public class StraferViewmodel : MonoBehaviour
    {
        private const int WhipSegmentCount = 8;
        private const float WhipSegmentLength = 0.06f;
        private const float WhipSegmentGap = 0.002f;
        private const float WhipSegmentWidth = 0.012f;
        private const float WhipSegmentHeight = 0.025f;

        // Distance from the whip root to where the blade starts (past the handle and guard).
        private const float WhipBladeStart = 0.11f;

        // Cylinders run along their local Y axis; tipping them 90 degrees forward lays them along Z.
        private static readonly Vector3 AlongForward = new Vector3(90f, 0f, 0f);

        private static readonly Color GunMetalColor = StraferMath.LinearColor(0.05f, 0.05f, 0.06f);
        private static readonly Color GunDetailColor = StraferMath.LinearColor(0.18f, 0.18f, 0.20f);
        private static readonly Color WhipHandleColor = StraferMath.LinearColor(0.25f, 0.15f, 0.08f);
        private static readonly Color WhipBladeColor = StraferMath.LinearColor(0.70f, 0.72f, 0.78f);

        [Header("Gun")]
        [SerializeField, Tooltip("Hand cannon rest position relative to the camera.")]
        private Vector3 gunOffset = new Vector3(0.14f, -0.14f, 0.32f);

        [SerializeField, Tooltip("Optional model (prefab or imported mesh) used in place of the placeholder gun shapes.")]
        private GameObject gunModel;

        [SerializeField, Tooltip("Position of the custom gun model relative to the gun's rest position. Use to line up a model whose pivot differs.")]
        private Vector3 gunModelPosition = Vector3.zero;

        [SerializeField, Tooltip("Rotation of the custom gun model, in degrees. Use to line up a model that does not face +Z.")]
        private Vector3 gunModelRotation = Vector3.zero;

        [SerializeField, Tooltip("Scale of the custom gun model.")]
        private Vector3 gunModelScale = Vector3.one;

        [Header("Whip")]
        [SerializeField, Tooltip("Whip rest position relative to the camera.")]
        private Vector3 whipOffset = new Vector3(-0.16f, -0.15f, 0.30f);

        [SerializeField, Tooltip("Whip position while deflecting, centered so the spin covers the view.")]
        private Vector3 whipDeflectOffset = new Vector3(0f, -0.04f, 0.40f);

        [SerializeField, Min(0.05f), Tooltip("Total time in seconds for the stun lash to extend and retract.")]
        private float lashDuration = 0.25f;

        [SerializeField, Tooltip("Spin rate of the whip during the deflect window, in degrees per second.")]
        private float deflectSpinDegreesPerSecond = 1440f;

        private StraferCharacter character;
        private Transform root;
        private Transform gunRoot;
        private Transform whipRoot;

        // Blade segments, ordered from the hilt outward.
        private readonly List<Transform> whipSegments = new List<Transform>();

        private float recoilAlpha;
        private float reloadAlpha;
        private float deflectAlpha;
        private float whipSpinAngle;

        // Seconds since the current lash started, or negative when no lash is playing.
        private float lashElapsed = -1f;

        private void Start()
        {
            character = GetComponent<StraferCharacter>();
            if (character == null || character.ViewCamera == null)
            {
                enabled = false;
                return;
            }

            root = new GameObject("Viewmodel").transform;
            root.SetParent(character.ViewCamera.transform, false);

            BuildGun();
            BuildWhip();

            character.HandCannon.Fired += HandleWeaponFired;
            character.Abilities.AbilityActivated += HandleAbilityActivated;
        }

        private void OnDestroy()
        {
            if (character != null)
            {
                character.HandCannon.Fired -= HandleWeaponFired;
                character.Abilities.AbilityActivated -= HandleAbilityActivated;
            }
        }

        private void Update()
        {
            if (gunRoot == null || whipRoot == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            UpdateGun(deltaTime);
            UpdateWhip(deltaTime);
        }

        /// <summary>Shows or hides the weapons. Used when the character dies and respawns.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.gameObject.SetActive(visible);
            }
        }

        private void BuildGun()
        {
            gunRoot = new GameObject("Gun").transform;
            gunRoot.SetParent(root, false);
            gunRoot.localPosition = gunOffset;

            if (BuildCustomGunModel())
            {
                return;
            }

            // A chunky revolver-style hand cannon, about 40 cm from grip to muzzle.
            AddViewmodelPart(gunRoot, "Frame", PrimitiveShape.Cube, Vector3.zero, Vector3.zero,
                new Vector3(0.045f, 0.07f, 0.24f), GunMetalColor);
            AddViewmodelPart(gunRoot, "Barrel", PrimitiveShape.Cylinder, new Vector3(0f, 0.015f, 0.16f), AlongForward,
                new Vector3(0.035f, 0.18f, 0.035f), GunMetalColor);
            AddViewmodelPart(gunRoot, "Drum", PrimitiveShape.Cylinder, new Vector3(0f, 0.005f, 0.02f), AlongForward,
                new Vector3(0.07f, 0.07f, 0.07f), GunDetailColor);
            AddViewmodelPart(gunRoot, "Grip", PrimitiveShape.Cube, new Vector3(0f, -0.08f, -0.08f), new Vector3(-15f, 0f, 0f),
                new Vector3(0.04f, 0.11f, 0.05f), GunDetailColor);
            AddViewmodelPart(gunRoot, "Sight", PrimitiveShape.Cube, new Vector3(0f, 0.04f, 0.23f), Vector3.zero,
                new Vector3(0.01f, 0.015f, 0.01f), GunMetalColor);
        }

        /// <summary>Creates the custom gun model if one is assigned. Returns false when the placeholder should be used.</summary>
        private bool BuildCustomGunModel()
        {
            if (gunModel == null)
            {
                return false;
            }

            GameObject model = Instantiate(gunModel, gunRoot, false);
            model.name = "GunModel";
            model.transform.localPosition = gunModelPosition;
            model.transform.localRotation = Quaternion.Euler(gunModelRotation);
            model.transform.localScale = gunModelScale;

            // Viewmodel parts must never block shots or cast shadows onto the world.
            foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>())
            {
                Destroy(modelCollider);
            }

            foreach (Renderer modelRenderer in model.GetComponentsInChildren<Renderer>())
            {
                modelRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            return true;
        }

        private void BuildWhip()
        {
            whipRoot = new GameObject("Whip").transform;
            whipRoot.SetParent(root, false);
            whipRoot.localPosition = whipOffset;

            AddViewmodelPart(whipRoot, "Handle", PrimitiveShape.Cylinder, Vector3.zero, AlongForward,
                new Vector3(0.03f, 0.14f, 0.03f), WhipHandleColor);
            AddViewmodelPart(whipRoot, "Guard", PrimitiveShape.Cube, new Vector3(0f, 0f, 0.075f), Vector3.zero,
                new Vector3(0.08f, 0.02f, 0.02f), WhipBladeColor);

            // When retracted, the segments sit end to end and read as a short sword blade.
            for (int i = 0; i < WhipSegmentCount; i++)
            {
                Transform segment = AddViewmodelPart(whipRoot, "Segment" + i, PrimitiveShape.Cube,
                    new Vector3(0f, 0f, RetractedSegmentCenter(i)), Vector3.zero,
                    new Vector3(WhipSegmentWidth, WhipSegmentHeight, WhipSegmentLength), WhipBladeColor);
                whipSegments.Add(segment);
            }
        }

        private void UpdateGun(float deltaTime)
        {
            bool reloading = character.HandCannon.IsReloading;

            recoilAlpha = StraferMath.InterpTo(recoilAlpha, 0f, deltaTime, 14f);
            reloadAlpha = StraferMath.InterpTo(reloadAlpha, reloading ? 1f : 0f, deltaTime, 10f);

            // Recoil kicks the gun back and tips the muzzle up; reloading drops it, tips the muzzle
            // down, and rolls it inward. Negative X rotation tips the muzzle up in Unity.
            gunRoot.localPosition = gunOffset + new Vector3(0f, -0.10f * reloadAlpha, -0.06f * recoilAlpha);
            gunRoot.localRotation = Quaternion.Euler(
                -(14f * recoilAlpha - 30f * reloadAlpha),
                0f,
                -25f * reloadAlpha);
        }

        private void UpdateWhip(float deltaTime)
        {
            bool deflecting = character.Abilities.IsDeflecting;

            // Lash extension rises from 0 to 1 and back over the lash duration.
            float extension = 0f;
            float lashProgress = 0f;
            if (lashElapsed >= 0f)
            {
                lashElapsed += deltaTime;
                lashProgress = lashElapsed / lashDuration;
                if (lashProgress >= 1f)
                {
                    lashElapsed = -1f;
                    lashProgress = 0f;
                }
                else
                {
                    extension = Mathf.Sin(lashProgress * Mathf.PI);
                }
            }

            deflectAlpha = StraferMath.InterpTo(deflectAlpha, deflecting ? 1f : 0f, deltaTime, 18f);
            if (deflecting)
            {
                whipSpinAngle = Mathf.Repeat(whipSpinAngle + deflectSpinDegreesPerSecond * deltaTime, 360f);
            }

            // Full reach measured from the whip root, so the tip lands at the stun range from the eyes.
            float fullReach = Mathf.Max(character.Abilities.StunRange - whipOffset.z - WhipBladeStart, 0.5f);
            float reach = fullReach * extension;

            // Angle the lash toward the crosshair, since the whip is held off to the lower left.
            float aimYaw = Mathf.Atan2(-whipOffset.x, fullReach) * Mathf.Rad2Deg * extension;
            float aimPitchUp = Mathf.Atan2(-whipOffset.y, fullReach) * Mathf.Rad2Deg * extension;

            // While deflecting, turn the blade sideways and spin it like a propeller across the view.
            Quaternion aimRotation = Quaternion.Euler(-aimPitchUp, aimYaw, 0f);
            Quaternion sidewaysRotation = Quaternion.AngleAxis(90f * deflectAlpha, Vector3.up);
            Quaternion spinRotation = Quaternion.AngleAxis(whipSpinAngle * deflectAlpha, Vector3.forward);

            whipRoot.localPosition = Vector3.Lerp(whipOffset, whipDeflectOffset, deflectAlpha);
            whipRoot.localRotation = spinRotation * sidewaysRotation * aimRotation;

            // Spread the segments out along the lash, stretching each so the blade stays continuous,
            // with a small travelling wave so it reads as a whip rather than a rigid pole.
            int segmentCount = whipSegments.Count;
            for (int i = 0; i < segmentCount; i++)
            {
                float extendedCenter = WhipBladeStart + (i + 0.5f) / segmentCount * reach;
                float extendedLength = Mathf.Max(WhipSegmentLength, reach / segmentCount);

                float center = Mathf.Lerp(RetractedSegmentCenter(i), extendedCenter, extension);
                float length = Mathf.Lerp(WhipSegmentLength, extendedLength, extension);
                float wave = Mathf.Sin(i * 0.9f - lashProgress * 2f * Mathf.PI) * 0.06f * extension;

                Transform segment = whipSegments[i];
                segment.localPosition = new Vector3(0f, wave, center);
                PrimitiveParts.SetPartSize(segment, PrimitiveShape.Cube,
                    new Vector3(WhipSegmentWidth, WhipSegmentHeight, length));
            }
        }

        private void HandleWeaponFired()
        {
            recoilAlpha = 1f;
        }

        private void HandleAbilityActivated(StraferAbility ability)
        {
            if (ability == StraferAbility.Stun)
            {
                lashElapsed = 0f;
            }
        }

        private static float RetractedSegmentCenter(int index)
        {
            return WhipBladeStart + WhipSegmentLength * 0.5f + index * (WhipSegmentLength + WhipSegmentGap);
        }

        private static Transform AddViewmodelPart(Transform parent, string partName, PrimitiveShape shape,
            Vector3 position, Vector3 euler, Vector3 size, Color color)
        {
            Transform part = PrimitiveParts.AddPart(parent, partName, shape, position, euler, size, color);
            part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return part;
        }
    }
}
