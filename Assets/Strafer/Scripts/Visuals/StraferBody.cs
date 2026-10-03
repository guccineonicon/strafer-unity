using Strafer.Characters;
using Strafer.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Strafer.Visuals
{
    /// <summary>
    /// Placeholder humanoid figure built from basic shapes: head, torso, hips,
    /// arms, and legs. Sized to fill the character capsule (about 1.76 m tall).
    ///
    /// The local player sees through the character's eyes, so their own body
    /// renders only its shadow. Built at startup, so it appears only in Play mode.
    /// </summary>
    public class StraferBody : MonoBehaviour
    {
        [SerializeField]
        private Color clothingColor = StraferMath.LinearColor(0.55f, 0.12f, 0.10f);

        [SerializeField]
        private Color skinColor = StraferMath.LinearColor(0.80f, 0.62f, 0.50f);

        [SerializeField]
        private Color trousersColor = StraferMath.LinearColor(0.12f, 0.12f, 0.14f);

        private Transform root;

        private void Start()
        {
            StraferCharacter character = GetComponent<StraferCharacter>();

            root = new GameObject("Body").transform;
            root.SetParent(transform, false);

            // Positions are measured from the feet.
            AddPart("Head", PrimitiveShape.Sphere, new Vector3(0f, 1.65f, 0f), new Vector3(0.22f, 0.22f, 0.22f), skinColor);
            AddPart("Torso", PrimitiveShape.Cube, new Vector3(0f, 1.24f, 0f), new Vector3(0.36f, 0.55f, 0.20f), clothingColor);
            AddPart("Hips", PrimitiveShape.Cube, new Vector3(0f, 0.91f, 0f), new Vector3(0.32f, 0.12f, 0.18f), trousersColor);
            AddPart("LeftArm", PrimitiveShape.Cylinder, new Vector3(-0.23f, 1.20f, 0f), new Vector3(0.09f, 0.60f, 0.09f), clothingColor);
            AddPart("RightArm", PrimitiveShape.Cylinder, new Vector3(0.23f, 1.20f, 0f), new Vector3(0.09f, 0.60f, 0.09f), clothingColor);
            AddPart("LeftLeg", PrimitiveShape.Cylinder, new Vector3(-0.09f, 0.425f, 0f), new Vector3(0.13f, 0.85f, 0.13f), trousersColor);
            AddPart("RightLeg", PrimitiveShape.Cylinder, new Vector3(0.09f, 0.425f, 0f), new Vector3(0.13f, 0.85f, 0.13f), trousersColor);

            if (character != null && character.PlayerControlled)
            {
                foreach (Renderer partRenderer in root.GetComponentsInChildren<Renderer>())
                {
                    partRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        /// <summary>Shows or hides the whole body. Used when the character dies and respawns.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.gameObject.SetActive(visible);
            }
        }

        private void AddPart(string partName, PrimitiveShape shape, Vector3 position, Vector3 size, Color color)
        {
            PrimitiveParts.AddPart(root, partName, shape, position, Vector3.zero, size, color);
        }
    }
}
