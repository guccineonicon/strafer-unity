using UnityEngine;

namespace Strafer.Visuals
{
    /// <summary>Basic shapes used to assemble placeholder art.</summary>
    public enum PrimitiveShape
    {
        Cube,
        Cylinder,
        Sphere
    }

    /// <summary>
    /// Helpers for assembling placeholder visuals from Unity's built-in primitives.
    /// Used until real character and weapon art exists.
    ///
    /// Sizes are final dimensions in meters (x = width, y = height, z = depth).
    /// Cylinders run along their local Y axis: x and z are the diameter, y is the length.
    /// </summary>
    public static class PrimitiveParts
    {
        /// <summary>
        /// Creates a primitive attached to <paramref name="parent"/> and tinted <paramref name="color"/>.
        /// </summary>
        /// <param name="keepCollider">Keep the primitive's collider. Off for visuals so they never block traces.</param>
        public static Transform AddPart(Transform parent, string name, PrimitiveShape shape,
            Vector3 localPosition, Vector3 localEuler, Vector3 size, Color color, bool keepCollider = false)
        {
            GameObject part = GameObject.CreatePrimitive(ToPrimitiveType(shape));
            part.name = name;

            if (!keepCollider)
            {
                Collider partCollider = part.GetComponent<Collider>();
                if (partCollider != null)
                {
                    // DestroyImmediate so the collider is gone before any trace runs this frame.
                    Object.DestroyImmediate(partCollider);
                }
            }

            Transform partTransform = part.transform;
            partTransform.SetParent(parent, false);
            partTransform.localPosition = localPosition;
            partTransform.localRotation = Quaternion.Euler(localEuler);
            SetPartSize(partTransform, shape, size);

            // Accessing .material creates a per-part copy of the active render pipeline's default material.
            part.GetComponent<Renderer>().material.color = color;
            return partTransform;
        }

        /// <summary>Resizes a part using the same size convention as <see cref="AddPart"/>.</summary>
        public static void SetPartSize(Transform part, PrimitiveShape shape, Vector3 size)
        {
            // Unity's cylinder is 1 m across and 2 m tall, so its height scale is halved.
            part.localScale = shape == PrimitiveShape.Cylinder
                ? new Vector3(size.x, size.y * 0.5f, size.z)
                : size;
        }

        private static PrimitiveType ToPrimitiveType(PrimitiveShape shape)
        {
            switch (shape)
            {
                case PrimitiveShape.Cylinder: return PrimitiveType.Cylinder;
                case PrimitiveShape.Sphere: return PrimitiveType.Sphere;
                default: return PrimitiveType.Cube;
            }
        }
    }
}
