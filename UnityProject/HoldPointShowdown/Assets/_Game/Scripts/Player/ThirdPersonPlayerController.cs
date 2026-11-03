using HoldPointShowdown.Combat;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Player
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonPlayerController : MonoBehaviour
    {
        public const string CrouchKeyboardBinding = "<Keyboard>/leftCtrl";
        public const string SprintKeyboardBinding = "<Keyboard>/leftShift";
        public const float BackwardDoubleTapWindow = 0.32f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 6.5f;
        [SerializeField, Min(0.1f)] private float _sprintSpeed = 10f;
        [SerializeField, Min(0.1f)] private float _crouchSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float _rotationSpeed = 14f;
        [SerializeField, Min(0.1f)] private float _jumpHeight = 1.25f;
        [SerializeField, Min(0.1f)] private float _gravity = 24f;

        [Header("References")]
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private PrototypeHitscanWeapon _weapon;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _sprintAction;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private InputAction _cursorAction;
        private float _verticalVelocity;
        private bool _isCrouching;
        private bool _isSprinting;
        private float _lastBackwardPressedAt = -10f;
        private bool _cursorUnlockedByUser;
        private float _standingHeight;
        private Vector3 _standingCenter;
        private Damageable _damageable;
        private CombatantLifecycle _lifecycle;
        private BlockCharacterVisual _characterVisual;

        public bool IsCrouching => _isCrouching;
        public bool IsSprinting => _isSprinting;
        public float MoveSpeed => _moveSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float CrouchSpeed => _crouchSpeed;

        public void Configure(Camera playerCamera, PrototypeHitscanWeapon weapon)
        {
            _playerCamera = playerCamera;
            _weapon = weapon;
        }

        public void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
            if (crouching)
            {
                SetSprinting(false);
            }
            _characterController.height = crouching ? _standingHeight * 0.6f : _standingHeight;
            _characterController.center = crouching
                ? _standingCenter - new Vector3(0f, _standingHeight * 0.2f, 0f)
                : _standingCenter;
            _characterVisual?.SetCrouching(crouching);
        }

        private void SetSprinting(bool sprinting)
        {
            _isSprinting = sprinting;
            _characterVisual?.SetSprinting(sprinting);
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _damageable = GetComponent<Damageable>();
            _lifecycle = GetComponent<CombatantLifecycle>();
            _characterVisual = GetComponentInChildren<BlockCharacterVisual>();
            _standingHeight = _characterController.height;
            _standingCenter = _characterController.center;

            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");
            _crouchAction = new InputAction("Crouch", InputActionType.Button, CrouchKeyboardBinding);
            _crouchAction.AddBinding("<Keyboard>/rightCtrl");
            _crouchAction.AddBinding("<Gamepad>/rightStickPress");
            _sprintAction = new InputAction("Sprint", InputActionType.Button, SprintKeyboardBinding);
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");
            _fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            _fireAction.AddBinding("<Gamepad>/rightTrigger");
            _reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
            _reloadAction.AddBinding("<Gamepad>/buttonWest");
            _cursorAction = new InputAction("Toggle Cursor", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _jumpAction.Enable();
            _crouchAction.Enable();
            _sprintAction.Enable();
            _fireAction.Enable();
            _reloadAction.Enable();
            _cursorAction.Enable();
            _cursorUnlockedByUser = false;
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetSprinting(false);
            _moveAction.Disable();
            _jumpAction.Disable();
            _crouchAction.Disable();
            _sprintAction.Disable();
            _fireAction.Disable();
            _reloadAction.Disable();
            _cursorAction.Disable();
        }

        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();
            _crouchAction?.Dispose();
            _sprintAction?.Dispose();
            _fireAction?.Dispose();
            _reloadAction?.Dispose();
            _cursorAction?.Dispose();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !_cursorUnlockedByUser)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            if (_cursorAction.WasPressedThisFrame())
            {
                bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
                _cursorUnlockedByUser = !shouldLock;
                SetCursorLocked(shouldLock);
            }

            if (Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _cursorUnlockedByUser = false;
                SetCursorLocked(true);
                return;
            }

            if (!_cursorUnlockedByUser
                && Application.isFocused
                && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }

            _lifecycle ??= GetComponent<CombatantLifecycle>();
            if ((_lifecycle != null && !_lifecycle.CanAct)
                || (_lifecycle == null && _damageable != null && !_damageable.IsAlive))
            {
                if (_isCrouching)
                {
                    SetCrouching(false);
                }
                SetSprinting(false);
                return;
            }

            if (ShouldToggleCrouch())
            {
                SetCrouching(!_isCrouching);
            }

            Move();

            if (Cursor.lockState == CursorLockMode.Locked && _fireAction.IsPressed())
            {
                _weapon?.TryFire();
            }

            if (_reloadAction.WasPressedThisFrame())
            {
                _weapon?.Reload();
            }
        }

        private void Move()
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();
            Transform cameraTransform = _playerCamera != null ? _playerCamera.transform : transform;
            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector3 horizontal = (cameraForward * input.y) + (cameraRight * input.x);

            if (horizontal.sqrMagnitude > 1f)
            {
                horizontal.Normalize();
            }

            bool sprinting = !_isCrouching
                && horizontal.sqrMagnitude > 0.01f
                && _sprintAction.IsPressed();
            SetSprinting(sprinting);

            if (_characterController.isGrounded)
            {
                _verticalVelocity = -2f;
                if (_jumpAction.WasPressedThisFrame() && !_isCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(2f * _gravity * _jumpHeight);
                }
            }
            else
            {
                _verticalVelocity -= _gravity * Time.deltaTime;
            }

            float speed = _isCrouching
                ? _crouchSpeed
                : (_isSprinting ? _sprintSpeed : _moveSpeed);
            Vector3 velocity = (horizontal * speed) + (Vector3.up * _verticalVelocity);
            _characterController.Move(velocity * Time.deltaTime);

            Vector3 facing = Cursor.lockState == CursorLockMode.Locked
                ? cameraForward
                : horizontal;
            if (facing.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facing, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime));
            }
        }

        private bool ShouldToggleCrouch()
        {
            bool toggle = _crouchAction.WasPressedThisFrame();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.sKey.wasPressedThisFrame)
            {
                return toggle;
            }

            float now = Time.unscaledTime;
            if (now - _lastBackwardPressedAt <= BackwardDoubleTapWindow)
            {
                _lastBackwardPressedAt = -10f;
                return true;
            }

            _lastBackwardPressedAt = now;
            return toggle;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
