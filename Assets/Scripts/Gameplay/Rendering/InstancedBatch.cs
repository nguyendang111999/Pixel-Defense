using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Collects instances for one mesh/material each frame and draws them with Graphics.RenderMeshInstanced,
    /// passing per-instance color and effect vectors to PixelDefense/ToyInstanced. Chunk arrays are reused so
    /// steady-state frames allocate nothing.
    /// </summary>
    public sealed class InstancedBatch
    {
        private const int ChunkSize = 500;
        private static readonly int ColorId = Shader.PropertyToID("_InstanceColor");
        private static readonly int FxId = Shader.PropertyToID("_InstanceFx");

        private readonly Mesh _mesh;
        private readonly List<Chunk> _chunks = new List<Chunk>();
        private RenderParams _params;
        private int _count;

        public InstancedBatch(Mesh mesh, Material material, bool castShadows, Bounds worldBounds)
        {
            _mesh = mesh;
            _params = new RenderParams(material)
            {
                shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = true,
                worldBounds = worldBounds,
                layer = 0
            };
        }

        public int Count => _count;

        public void Clear()
        {
            _count = 0;
        }

        /// <param name="linearColor">Linear-space color (convert palette colors with Color.linear).</param>
        /// <param name="fx">x: flash to white, y: emission, z: darken.</param>
        public void Add(in Matrix4x4 matrix, Vector4 linearColor, Vector4 fx)
        {
            int chunkIndex = _count / ChunkSize;
            if (chunkIndex >= _chunks.Count)
            {
                _chunks.Add(new Chunk());
            }

            Chunk chunk = _chunks[chunkIndex];
            int i = _count % ChunkSize;
            chunk.Matrices[i] = matrix;
            chunk.Colors[i] = linearColor;
            chunk.Fx[i] = fx;
            _count++;
        }

        public void Render()
        {
            int remaining = _count;
            for (int c = 0; remaining > 0; c++)
            {
                Chunk chunk = _chunks[c];
                int n = remaining < ChunkSize ? remaining : ChunkSize;
                chunk.Block.SetVectorArray(ColorId, chunk.Colors);
                chunk.Block.SetVectorArray(FxId, chunk.Fx);
                _params.matProps = chunk.Block;
                Graphics.RenderMeshInstanced(_params, _mesh, 0, chunk.Matrices, n);
                remaining -= n;
            }
        }

        private sealed class Chunk
        {
            public readonly Matrix4x4[] Matrices = new Matrix4x4[ChunkSize];
            public readonly Vector4[] Colors = new Vector4[ChunkSize];
            public readonly Vector4[] Fx = new Vector4[ChunkSize];
            public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        }
    }
}
