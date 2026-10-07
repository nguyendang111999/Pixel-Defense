using System.Collections.Generic;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Frames a set of world points inside a viewport rect (leaving room for HUD and safe areas) at a fixed tilt,
    /// then layers trauma-based shake, short FOV kicks and an optional cinematic push-in on top.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private float _pitch = 64f;
        [SerializeField] private float _fieldOfView = 30f;
        [SerializeField] private float _maxShakeOffset = 0.35f;
        [SerializeField] private float _maxShakeAngle = 1.6f;
        [SerializeField] private float _traumaDecay = 1.4f;

        private Camera _camera;
        private Quaternion _rotation;
        private Vector3 _pivot;
        private float _distance;
        private float _trauma;
        private float _fovKick;
        private float _fovVelocity;
        private float _zoom = 1f;
        private float _zoomTarget = 1f;
        private Vector3 _focus;
        private Vector3 _focusTarget;
        private bool _framed;

        public Camera Camera => _camera != null ? _camera : (_camera = GetComponent<Camera>());

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        /// <param name="viewport">Rect in viewport space (0..1) that the points must fit inside.</param>
        public void Frame(IReadOnlyList<Vector3> points, Rect viewport)
        {
            Camera camera = Camera;
            camera.fieldOfView = _fieldOfView;
            _rotation = Quaternion.Euler(_pitch, 0f, 0f);
            Vector3 forward = _rotation * Vector3.forward;

            Vector3 target = Vector3.zero;
            for (int i = 0; i < points.Count; i++)
            {
                target += points[i];
            }
            target /= Mathf.Max(1, points.Count);
            target.y = 0f;

            float distance = 30f;
            for (int iteration = 0; iteration < 24; iteration++)
            {
                camera.transform.SetPositionAndRotation(target - forward * distance, _rotation);
                ViewportBounds(camera, points, out Vector2 min, out Vector2 max);
                float scale = Mathf.Max((max.x - min.x) / viewport.width, (max.y - min.y) / viewport.height);
                distance *= Mathf.Lerp(1f, scale, 0.85f);

                camera.transform.SetPositionAndRotation(target - forward * distance, _rotation);
                ViewportBounds(camera, points, out min, out max);
                Vector2 offset = (min + max) * 0.5f - viewport.center;

                // Viewport response to moving the target one unit along world X and Z, solved as a 2x2 system.
                Vector2 origin = camera.WorldToViewportPoint(target);
                Vector2 dx = (Vector2)camera.WorldToViewportPoint(target + Vector3.right) - origin;
                Vector2 dz = (Vector2)camera.WorldToViewportPoint(target + Vector3.forward) - origin;
                float det = dx.x * dz.y - dz.x * dx.y;
                if (Mathf.Abs(det) > 1e-6f)
                {
                    float mx = (offset.x * dz.y - dz.x * offset.y) / det;
                    float mz = (dx.x * offset.y - offset.x * dx.y) / det;
                    target += new Vector3(mx, 0f, mz);
                }
            }

            _pivot = target;
            _distance = distance;
            _framed = true;
            Apply();
        }

        private static void ViewportBounds(Camera camera, IReadOnlyList<Vector3> points, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 v = camera.WorldToViewportPoint(points[i]);
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        /// <summary>Negative degrees punch in (narrower FOV), positive punch out.</summary>
        public void Kick(float degrees)
        {
            _fovVelocity += degrees * 18f;
        }

        /// <summary>Cinematic push toward a floor point; zoom &gt; 1 moves closer.</summary>
        public void FocusOn(Vector3 worldPoint, float zoom, float follow = 0.6f)
        {
            _focusTarget = (worldPoint - _pivot) * follow;
            _focusTarget.y = 0f;
            _zoomTarget = zoom;
        }

        public void ResetFocus()
        {
            _focusTarget = Vector3.zero;
            _zoomTarget = 1f;
        }

        private void LateUpdate()
        {
            if (!_framed)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _trauma = Mathf.Max(0f, _trauma - _traumaDecay * dt);
            float omega = 2f * Mathf.PI * 4f;
            _fovVelocity += (-omega * omega * _fovKick - omega * _fovVelocity) * dt;
            _fovKick += _fovVelocity * dt;
            float blend = 1f - Mathf.Exp(-3.2f * dt);
            _zoom = Mathf.Lerp(_zoom, _zoomTarget, blend);
            _focus = Vector3.Lerp(_focus, _focusTarget, blend);
            Apply();
        }

        private void Apply()
        {
            float shake = _trauma * _trauma;
            float time = Time.unscaledTime * 22f;
            var offset = new Vector3(Mathf.PerlinNoise(time, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, time) - 0.5f, 0f) * (2f * _maxShakeOffset * shake);
            float roll = (Mathf.PerlinNoise(time, time * 0.5f) - 0.5f) * 2f * _maxShakeAngle * shake;

            Vector3 forward = _rotation * Vector3.forward;
            Vector3 position = _pivot + _focus - forward * (_distance / Mathf.Max(0.2f, _zoom));
            Camera.transform.SetPositionAndRotation(position + _rotation * offset, _rotation * Quaternion.Euler(0f, 0f, roll));
            Camera.fieldOfView = _fieldOfView + _fovKick;
        }
    }
}
