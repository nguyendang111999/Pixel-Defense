using System.Collections.Generic;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>Sparse colored voxel grid, meshed with hidden-face removal. Also exposes voxels for shatter effects.</summary>
    public sealed class VoxelModel
    {
        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        private readonly Dictionary<Vector3Int, Color> _voxels = new Dictionary<Vector3Int, Color>();

        public int Count => _voxels.Count;
        public IEnumerable<KeyValuePair<Vector3Int, Color>> Voxels => _voxels;

        public void Set(int x, int y, int z, Color color)
        {
            _voxels[new Vector3Int(x, y, z)] = color;
        }

        public void SetMirrored(int x, int y, int z, Color color)
        {
            Set(x, y, z, color);
            Set(-x, y, z, color);
        }

        public bool Has(int x, int y, int z)
        {
            return _voxels.ContainsKey(new Vector3Int(x, y, z));
        }

        public void Remove(int x, int y, int z)
        {
            _voxels.Remove(new Vector3Int(x, y, z));
        }

        /// <summary>Fills voxels whose centers lie inside the ellipsoid, clipped to the given box.</summary>
        public void Ellipsoid(Vector3 center, Vector3 radii, Color color, Vector3Int min, Vector3Int max)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        float dx = (x - center.x) / radii.x;
                        float dy = (y - center.y) / radii.y;
                        float dz = (z - center.z) / radii.z;
                        if (dx * dx + dy * dy + dz * dz <= 1f)
                        {
                            Set(x, y, z, color);
                        }
                    }
                }
            }
        }

        /// <summary>Recolors existing voxels matching a predicate on their coordinates.</summary>
        public void Paint(System.Func<Vector3Int, bool> where, Color color)
        {
            var keys = new List<Vector3Int>(_voxels.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (where(keys[i]))
                {
                    _voxels[keys[i]] = color;
                }
            }
        }

        /// <summary>Highest filled y in a column, or int.MinValue.</summary>
        public int TopAt(int x, int z, int fromY = 40)
        {
            for (int y = fromY; y >= -40; y--)
            {
                if (Has(x, y, z))
                {
                    return y;
                }
            }
            return int.MinValue;
        }

        /// <param name="pivot">Voxel-space point that maps to the mesh origin.</param>
        public Mesh BuildMesh(float voxelSize, Vector3 pivot, string name)
        {
            var builder = new MeshBuilder();
            foreach (KeyValuePair<Vector3Int, Color> voxel in _voxels)
            {
                Vector3Int p = voxel.Key;
                Vector3 center = (new Vector3(p.x, p.y, p.z) - pivot) * voxelSize;
                float h = voxelSize * 0.5f;
                for (int d = 0; d < Directions.Length; d++)
                {
                    Vector3Int dir = Directions[d];
                    if (_voxels.ContainsKey(p + dir))
                    {
                        continue;
                    }

                    Vector3 normal = dir;
                    Vector3 tangent = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                    Vector3 bitangent = Vector3.Cross(normal, tangent);
                    Vector3 c = center + normal * h;
                    Vector3 a = c - tangent * h - bitangent * h;
                    Vector3 b = c + tangent * h - bitangent * h;
                    Vector3 e = c + tangent * h + bitangent * h;
                    Vector3 f = c - tangent * h + bitangent * h;
                    AddFacingQuad(builder, a, b, e, f, normal, voxel.Value);
                }
            }
            return builder.Build(name);
        }

        private static void AddFacingQuad(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
        {
            // Pick the winding whose geometric normal agrees with the face normal.
            Vector3 geometric = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(geometric, normal) > 0f)
            {
                int i = builder.AddVertex(a, normal, color);
                builder.AddVertex(b, normal, color);
                builder.AddVertex(c, normal, color);
                builder.AddVertex(d, normal, color);
                builder.AddTriangle(i, i + 1, i + 2);
                builder.AddTriangle(i, i + 2, i + 3);
            }
            else
            {
                int i = builder.AddVertex(a, normal, color);
                builder.AddVertex(b, normal, color);
                builder.AddVertex(c, normal, color);
                builder.AddVertex(d, normal, color);
                builder.AddTriangle(i, i + 2, i + 1);
                builder.AddTriangle(i, i + 3, i + 2);
            }
        }

        /// <summary>Surface voxels only (at least one open face) as local positions and colors, for shattering.</summary>
        public void CollectSurface(float voxelSize, Vector3 pivot, List<Vector3> positions, List<Color> colors)
        {
            foreach (KeyValuePair<Vector3Int, Color> voxel in _voxels)
            {
                Vector3Int p = voxel.Key;
                bool open = false;
                for (int d = 0; d < Directions.Length && !open; d++)
                {
                    open = !_voxels.ContainsKey(p + Directions[d]);
                }

                if (open)
                {
                    positions.Add((new Vector3(p.x, p.y, p.z) - pivot) * voxelSize);
                    colors.Add(voxel.Value);
                }
            }
        }
    }
}
