using Cysharp.Threading.Tasks;
using DG.Tweening;
using PixelDefense.Core;
using PixelDefense.Services.Motion;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    public enum CannonState
    {
        InColumn,
        Jumping,
        InSlot,
        Leaving,
        Gone
    }

    /// <summary>
    /// One cannon critter: idles in its column, hops to a base slot, turns to face its targets, recoils on every
    /// shot and spins away when empty. Its ammo is a pixel-font decal on the lid that always reads upright.
    /// </summary>
    public sealed class CannonView : MonoBehaviour
    {
        private const float HiddenGray = 0.62f;
        private const float RestPitch = -8f;

        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int EmissionId = Shader.PropertyToID("_Emission");

        private VisualConfig _config;
        private CannonMeshes _meshes;
        private Transform _squash;
        private Transform _yaw;
        private Transform _turret;
        private Transform _barrel;
        private MeshFilter _bodyFilter;
        private MeshFilter _barrelFilter;
        private MeshRenderer _bodyRenderer;
        private MeshRenderer _barrelRenderer;
        private MeshRenderer _labelRenderer;
        private Transform _label;
        private PixelFont.Label _labelMesh;
        private MaterialPropertyBlock _block;
        private FloatSpring _squashSpring;
        private FloatSpring _recoil;
        private FloatSpring _labelPunch;
        private float _yawCurrent;
        private float _yawTarget;
        private float _flash;
        private float _hint;
        private float _idlePhase;
        private Tween _move;
        private float _traySize;
        private float _slotSize;

        public int CannonId { get; private set; }
        public CannonSpec Spec { get; private set; }
        public CannonState State { get; private set; }
        public bool Revealed { get; private set; }
        public Color TintColor => _config.ScaleColor(Spec.Color);

        public Vector3 MuzzlePosition => _barrel.TransformPoint(new Vector3(0f, 0f, CannonMeshes.BarrelLength + 0.05f));

        public void Create(VisualConfig config, CannonMeshes meshes)
        {
            _config = config;
            _meshes = meshes;
            _block = new MaterialPropertyBlock();
            _traySize = config.CannonSize * config.WorldPerSlice;
            _slotSize = config.SlotCannonSize * config.WorldPerSlice;
            transform.localScale = Vector3.one * _traySize;

            _squash = new GameObject("Squash").transform;
            _squash.SetParent(transform, false);
            _yaw = new GameObject("Yaw").transform;
            _yaw.SetParent(_squash, false);

            var body = new GameObject("Body");
            body.transform.SetParent(_yaw, false);
            _bodyFilter = body.AddComponent<MeshFilter>();
            _bodyRenderer = body.AddComponent<MeshRenderer>();
            _bodyRenderer.sharedMaterial = config.ToyMaterial;

            _turret = new GameObject("Turret").transform;
            _turret.SetParent(_yaw, false);
            _turret.localPosition = CannonMeshes.TurretPivot;
            _turret.localRotation = Quaternion.Euler(RestPitch, 0f, 0f);
            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(_turret, false);
            _barrel = barrel.transform;
            _barrelFilter = barrel.AddComponent<MeshFilter>();
            _barrelRenderer = barrel.AddComponent<MeshRenderer>();
            _barrelRenderer.sharedMaterial = config.ToyMaterial;

            _labelMesh = new PixelFont.Label();
            var label = new GameObject("Ammo");
            label.transform.SetParent(_squash, false);
            _label = label.transform;
            _label.localPosition = CannonMeshes.LabelCenter;
            label.AddComponent<MeshFilter>().sharedMesh = _labelMesh.Mesh;
            _labelRenderer = label.AddComponent<MeshRenderer>();
            _labelRenderer.sharedMaterial = config.DigitsMaterial;
            _labelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _labelRenderer.receiveShadows = false;
        }

        public void Bind(int cannonId, CannonSpec spec, bool revealed)
        {
            _move?.Kill();
            CannonId = cannonId;
            Spec = spec;
            State = CannonState.InColumn;
            Revealed = revealed;
            _yawCurrent = 0f;
            _yawTarget = 0f;
            _flash = 0f;
            _hint = 0f;
            _idlePhase = Random.value * 10f;
            _squashSpring = new FloatSpring(0f);
            _recoil = new FloatSpring(0f);
            _labelPunch = new FloatSpring(0f);
            _squash.localPosition = Vector3.zero;
            _squash.localRotation = Quaternion.identity;
            _squash.localScale = Vector3.one;
            transform.localScale = Vector3.one * _traySize;
            ApplyLook();
            SetAmmo(spec.Ammo, punch: false);
            gameObject.SetActive(true);
        }

        private void ApplyLook()
        {
            Color color = Revealed ? _config.ScaleColor(Spec.Color) : new Color(HiddenGray, HiddenGray, HiddenGray + 0.08f);
            _bodyFilter.sharedMesh = _meshes.Body(color);
            _barrelFilter.sharedMesh = _meshes.Barrel(color);
        }

        public void SetAmmo(int ammo, bool punch = true)
        {
            _labelMesh.SetNumber(Revealed || State != CannonState.InColumn ? ammo : -1, 0.5f);
            _label.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (punch)
            {
                _labelPunch.Kick(9f);
            }
        }

        public void Reveal()
        {
            if (Revealed)
            {
                return;
            }

            Revealed = true;
            ApplyLook();
            SetAmmo(Spec.Ammo);
            _squashSpring.Kick(10f);
            _flash = 1f;
            _yaw.DOLocalRotate(new Vector3(0f, 360f, 0f), 0.45f, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        public void PlaceAt(Vector3 position)
        {
            _move?.Kill();
            transform.position = position;
        }

        public void SlideTo(Vector3 position, float delay)
        {
            _move?.Kill();
            _move = transform.DOMove(position, 0.26f).SetDelay(delay).SetEase(Ease.OutBack, 1.6f).SetLink(gameObject)
                .OnStart(() => _squashSpring.Kick(-4f));
        }

        public void PopIn(Vector3 position, float delay)
        {
            _move?.Kill();
            gameObject.SetActive(true);
            transform.position = position;
            transform.localScale = Vector3.zero;
            _move = transform.DOScale(_traySize, 0.3f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        public async UniTask JumpTo(Vector3 position, float duration, System.Threading.CancellationToken token)
        {
            _move?.Kill();
            State = CannonState.Jumping;
            _hint = 0f;
            transform.localScale = Vector3.one * _traySize;
            _squashSpring.Value = 0.35f;
            _squashSpring.Velocity = -6f;
            _flash = 0.6f;
            SetAmmo(Spec.Ammo, punch: false);

            Sequence jump = BuildJump(position, duration);
            _move = jump;
            await jump.ToUniTask(TweenCancelBehaviour.Complete, token);
            State = CannonState.InSlot;
            _squashSpring.Value = -0.4f;
            _squashSpring.Velocity = 0f;
        }

        private Sequence BuildJump(Vector3 position, float duration)
        {
            // Shrinks in flight: tray cannons are big for thumbs, seated cannons fit the small base pads.
            Sequence jump = transform.DOJump(position, 2.6f, 1, duration).SetEase(Ease.Linear).SetLink(gameObject);
            jump.Join(transform.DOScale(_slotSize, duration).SetEase(Ease.InOutQuad));
            return jump;
        }

        public void Aim(Vector3 target)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 1e-6f)
            {
                _yawTarget = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            }
        }

        public void Fire()
        {
            _recoil.Value = 0.22f;
            _recoil.Velocity = 0f;
            _squashSpring.Kick(-5f);
            _flash = Mathf.Max(_flash, 0.35f);
        }

        public void Deny()
        {
            _squash.DOKill();
            _squash.localRotation = Quaternion.identity;
            _squash.DOPunchRotation(new Vector3(0f, 0f, 18f), 0.38f, 12, 0.6f).SetLink(gameObject);
            _flash = 0.5f;
        }

        public void SetHint(bool on)
        {
            _hint = on ? 1f : 0f;
        }

        public async UniTask Leave(bool dismissed, System.Threading.CancellationToken token)
        {
            State = CannonState.Leaving;
            _move?.Kill();
            _squashSpring.Kick(dismissed ? -6f : 12f);
            _flash = 0.8f;
            Sequence exit = BuildExit(dismissed);
            _move = exit;
            await exit.ToUniTask(TweenCancelBehaviour.Complete, token);
            State = CannonState.Gone;
            gameObject.SetActive(false);
        }

        private Sequence BuildExit(bool dismissed)
        {
            Sequence exit = DOTween.Sequence().SetLink(gameObject);
            exit.Append(_yaw.DOLocalRotate(new Vector3(0f, dismissed ? 180f : 720f, 0f), 0.34f, RotateMode.LocalAxisAdd).SetEase(Ease.InCubic));
            exit.Join(transform.DOScale(transform.localScale * 1.25f, 0.12f).SetEase(Ease.OutQuad));
            exit.Insert(0.12f, transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack));
            exit.Join(transform.DOMoveY(transform.position.y + (dismissed ? 0.1f : 0.5f), 0.22f));
            return exit;
        }

        /// <summary>Parks a column cannon that is too deep to be visible; <see cref="PopIn"/> brings it back.</summary>
        public void Hide()
        {
            _move?.Kill();
            gameObject.SetActive(false);
        }

        public void Recycle()
        {
            _move?.Kill();
            State = CannonState.Gone;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;
            _squashSpring.Step(0f, 6f, 0.32f, dt);
            _recoil.Step(0f, 9f, 0.7f, dt);
            _labelPunch.Step(0f, 8f, 0.4f, dt);
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 4f);

            float squash = _squashSpring.Value * 0.18f;
            float idle = State == CannonState.InColumn ? Mathf.Sin(time * 3.1f + _idlePhase) * 0.025f : 0f;
            float hintBounce = _hint > 0f ? Mathf.Abs(Mathf.Sin(time * 7f)) * 0.22f : 0f;
            _squash.localScale = new Vector3(1f + squash - idle * 0.5f, 1f - squash + idle, 1f + squash - idle * 0.5f);
            _squash.localPosition = new Vector3(0f, hintBounce, 0f);

            if (State == CannonState.InSlot)
            {
                _yawCurrent = Mathf.LerpAngle(_yawCurrent, _yawTarget, 1f - Mathf.Exp(-14f * dt));
            }
            else if (State == CannonState.InColumn)
            {
                _yawCurrent = Mathf.LerpAngle(_yawCurrent, 0f, 1f - Mathf.Exp(-10f * dt));
            }

            if (State != CannonState.Leaving)
            {
                _yaw.localRotation = Quaternion.Euler(0f, _yawCurrent, 0f);
            }

            _barrel.localPosition = new Vector3(0f, 0f, -Mathf.Max(0f, _recoil.Value));
            float labelScale = 1f + _labelPunch.Value * 0.05f;
            _label.localScale = new Vector3(labelScale, labelScale, labelScale);

            float glow = _hint > 0f ? 0.25f + 0.25f * Mathf.Sin(time * 7f) : 0f;
            _block.SetFloat(FlashId, Mathf.Clamp01(_flash * 0.75f));
            _block.SetFloat(EmissionId, glow);
            _bodyRenderer.SetPropertyBlock(_block);
            _barrelRenderer.SetPropertyBlock(_block);
        }

        private void OnDisable()
        {
            _move?.Kill();
        }

        private void OnDestroy()
        {
            if (_labelMesh != null)
            {
                Destroy(_labelMesh.Mesh);
            }
        }
    }
}
