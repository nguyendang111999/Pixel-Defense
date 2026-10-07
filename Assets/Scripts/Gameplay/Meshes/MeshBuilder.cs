using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelDefense.Gameplay
{
    /// <summary>Accumulates flat- or smooth-shaded geometry with vertex colors (stored linear) and builds a Mesh.</summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> _positions = new List<Vector3>(1024);
        private readonly List<Vector3> _normals = new List<Vector3>(1024);
        private readonly List<Color> _colors = new List<Color>(1024);
        private readonly List<Vector2> _uvs = new List<Vector2>(1024);
        private readonly List<int> _indices = new List<int>(2048);

        public int VertexCount => _positions.Count;

        public void Clear()
        {
            _positions.Clear();
            _normals.Clear();
            _colors.Clear();
            _uvs.Clear();
            _indices.Clear();
        }

        /// <param name="color">sRGB color; converted to linear because vertex colors bypass color-space conversion.</param>
        public int AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 uv = default)
        {
            _positions.Add(position);
            _normals.Add(normal);
            _colors.Add(color.linear);
            _uvs.Add(uv);
            return _positions.Count - 1;
        }

        public void AddTriangle(int a, int b, int c)
        {
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
        }

        /// <summary>Adds a flat convex polygon, flipping winding so its normal points away from <paramref name="inside"/>.</summary>
        public void AddPolygon(Vector3[] points, int count, Vector3 inside, Color color)
        {
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                centroid += points[i];
            }
            centroid /= count;

            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            if (normal.sqrMagnitude < 1e-12f && count > 3)
            {
                normal = Vector3.Cross(points[2] - points[0], points[3] - points[0]);
            }
            normal.Normalize();
            bool flip = Vector3.Dot(normal, centroid - inside) < 0f;
            if (flip)
            {
                normal = -normal;
            }

            int start = _positions.Count;
            for (int i = 0; i < count; i++)
            {
                AddVertex(points[i], normal, color);
            }

            for (int i = 1; i < count - 1; i++)
            {
                if (flip)
                {
                    AddTriangle(start, start + i + 1, start + i);
                }
                else
                {
                    AddTriangle(start, start + i, start + i + 1);
                }
            }
        }

        /// <summary>Quad with explicit counter-clockwise (seen from the front) corner order.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
        {
            int i = AddVertex(a, normal, color, new Vector2(0f, 0f));
            AddVertex(b, normal, color, new Vector2(1f, 0f));
            AddVertex(c, normal, color, new Vector2(1f, 1f));
            AddVertex(d, normal, color, new Vector2(0f, 1f));
            AddTriangle(i, i + 2, i + 1);
            AddTriangle(i, i + 3, i + 2);
        }

        public Mesh Build(string name, Mesh reuse = null)
        {
            Mesh mesh = reuse != null ? reuse : new Mesh();
            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = _positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_indices, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Append(MeshBuilder other, Matrix4x4 transform)
        {
            int offset = _positions.Count;
            for (int i = 0; i < other._positions.Count; i++)
            {
                _positions.Add(transform.MultiplyPoint3x4(other._positions[i]));
                _normals.Add(transform.MultiplyVector(other._normals[i]).normalized);
                _colors.Add(other._colors[i]);
                _uvs.Add(other._uvs[i]);
            }

            for (int i = 0; i < other._indices.Count; i++)
            {
                _indices.Add(other._indices[i] + offset);
            }
        }
    }
}
