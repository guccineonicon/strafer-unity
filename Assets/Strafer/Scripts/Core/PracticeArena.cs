using Strafer.Visuals;
using UnityEngine;

namespace Strafer.Core
{
    /// <summary>
    /// Builds a simple practice level from basic shapes: a floor, low cover,
    /// tall pillars, and a raised platform with a ramp. Used when a scene has no
    /// ground to stand on, so the game is playable in a brand-new empty scene.
    /// </summary>
    public static class PracticeArena
    {
        private static readonly Color FloorColor = StraferMath.LinearColor(0.18f, 0.19f, 0.21f);
        private static readonly Color CoverColor = StraferMath.LinearColor(0.35f, 0.36f, 0.40f);
        private static readonly Color AccentColor = StraferMath.LinearColor(0.60f, 0.35f, 0.12f);

        /// <summary>Builds the arena centered on <paramref name="center"/>, with its floor surface at that height.</summary>
        public static void Build(Vector3 center)
        {
            Transform root = new GameObject("PracticeArena").transform;
            root.position = center;

            // 60 m square floor whose top surface sits exactly at the center height.
            AddBlock(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f), Vector3.zero, FloorColor);

            // Perimeter walls.
            AddBlock(root, "WallNorth", new Vector3(0f, 2f, 30.5f), new Vector3(62f, 4f, 1f), Vector3.zero, CoverColor);
            AddBlock(root, "WallSouth", new Vector3(0f, 2f, -30.5f), new Vector3(62f, 4f, 1f), Vector3.zero, CoverColor);
            AddBlock(root, "WallEast", new Vector3(30.5f, 2f, 0f), new Vector3(1f, 4f, 60f), Vector3.zero, CoverColor);
            AddBlock(root, "WallWest", new Vector3(-30.5f, 2f, 0f), new Vector3(1f, 4f, 60f), Vector3.zero, CoverColor);

            // Waist-high cover to practice peeking and dash-sliding along walls.
            AddBlock(root, "CoverLeft", new Vector3(-6f, 0.6f, 14f), new Vector3(4f, 1.2f, 1f), Vector3.zero, CoverColor);
            AddBlock(root, "CoverRight", new Vector3(6f, 0.6f, 14f), new Vector3(4f, 1.2f, 1f), Vector3.zero, CoverColor);
            AddBlock(root, "CoverAngled", new Vector3(0f, 0.6f, -10f), new Vector3(5f, 1.2f, 1f), new Vector3(0f, 30f, 0f), CoverColor);

            // Tall pillars that fully block line of sight.
            AddBlock(root, "PillarA", new Vector3(-14f, 2f, 4f), new Vector3(2f, 4f, 2f), Vector3.zero, AccentColor);
            AddBlock(root, "PillarB", new Vector3(14f, 2f, 4f), new Vector3(2f, 4f, 2f), Vector3.zero, AccentColor);
            AddBlock(root, "PillarC", new Vector3(0f, 2f, 22f), new Vector3(2f, 4f, 2f), Vector3.zero, AccentColor);

            // Raised platform with a ramp, for height and jump practice.
            AddBlock(root, "Platform", new Vector3(18f, 1f, -18f), new Vector3(8f, 2f, 8f), Vector3.zero, CoverColor);
            AddBlock(root, "Ramp", new Vector3(18f, 0.95f, -10.5f), new Vector3(4f, 0.3f, 7.5f), new Vector3(16f, 0f, 0f), AccentColor);
        }

        private static void AddBlock(Transform root, string name, Vector3 position, Vector3 size, Vector3 euler, Color color)
        {
            PrimitiveParts.AddPart(root, name, PrimitiveShape.Cube, position, euler, size, color, keepCollider: true);
        }
    }
}
