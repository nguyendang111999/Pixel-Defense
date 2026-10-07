using System.Collections.Generic;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Voxel dragon head in four parts (skull, jaw, eye whites, pupils) so the jaw can open and the eyes blink.
    /// Voxel space: x across (−5..5), y up, z forward; the neck sits at z = 0.
    /// </summary>
    public sealed class DragonHeadModel
    {
        public static readonly Vector3 JawPivot = new Vector3(0f, 1.6f, 1.5f);
        public static readonly Vector3 EyePivotLeft = new Vector3(-2.5f, 8.5f, 5.5f);
        public static readonly Vector3 EyePivotRight = new Vector3(2.5f, 8.5f, 5.5f);
        public static readonly Vector3 MouthPoint = new Vector3(0f, 2.5f, 11.5f);
        public static readonly Vector3 NostrilPoint = new Vector3(0f, 6.5f, 11.5f);

        /// <summary>Length from the neck to the snout tip in voxels.</summary>
        public const float LengthVoxels = 12.5f;

        public readonly VoxelModel Skull = new VoxelModel();
        public readonly VoxelModel Jaw = new VoxelModel();
        public readonly VoxelModel EyeLeft = new VoxelModel();
        public readonly VoxelModel EyeRight = new VoxelModel();

        public DragonHeadModel(DragonSkin skin)
        {
            Color main = skin.Main;
            Color belly = skin.Belly;
            Color horn = skin.Horn;
            Color accent = skin.Accent;
            Color mouth = new Color(0.55f, 0.1f, 0.16f);
            Color dark = new Color(0.13f, 0.12f, 0.18f);
            Color white = new Color(1f, 0.98f, 0.95f);

            var min = new Vector3Int(-6, 2, -1);
            var max = new Vector3Int(6, 9, 12);
            Skull.Ellipsoid(new Vector3(0f, 5.2f, 3.2f), new Vector3(5.6f, 3.4f, 4.6f), main, min, max);
            Skull.Ellipsoid(new Vector3(0f, 4.3f, 8.6f), new Vector3(4.3f, 2.3f, 4.1f), main, min, max);
            Skull.Ellipsoid(new Vector3(0f, 7.6f, 4.8f), new Vector3(4.6f, 1.2f, 1.6f), accent, min, max);

            // Mouth roof, visible when the jaw opens.
            Skull.Paint(p => p.y == 2 && p.z >= 4 && Mathf.Abs(p.x) <= 3, mouth);

            // Nostrils on top of the snout tip.
            for (int side = -1; side <= 1; side += 2)
            {
                int x = side * 2;
                int top = Skull.TopAt(x, 11);
                if (top != int.MinValue)
                {
                    Skull.Set(x, top, 11, dark);
                }
            }

            // Angry brow blocks above the eyes (eyes are separate parts).
            for (int side = -1; side <= 1; side += 2)
            {
                Skull.Set(side * 1, 10, 6, accent);
                Skull.Set(side * 2, 10, 5, accent);
                Skull.Set(side * 3, 10, 5, accent);
                Skull.Set(side * 4, 9, 4, accent);
            }

            // Chunky horns sweeping back and out (two voxels thick so they read from above).
            int[,] hornPath =
            {
                { 4, 8, 2 }, { 5, 8, 2 }, { 4, 9, 1 }, { 5, 9, 1 }, { 4, 9, 2 }, { 5, 10, 0 }, { 6, 10, 0 },
                { 5, 10, -1 }, { 6, 11, -1 }, { 6, 11, -2 }, { 7, 12, -3 }
            };
            for (int i = 0; i < hornPath.GetLength(0); i++)
            {
                Skull.SetMirrored(hornPath[i, 0], hornPath[i, 1], hornPath[i, 2], horn);
            }

            // Crest: a short flame ridge down the middle of the skull.
            int[,] crest = { { 0, 9, 3 }, { 0, 10, 2 }, { 0, 9, 2 }, { 0, 9, 1 }, { 0, 10, 1 }, { 0, 11, 1 }, { 0, 9, 0 }, { 0, 10, 0 }, { 0, 8, -1 }, { 0, 9, -1 } };
            for (int i = 0; i < crest.GetLength(0); i++)
            {
                Skull.SetMirrored(crest[i, 0], crest[i, 1], crest[i, 2], horn);
            }

            // Cheek frills.
            int[,] frill = { { 6, 5, 2 }, { 6, 5, 1 }, { 7, 5, 1 }, { 7, 6, 0 } };
            for (int i = 0; i < frill.GetLength(0); i++)
            {
                Skull.SetMirrored(frill[i, 0], frill[i, 1], frill[i, 2], horn);
            }

            // Upper fangs hanging over the jaw.
            Skull.SetMirrored(3, 1, 10, white);
            Skull.SetMirrored(2, 1, 11, white);

            // Lower jaw with tongue and teeth.
            Jaw.Ellipsoid(new Vector3(0f, 1f, 6.2f), new Vector3(4.4f, 1.8f, 5.6f), belly, new Vector3Int(-5, 0, 1), new Vector3Int(5, 1, 11));
            Jaw.Paint(p => p.y == 1 && Mathf.Abs(p.x) <= 2 && p.z >= 3 && p.z <= 9, mouth);
            Jaw.Paint(p => p.y == 0, accent);
            Jaw.SetMirrored(3, 2, 9, white);
            Jaw.SetMirrored(3, 2, 7, white);
            Jaw.SetMirrored(1, 2, 10, white);

            BuildEye(EyeLeft, -1, white, dark);
            BuildEye(EyeRight, 1, white, dark);
        }

        private static void BuildEye(VoxelModel eye, int side, Color white, Color pupil)
        {
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    for (int dz = 0; dz <= 1; dz++)
                    {
                        eye.Set(side * (2 + dx), 8 + dy, 5 + dz, white);
                    }
                }
            }

            // Pupil on the inner-front-top corner so it reads as a glare toward the base.
            eye.Set(side * 2, 9, 6, pupil);
            eye.Set(side * 2, 8, 6, pupil);
        }

        public void CollectSurface(float voxelSize, List<Vector3> positions, List<Color> colors)
        {
            Skull.CollectSurface(voxelSize, Vector3.zero, positions, colors);
            Jaw.CollectSurface(voxelSize, Vector3.zero, positions, colors);
            EyeLeft.CollectSurface(voxelSize, Vector3.zero, positions, colors);
            EyeRight.CollectSurface(voxelSize, Vector3.zero, positions, colors);
        }
    }
}
