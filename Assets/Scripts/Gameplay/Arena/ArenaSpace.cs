using PixelDefense.Core;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Maps the track's slice-unit plane onto the world floor (x → X, y → Z) and lays out slots and columns.
    /// The base platform is centered at <see cref="Origin"/>.
    /// </summary>
    public sealed class ArenaSpace
    {
        private const float MaxSlotRowWidth = 15.0f;

        private readonly VisualConfig _config;

        public ArenaSpace(TrackShape track, VisualConfig config, Vector3 origin)
        {
            Track = track;
            _config = config;
            Scale = config.WorldPerSlice;
            Origin = origin;
        }

        public TrackShape Track { get; }
        public float Scale { get; }
        public Vector3 Origin { get; }

        public float ArenaRadiusWorld => Track.ArenaRadius * Scale;

        public Vector3 ToWorld(float x, float y, float height = 0f)
        {
            return Origin + new Vector3(x * Scale, height, y * Scale);
        }

        /// <summary>World pose along the track at arc length <paramref name="s"/> (slice units).</summary>
        public void Pose(float s, out Vector3 position, out Vector3 forward, out Vector3 right)
        {
            Track.Sample(s, out float x, out float y, out float tx, out float ty);
            position = ToWorld(x, y);
            forward = new Vector3(tx, 0f, ty);
            right = new Vector3(ty, 0f, -tx);
        }

        public float LaneOffset(int lane, int lanes)
        {
            return (lane - (lanes - 1) * 0.5f) * Scale;
        }

        public float SlotSize(int slotCount)
        {
            float fit = (MaxSlotRowWidth - (slotCount - 1) * _config.SlotGap) / slotCount;
            return Mathf.Min(_config.SlotSize, fit);
        }

        public Vector3 SlotPosition(int index, int slotCount, float height)
        {
            float size = SlotSize(slotCount);
            float pitch = size + _config.SlotGap;
            float x = (index - (slotCount - 1) * 0.5f) * pitch;
            return ToWorld(x, 0f, height);
        }

        /// <summary>Z (slice units) of the front row of cannon columns.</summary>
        public float ColumnsFrontY => -Track.ArenaRadius - _config.ColumnsGap - _config.CannonSize * 0.5f;

        public Vector3 ColumnPosition(int column, int columnCount, int row)
        {
            float x = (column - (columnCount - 1) * 0.5f) * _config.ColumnSpacing;
            float y = ColumnsFrontY - row * _config.RowSpacing;
            return ToWorld(x, y);
        }
    }
}
