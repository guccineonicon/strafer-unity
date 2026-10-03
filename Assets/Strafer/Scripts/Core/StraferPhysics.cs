using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Physics queries for weapon traces that skip the shooter's own colliders.
    ///
    /// Unity's single-hit queries cannot ignore a specific object, so these
    /// helpers gather every hit along the trace and return the nearest one that
    /// does not belong to <c>ignoreRoot</c>.
    /// </summary>
    public static class StraferPhysics
    {
        /// <summary>Layers that weapon traces can hit. Mirrors the "Weapon" trace channel in the Unreal version.</summary>
        public static int WeaponTraceMask = Physics.DefaultRaycastLayers;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[64];

        /// <summary>Line trace that ignores <paramref name="ignoreRoot"/> and its children.</summary>
        public static bool Raycast(Vector3 origin, Vector3 direction, float distance, Transform ignoreRoot, out RaycastHit hit)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, HitBuffer, distance,
                WeaponTraceMask, QueryTriggerInteraction.Ignore);
            return FindNearest(count, ignoreRoot, out hit);
        }

        /// <summary>Sphere sweep that ignores <paramref name="ignoreRoot"/> and its children.</summary>
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float distance, Transform ignoreRoot, out RaycastHit hit)
        {
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, HitBuffer, distance,
                WeaponTraceMask, QueryTriggerInteraction.Ignore);
            return FindNearest(count, ignoreRoot, out hit);
        }

        private static bool FindNearest(int count, Transform ignoreRoot, out RaycastHit nearest)
        {
            nearest = default(RaycastHit);
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = HitBuffer[i];
                if (ignoreRoot != null && candidate.collider.transform.IsChildOf(ignoreRoot))
                {
                    continue;
                }

                if (!found || candidate.distance < nearest.distance)
                {
                    nearest = candidate;
                    found = true;
                }
            }

            return found;
        }
    }
}
