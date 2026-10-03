using UnityEngine;

namespace Strafer.Visuals
{
    /// <summary>
    /// Draws short-lived lines and points that are visible in the Game view,
    /// unlike <see cref="Debug.DrawLine(Vector3, Vector3)"/> which only shows in the Scene view.
    /// Used to visualize shots and whip strikes until real effects exist.
    /// </summary>
    public static class StraferDebugDraw
    {
        private static Material lineMaterial;

        /// <summary>Draws a line from <paramref name="start"/> to <paramref name="end"/> for <paramref name="duration"/> seconds.</summary>
        public static void Line(Vector3 start, Vector3 end, Color color, float duration, float width = 0.01f)
        {
            GameObject lineObject = new GameObject("DebugLine");
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = GetLineMaterial();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            Object.Destroy(lineObject, duration);
        }

        /// <summary>Draws a small cube marker at <paramref name="position"/> for <paramref name="duration"/> seconds.</summary>
        public static void Point(Vector3 position, Color color, float duration, float size = 0.08f)
        {
            Transform marker = PrimitiveParts.AddPart(null, "DebugPoint", PrimitiveShape.Cube,
                position, Vector3.zero, Vector3.one * size, color);
            marker.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Object.Destroy(marker.gameObject, duration);
        }

        private static Material GetLineMaterial()
        {
            if (lineMaterial == null)
            {
                // An unlit, vertex-colored shader that exists in both the built-in and URP pipelines.
                Shader shader = Shader.Find("Sprites/Default");
                lineMaterial = new Material(shader);
            }

            return lineMaterial;
        }
    }
}
