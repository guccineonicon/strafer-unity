using System;
using Strafer.Characters;
using UnityEngine;

namespace Strafer.Visuals
{
    /// <summary>
    /// Camera motion blur that switches on only while a dash or roll is active.
    ///
    /// The blur itself depends on the render pipeline, so this component delegates
    /// to an <see cref="ISpeedBlur"/> created by <see cref="BlurFactory"/>. The
    /// Strafer.URP assembly registers a factory when the Universal Render Pipeline
    /// package is installed. In other pipelines the effect does nothing.
    /// </summary>
    public class StraferSpeedEffect : MonoBehaviour
    {
        /// <summary>A pipeline-specific motion blur that can be switched on and off.</summary>
        public interface ISpeedBlur
        {
            void SetActive(bool active, float intensity, float maxBlur);
        }

        /// <summary>Creates the blur for a camera. Set by a render-pipeline integration; null when none is available.</summary>
        public static Func<Camera, ISpeedBlur> BlurFactory;

        [SerializeField, Range(0f, 1f), Tooltip("Motion blur strength while a dash or roll is active.")]
        private float blurIntensity = 1f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Maximum blur length as a fraction of screen size while a dash or roll is active.")]
        private float maxBlur = 0.1f;

        private ISpeedBlur blur;

        private void Start()
        {
            StraferCharacter character = GetComponent<StraferCharacter>();
            if (character != null && character.ViewCamera != null && BlurFactory != null)
            {
                blur = BlurFactory(character.ViewCamera);
            }
        }

        /// <summary>Enables or disables the speed blur.</summary>
        public void SetActive(bool active)
        {
            if (blur != null)
            {
                blur.SetActive(active, blurIntensity, maxBlur);
            }
        }
    }
}
