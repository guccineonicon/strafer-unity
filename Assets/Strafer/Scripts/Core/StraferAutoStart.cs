using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Starts a match in any scene that does not set one up itself, so pressing
    /// Play works with no setup: an empty scene gets a practice arena, the player,
    /// and practice dummies.
    ///
    /// Skipped in scenes that already contain a <see cref="StraferGameMode"/> or a
    /// <see cref="StraferAutoStartOptOut"/>.
    /// </summary>
    public static class StraferAutoStart
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartMatchIfNeeded()
        {
            if (Object.FindAnyObjectByType<StraferGameMode>() != null
                || Object.FindAnyObjectByType<StraferAutoStartOptOut>() != null)
            {
                return;
            }

            new GameObject("StraferGameMode").AddComponent<StraferGameMode>();
        }
    }
}
