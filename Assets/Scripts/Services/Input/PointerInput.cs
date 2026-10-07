using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelDefense.Services.Input
{
    /// <summary>
    /// Single-pointer input on the Input System, event driven so even a press and release inside one frame (a
    /// quick tap on a slow device) is seen. Binds to <c>&lt;Pointer&gt;</c>: the primary touch on devices, the
    /// mouse in the Editor. Gameplay subscribes to <see cref="Pressed"/> and never reads devices directly.
    /// </summary>
    public sealed class PointerInput : MonoBehaviour
    {
        private InputAction _press;

        /// <summary>Screen position (pixels, origin bottom-left) of a new press.</summary>
        public event Action<Vector2> Pressed;

        public event Action<Vector2> Released;

        /// <summary>Returns true when a screen position is covered by interactive UI.</summary>
        public Func<Vector2, bool> BlockedBy { get; set; }

        public bool IsHeld { get; private set; }
        public Vector2 Position { get; private set; }

        private void Awake()
        {
            _press = new InputAction("PointerPress", InputActionType.Button, "<Pointer>/press");
            _press.started += OnStarted;
            _press.canceled += OnCanceled;
        }

        private void OnEnable()
        {
            _press.Enable();
        }

        private void OnDisable()
        {
            _press.Disable();
            IsHeld = false;
        }

        private void OnDestroy()
        {
            _press.Dispose();
        }

        private void OnStarted(InputAction.CallbackContext context)
        {
            if (IsHeld)
            {
                return;
            }

            Position = ReadPosition(context);
            Func<Vector2, bool> blocked = BlockedBy;
            if (blocked != null && blocked(Position))
            {
                return;
            }

            IsHeld = true;
            Pressed?.Invoke(Position);
        }

        private void OnCanceled(InputAction.CallbackContext context)
        {
            if (!IsHeld)
            {
                return;
            }

            IsHeld = false;
            Position = ReadPosition(context);
            Released?.Invoke(Position);
        }

        private Vector2 ReadPosition(InputAction.CallbackContext context)
        {
            return context.control.device is Pointer pointer ? pointer.position.ReadValue() : Position;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null)
            {
                Position = pointer.position.ReadValue();
            }
        }
    }
}
