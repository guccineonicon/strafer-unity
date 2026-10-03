using UnityEngine;

namespace Strafer.Settings
{
    /// <summary>
    /// Per-player game settings such as mouse sensitivity, saved to PlayerPrefs
    /// so they persist between sessions.
    ///
    /// Key bindings are stored separately by <see cref="Strafer.Controls.StraferControls"/>.
    /// </summary>
    public static class StraferSettings
    {
        /// <summary>
        /// Mouse sensitivity uses the same scale as Overwatch: each mouse count turns
        /// the view by this many degrees per point of sensitivity. A value that feels
        /// right in Overwatch therefore feels identical here at the same DPI.
        /// </summary>
        public const float DegreesPerCountAtSensitivityOne = 0.0066f;

        public const float MinMouseSensitivity = 0.01f;
        public const float MaxMouseSensitivity = 20f;
        public const float DefaultMouseSensitivity = 5f;

        // Versioned key so values saved under a different sensitivity scale are not misread.
        private const string MouseSensitivityKey = "Strafer.MouseSensitivityOverwatchScale";

        private static float mouseSensitivity = DefaultMouseSensitivity;
        private static bool loaded;

        /// <summary>Mouse sensitivity on the Overwatch scale (see <see cref="DegreesPerCountAtSensitivityOne"/>).</summary>
        public static float MouseSensitivity
        {
            get
            {
                EnsureLoaded();
                return mouseSensitivity;
            }
            set
            {
                EnsureLoaded();
                mouseSensitivity = Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
            }
        }

        /// <summary>View rotation in degrees for each raw mouse count at the current sensitivity.</summary>
        public static float DegreesPerCount
        {
            get { return DegreesPerCountAtSensitivityOne * MouseSensitivity; }
        }

        /// <summary>Writes all settings to disk.</summary>
        public static void Save()
        {
            EnsureLoaded();
            PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
            PlayerPrefs.Save();
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            mouseSensitivity = Mathf.Clamp(
                PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity),
                MinMouseSensitivity, MaxMouseSensitivity);
        }

        // Static state survives between Play sessions when domain reload is disabled,
        // so reload from disk at the start of each session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            loaded = false;
        }
    }
}
