using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Frame-rate independent smoothing helpers that match the feel of the
    /// original Unreal implementation.
    /// </summary>
    public static class StraferMath
    {
        /// <summary>
        /// Moves <paramref name="current"/> toward <paramref name="target"/> by a
        /// fraction of the remaining distance each frame. Higher speeds close the gap
        /// faster. Equivalent to Unreal's FMath::FInterpTo.
        /// </summary>
        public static float InterpTo(float current, float target, float deltaTime, float speed)
        {
            if (speed <= 0f)
            {
                return target;
            }

            float distance = target - current;
            if (distance * distance < 1e-8f)
            {
                return target;
            }

            return current + distance * Mathf.Clamp01(deltaTime * speed);
        }

        /// <summary>
        /// Converts a color authored in linear space (as Unreal's FLinearColor is)
        /// to the sRGB value Unity materials expect, so tints look the same as before.
        /// </summary>
        public static Color LinearColor(float r, float g, float b, float a = 1f)
        {
            return new Color(LinearToSrgb(r), LinearToSrgb(g), LinearToSrgb(b), a);
        }

        // Standard sRGB transfer function. Written in plain C# rather than using
        // Color.gamma so it is safe to call from serialized field initializers.
        private static float LinearToSrgb(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            if (value <= 0.0031308f)
            {
                return value * 12.92f;
            }

            return 1.055f * (float)System.Math.Pow(value, 1.0 / 2.4) - 0.055f;
        }
    }
}
