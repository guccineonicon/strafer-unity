using Strafer.Visuals;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Strafer.URP
{
    /// <summary>
    /// Universal Render Pipeline implementation of the dash and roll speed blur.
    ///
    /// Creates a global post-processing volume holding a Motion Blur override
    /// that stays at zero intensity until a movement burst starts. This assembly
    /// only compiles when the URP package is installed.
    /// </summary>
    public sealed class UrpSpeedBlur : StraferSpeedEffect.ISpeedBlur
    {
        private readonly MotionBlur motionBlur;

        private UrpSpeedBlur(Camera camera)
        {
            // Post-processing is off on new cameras by default in URP.
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            GameObject volumeObject = new GameObject("SpeedBlurVolume");
            volumeObject.transform.SetParent(camera.transform, false);

            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.intensity.Override(0f);
            motionBlur.clamp.Override(0f);
            volume.profile = profile;
        }

        public void SetActive(bool active, float intensity, float maxBlur)
        {
            motionBlur.intensity.Override(active ? intensity : 0f);
            motionBlur.clamp.Override(active ? maxBlur : 0f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            StraferSpeedEffect.BlurFactory = camera => new UrpSpeedBlur(camera);
        }
    }
}
