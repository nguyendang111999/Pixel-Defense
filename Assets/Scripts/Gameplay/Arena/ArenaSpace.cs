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

        /// <summary>Slot pad size (slice units): as large as configured while the whole row fits on the platform.</summary>
        public float SlotSize(int slotCount)
        {
            float maxRow = Track.BaseRadius * 2f * _config.SlotRowFill;
            float fit = (maxRow - (slotCount - 1) * _config.SlotGap) / slotCount;
            return Mathf.Min(_config.SlotSize, fit);
        }

        /// <summary>Seated cannon size (slice units): more slots make a tighter row, so the cannons on it shrink.</summary>
        public float SeatedCannonSize(int slotCount)
        {
            float pitch = SlotSize(slotCount) + _config.SlotGap;
            return Mathf.Min(_config.SlotCannonSize, pitch * _config.SeatPitchFill);
        }

        public Vector3 SlotPosition(int index, int slotCount, float height)
        {
            float size = SlotSize(slotCount);
            float pitch = size + _config.SlotGap;
            float x = (index - (slotCount - 1) * 0.5f) * pitch;
            return ToWorld(x, 0f, height);
        }

        /// <summary>Z (slice units) of the front row of cannon columns, just below the arena.</summary>
        public float ColumnsFrontY => Track.ArenaMinY - _config.ColumnsGap - _config.CannonSize * 0.5f;

        public Vector3 ColumnPosition(int column, int columnCount, int row)
        {
            float x = (column - (columnCount - 1) * 0.5f) * _config.ColumnSpacing;
            float y = ColumnsFrontY - row * _config.RowSpacing;
            return ToWorld(x, y);
        }
    }
}
