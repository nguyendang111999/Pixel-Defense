using System.Collections.Generic;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Cached cannon-critter meshes per color, designed for a steep top-down camera: the lid stays clear for a
    /// big ammo number, the eyes peek over the front edge and the barrel sticks out of the front face.
    /// Unit space: body ~1 wide, origin at the floor center, facing +Z.
    /// </summary>
    public sealed class CannonMeshes
    {
        public const float LidHeight = 0.7f;
        public static readonly Vector3 LabelCenter = new Vector3(0f, LidHeight + 0.015f, -0.06f);
        public static readonly Vector3 TurretPivot = new Vector3(0f, 0.36f, 0.42f);
        public const float BarrelLength = 0.5f;

        private static readonly Color Metal = new Color(0.27f, 0.29f, 0.38f);
        private static readonly Color Rubber = new Color(0.16f, 0.16f, 0.22f);
        private static readonly Color EyeWhite = new Color(1f, 1f, 0.98f);
        private static readonly Color Pupil = new Color(0.1f, 0.1f, 0.15f);

        private readonly Dictionary<int, Mesh> _bodies = new Dictionary<int, Mesh>();
        private readonly Dictionary<int, Mesh> _barrels = new Dictionary<int, Mesh>();

        public Mesh Body(Color color)
        {
            int key = Key(color);
            if (!_bodies.TryGetValue(key, out Mesh mesh))
            {
                mesh = BuildBody(color);
                _bodies[key] = mesh;
            }
            return mesh;
        }

        public Mesh Barrel(Color color)
        {
            int key = Key(color);
            if (!_barrels.TryGetValue(key, out Mesh mesh))
            {
                mesh = BuildBarrel(color);
                _barrels[key] = mesh;
            }
            return mesh;
        }

        public void Dispose()
        {
            foreach (Mesh mesh in _bodies.Values)
            {
                Object.Destroy(mesh);
            }
            foreach (Mesh mesh in _barrels.Values)
            {
                Object.Destroy(mesh);
            }
            _bodies.Clear();
            _barrels.Clear();
        }

        private static int Key(Color color)
        {
            Color32 c = color;
            return c.r << 16 | c.g << 8 | c.b;
        }

        private static Mesh BuildBody(Color color)
        {
            var builder = new MeshBuilder();
            Color lid = Color.Lerp(color, Color.white, 0.22f);
            Color dark = Color.Lerp(color, Color.black, 0.18f);

            MeshFactory.AddChamferCube(builder, Matrix4x4.TRS(new Vector3(0f, 0.36f, 0f), Quaternion.identity, new Vector3(1f, 0.6f, 0.94f)), 0.24f, color);
            MeshFactory.AddChamferCube(builder, Matrix4x4.TRS(new Vector3(0f, 0.64f, -0.04f), Quaternion.identity, new Vector3(0.84f, 0.13f, 0.76f)), 0.3f, lid);
            MeshFactory.AddChamferCube(builder, Matrix4x4.TRS(new Vector3(0f, 0.12f, 0f), Quaternion.identity, new Vector3(1.04f, 0.16f, 0.98f)), 0.3f, dark);

            for (int side = -1; side <= 1; side += 2)
            {
                Matrix4x4 wheel = Matrix4x4.TRS(new Vector3(side * 0.5f, 0.2f, -0.08f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
                MeshFactory.AddCylinder(builder, wheel * Matrix4x4.Translate(new Vector3(0f, 0f, -0.07f)), 0.2f, 0.14f, 14, Rubber, Metal);

                // Frog-style eyes peeking over the front edge of the lid, visible from above.
                MeshFactory.AddSphere(builder, Matrix4x4.TRS(new Vector3(side * 0.21f, 0.7f, 0.33f), Quaternion.identity, Vector3.one * 0.25f), 12, 8, EyeWhite);
                MeshFactory.AddSphere(builder, Matrix4x4.TRS(new Vector3(side * 0.2f, 0.77f, 0.4f), Quaternion.identity, Vector3.one * 0.12f), 10, 6, Pupil);
            }

            return builder.Build("CannonBody");
        }

        private static Mesh BuildBarrel(Color color)
        {
            var builder = new MeshBuilder();
            Color dark = Color.Lerp(color, Color.black, 0.25f);
            MeshFactory.AddSphere(builder, Matrix4x4.Scale(Vector3.one * 0.34f), 12, 8, dark);
            MeshFactory.AddCylinder(builder, Matrix4x4.identity, 0.14f, BarrelLength, 14, Metal, Metal);
            MeshFactory.AddCylinder(builder, Matrix4x4.Translate(new Vector3(0f, 0f, BarrelLength - 0.13f)), 0.18f, 0.15f, 14, color, Rubber);
            return builder.Build("CannonBarrel");
        }
    }
}
