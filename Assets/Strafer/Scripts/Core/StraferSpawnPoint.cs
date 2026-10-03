using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Marks where players spawn and respawn. The position is the spot under the
    /// character's feet, and the blue arrow shows the direction they face.
    ///
    /// Practice dummies line up in front of the first spawn point found.
    /// </summary>
    public class StraferSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            // Outline of a character-sized capsule plus a facing arrow.
            Vector3 feet = transform.position;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(feet + Vector3.up * 0.3f, 0.3f);
            Gizmos.DrawWireSphere(feet + Vector3.up * 1.46f, 0.3f);
            Gizmos.DrawLine(feet + Vector3.up * 0.88f, feet + Vector3.up * 0.88f + transform.forward * 0.8f);

            Gizmos.color = Color.blue;
            Vector3 eye = feet + Vector3.up * 1.6f;
            Gizmos.DrawLine(eye, eye + transform.forward * 1.2f);
        }
    }
}
