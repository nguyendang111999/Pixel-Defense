using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>Procedural building blocks for the toy-block look.</summary>
    public static class MeshFactory
    {
        /// <summary>Unit cube (±0.5) with flat-shaded chamfered edges that catch highlights.</summary>
        public static Mesh ChamferCube(float chamfer, Color color)
        {
            var builder = new MeshBuilder();
            AddChamferCube(builder, Matrix4x4.identity, chamfer, color);
            return builder.Build("ChamferCube");
        }

        public static void AddChamferCube(MeshBuilder target, Matrix4x4 transform, float chamfer, Color color)
        {
            var builder = new MeshBuilder();
            float h = 0.5f;
            float k = 0.5f - chamfer;
            var poly = new Vector3[4];

            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    poly[0] = FacePoint(axis, sign * h, -k, -k);
                    poly[1] = FacePoint(axis, sign * h, k, -k);
                    poly[2] = FacePoint(axis, sign * h, k, k);
                    poly[3] = FacePoint(axis, sign * h, -k, k);
                    builder.AddPolygon(poly, 4, Vector3.zero, color);
                }
            }

            for (int a = 0; a < 3; a++)
            {
                for (int b = a + 1; b < 3; b++)
                {
                    int t = 3 - a - b;
                    for (int sa = -1; sa <= 1; sa += 2)
                    {
                        for (int sb = -1; sb <= 1; sb += 2)
                        {
                            poly[0] = Point(a, sa * h, b, sb * k, t, -k);
                            poly[1] = Point(a, sa * h, b, sb * k, t, k);
                            poly[2] = Point(b, sb * h, a, sa * k, t, k);
                            poly[3] = Point(b, sb * h, a, sa * k, t, -k);
                            builder.AddPolygon(poly, 4, Vector3.zero, color);
                        }
                    }
                }
            }

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        poly[0] = new Vector3(sx * h, sy * k, sz * k);
                        poly[1] = new Vector3(sx * k, sy * h, sz * k);
                        poly[2] = new Vector3(sx * k, sy * k, sz * h);
                        builder.AddPolygon(poly, 3, Vector3.zero, color);
                    }
                }
            }

            target.Append(builder, transform);
        }

        private static Vector3 FacePoint(int axis, float d, float u, float v)
        {
            return Point(axis, d, (axis + 1) % 3, u, (axis + 2) % 3, v);
        }

        private static Vector3 Point(int axisA, float a, int axisB, float b, int axisC, float c)
        {
            var p = new Vector3();
            p[axisA] = a;
            p[axisB] = b;
            p[axisC] = c;
            return p;
        }

        public static Mesh Sphere(int longitude, int latitude, Color color)
        {
            var builder = new MeshBuilder();
            AddSphere(builder, Matrix4x4.identity, longitude, latitude, color);
            return builder.Build("Sphere");
        }

        /// <summary>Unit-diameter UV sphere, smooth shaded.</summary>
        public static void AddSphere(MeshBuilder target, Matrix4x4 transform, int longitude, int latitude, Color color)
        {
            var builder = new MeshBuilder();
            for (int lat = 0; lat <= latitude; lat++)
            {
                float theta = Mathf.PI * lat / latitude;
                for (int lon = 0; lon <= longitude; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / longitude;
                    var normal = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    builder.AddVertex(normal * 0.5f, normal, color);
                }
            }

            int ring = longitude + 1;
            for (int lat = 0; lat < latitude; lat++)
            {
                for (int lon = 0; lon < longitude; lon++)
                {
                    int a = lat * ring + lon;
                    int b = a + ring;
                    builder.AddTriangle(a, a + 1, b);
                    builder.AddTriangle(a + 1, b + 1, b);
                }
            }

            target.Append(builder, transform);
        }

        /// <summary>Cylinder along +Z from 0 to length, smooth sides with flat caps.</summary>
        public static void AddCylinder(MeshBuilder target, Matrix4x4 transform, float radius, float length, int segments, Color sideColor, Color capColor)
        {
            var builder = new MeshBuilder();
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                var normal = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                builder.AddVertex(normal * radius, normal, sideColor);
                builder.AddVertex(normal * radius + Vector3.forward * length, normal, sideColor);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                builder.AddTriangle(a, a + 2, a + 1);
                builder.AddTriangle(a + 1, a + 2, a + 3);
            }

            for (int cap = 0; cap < 2; cap++)
            {
                float z = cap == 0 ? 0f : length;
                Vector3 normal = cap == 0 ? Vector3.back : Vector3.forward;
                int center = builder.AddVertex(new Vector3(0f, 0f, z), normal, capColor);
                int first = builder.VertexCount;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = 2f * Mathf.PI * i / segments;
                    builder.AddVertex(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z), normal, capColor);
                }

                for (int i = 0; i < segments; i++)
                {
                    if (cap == 0)
                    {
                        builder.AddTriangle(center, first + i + 1, first + i);
                    }
                    else
                    {
                        builder.AddTriangle(center, first + i, first + i + 1);
                    }
                }
            }

            target.Append(builder, transform);
        }

        /// <summary>
        /// Rounded-rectangle slab resting on y=0 with a beveled top edge. width/depth are full sizes; a radius of
        /// half the size gives a disc.
        /// </summary>
        public static void AddRoundedSlab(MeshBuilder target, Matrix4x4 transform, float width, float depth, float height,
            float radius, float bevel, int cornerSegments, Color topColor, Color sideColor)
        {
            var builder = new MeshBuilder();
            radius = Mathf.Min(radius, Mathf.Min(width, depth) * 0.5f);
            bevel = Mathf.Min(bevel, radius * 0.95f, height * 0.95f);
            int count = cornerSegments * 4 + 4;
            var outline = new Vector2[count];
            var outlineNormal = new Vector2[count];
            float hx = width * 0.5f - radius;
            float hz = depth * 0.5f - radius;
            int n = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                float cx = corner == 0 || corner == 3 ? hx : -hx;
                float cz = corner < 2 ? hz : -hz;
                float start = corner * 90f;
                for (int i = 0; i <= cornerSegments; i++)
                {
                    float angle = (start + 90f * i / cornerSegments) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    outline[n] = new Vector2(cx, cz) + dir * radius;
                    outlineNormal[n] = dir;
                    n++;
                }
            }

            float top = height;
            float bevelBottom = height - bevel;

            // Top face: fan from the center over the inset outline.
            int center = builder.AddVertex(new Vector3(0f, top, 0f), Vector3.up, topColor);
            int topStart = builder.VertexCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = outline[i] - outlineNormal[i] * bevel;
                builder.AddVertex(new Vector3(p.x, top, p.y), Vector3.up, topColor);
            }
            for (int i = 0; i < count; i++)
            {
                builder.AddTriangle(center, topStart + (i + 1) % count, topStart + i);
            }

            // Bevel ring (45°) and vertical side.
            int bevelStart = builder.VertexCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 o = outline[i];
                Vector2 inner = o - outlineNormal[i] * bevel;
                Vector3 normal = new Vector3(outlineNormal[i].x, 1f, outlineNormal[i].y).normalized;
                builder.AddVertex(new Vector3(inner.x, top, inner.y), normal, topColor);
                builder.AddVertex(new Vector3(o.x, bevelBottom, o.y), normal, sideColor);
            }
            for (int i = 0; i < count; i++)
            {
                int a = bevelStart + i * 2;
                int b = bevelStart + ((i + 1) % count) * 2;
                builder.AddTriangle(a, b, a + 1);
                builder.AddTriangle(a + 1, b, b + 1);
            }

            int sideStart = builder.VertexCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 o = outline[i];
                var normal = new Vector3(outlineNormal[i].x, 0f, outlineNormal[i].y);
                builder.AddVertex(new Vector3(o.x, bevelBottom, o.y), normal, sideColor);
                builder.AddVertex(new Vector3(o.x, 0f, o.y), normal, sideColor);
            }
            for (int i = 0; i < count; i++)
            {
                int a = sideStart + i * 2;
                int b = sideStart + ((i + 1) % count) * 2;
                builder.AddTriangle(a, b, a + 1);
                builder.AddTriangle(a + 1, b, b + 1);
            }

            target.Append(builder, transform);
        }

        /// <summary>
        /// Pipe-like wall along an XZ polyline: vertical sides with a rounded top and rounded end caps.
        /// </summary>
        public static void AddWall(MeshBuilder target, Vector3[] path, int pathCount, float width, float height, int arcSegments, Color color)
        {
            if (pathCount < 2)
            {
                return;
            }

            float radius = width * 0.5f;
            float shoulder = Mathf.Max(0f, height - radius);
            int profileCount = arcSegments + 3;
            var profile = new Vector2[profileCount];
            var profileNormal = new Vector2[profileCount];
            profile[0] = new Vector2(-radius, 0f);
            profileNormal[0] = new Vector2(-1f, 0f);
            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = Mathf.PI - Mathf.PI * i / arcSegments;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                profile[i + 1] = new Vector2(0f, shoulder) + dir * radius;
                profileNormal[i + 1] = dir;
            }
            profile[profileCount - 1] = new Vector2(radius, 0f);
            profileNormal[profileCount - 1] = new Vector2(1f, 0f);

            var builder = new MeshBuilder();
            for (int p = 0; p < pathCount; p++)
            {
                Vector3 tangent = path[Mathf.Min(p + 1, pathCount - 1)] - path[Mathf.Max(p - 1, 0)];
                tangent.y = 0f;
                tangent.Normalize();
                Vector3 side = new Vector3(tangent.z, 0f, -tangent.x);
                for (int i = 0; i < profileCount; i++)
                {
                    Vector3 position = path[p] + side * profile[i].x + Vector3.up * profile[i].y;
                    Vector3 normal = (side * profileNormal[i].x + Vector3.up * profileNormal[i].y).normalized;
                    builder.AddVertex(position, normal, color);
                }
            }

            for (int p = 0; p < pathCount - 1; p++)
            {
                int a = p * profileCount;
                int b = a + profileCount;
                for (int i = 0; i < profileCount - 1; i++)
                {
                    builder.AddTriangle(a + i, b + i, a + i + 1);
                    builder.AddTriangle(a + i + 1, b + i, b + i + 1);
                }
            }

            AddWallCap(builder, path[0], (path[0] - path[1]).normalized, radius, shoulder, arcSegments, color);
            AddWallCap(builder, path[pathCount - 1], (path[pathCount - 1] - path[pathCount - 2]).normalized, radius, shoulder, arcSegments, color);
            target.Append(builder, Matrix4x4.identity);
        }

        /// <summary>Half-cylinder plus quarter-sphere closing a wall end, facing <paramref name="outward"/>.</summary>
        private static void AddWallCap(MeshBuilder builder, Vector3 end, Vector3 outward, float radius, float shoulder, int segments, Color color)
        {
            outward.y = 0f;
            outward.Normalize();
            Vector3 side = new Vector3(outward.z, 0f, -outward.x);
            int rings = segments;
            int around = segments * 2;
            int start = builder.VertexCount;
            for (int r = 0; r <= rings + 1; r++)
            {
                // r = 0: floor ring, r = 1..rings+1: elevation from the shoulder (0°) to the top (90°).
                float elevation = r == 0 ? 0f : (r - 1) * 0.5f * Mathf.PI / rings;
                float ringRadius = r == 0 ? radius : Mathf.Cos(elevation) * radius;
                float y = r == 0 ? 0f : shoulder + Mathf.Sin(elevation) * radius;
                for (int a = 0; a <= around; a++)
                {
                    float angle = Mathf.PI * a / around - Mathf.PI * 0.5f;
                    Vector3 dir = outward * Mathf.Cos(angle) + side * Mathf.Sin(angle);
                    Vector3 normal = r == 0 ? dir : (dir * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation)).normalized;
                    builder.AddVertex(end + dir * ringRadius + Vector3.up * y, normal, color);
                }
            }

            int stride = around + 1;
            for (int r = 0; r <= rings; r++)
            {
                for (int a = 0; a < around; a++)
                {
                    int i0 = start + r * stride + a;
                    int i1 = i0 + stride;
                    builder.AddTriangle(i0, i0 + 1, i1);
                    builder.AddTriangle(i0 + 1, i1 + 1, i1);
                }
            }
        }

        /// <summary>Flat XZ ring segment from angle 0 to <paramref name="fraction"/> of a turn; uv.y runs across.</summary>
        public static void BuildRing(MeshBuilder builder, float innerRadius, float outerRadius, float fraction, int segments, Color color)
        {
            builder.Clear();
            int steps = Mathf.Max(1, Mathf.CeilToInt(segments * Mathf.Clamp01(fraction)));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float angle = Mathf.PI * 0.5f - 2f * Mathf.PI * fraction * t;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                builder.AddVertex(dir * innerRadius, Vector3.up, color, new Vector2(t, 0f));
                builder.AddVertex(dir * outerRadius, Vector3.up, color, new Vector2(t, 1f));
            }

            for (int i = 0; i < steps; i++)
            {
                int a = i * 2;
                builder.AddTriangle(a, a + 1, a + 2);
                builder.AddTriangle(a + 1, a + 3, a + 2);
            }
        }

        /// <summary>Flat XZ quad centered at the origin, facing up.</summary>
        public static Mesh GroundQuad(float size)
        {
            var builder = new MeshBuilder();
            float h = size * 0.5f;
            builder.AddQuad(new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h), new Vector3(h, 0f, h), new Vector3(-h, 0f, h), Vector3.up, Color.white);
            return builder.Build("GroundQuad");
        }
    }
}
