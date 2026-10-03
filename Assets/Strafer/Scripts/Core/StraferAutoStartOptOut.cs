using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Add this to any GameObject in a scene to stop Strafer from starting a match
    /// automatically in that scene. Use it for menus or scenes that set up their
    /// own <see cref="StraferGameMode"/>.
    /// </summary>
    public class StraferAutoStartOptOut : MonoBehaviour
    {
    }
}
