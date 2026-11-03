using UnityEngine;
using UnityEngine.InputSystem;
using HoldPointShowdown.Presentation;

namespace HoldPointShowdown.Player
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        public const string PerspectiveKeyboardBinding = "<Keyboard>/v";

        [Header("Follow")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new(0f, 1.6f, 0f);
        [SerializeField, Min(1f)] private float _distance = 9f;
        [SerializeField] private float _shoulderOffset = 1f;
        [SerializeField, Min(0f)] private float _collisionRadius = 0.34f;

        [Header("Perspective")]
        [SerializeField] private Vector3 _firstPersonOffset = new(0f, 2.28f, 0.12f);
        [SerializeField] private bool _startInFirstPerson;

        [Header("Aim")]
        [SerializeField, Min(0.01f)] private float _mouseSensitivity = 0.1f;
        [SerializeField, Min(1f)] private float _gamepadSensitivity = 120f;
        [SerializeField] private Vector2 _pitchLimits = new(-30f, 65f);

        [Header("Zoom")]
        [SerializeField] private Vector2 _zoomLimits = new(5.5f, 12f);
        [SerializeField, Min(0.1f)] private float _zoomStep = 1f;
        [SerializeField, Min(0.01f)] private float _zoomSmoothTime = 0.12f;

        private float _yaw;
        private float _pitch = 10f;
        private float _targetDistance;
        private float _currentDistance;
        private float _zoomVelocity;
        private bool _isFirstPerson;
        private Renderer[] _localBodyRenderers;

        public float CurrentDistance => _currentDistance;
        public float TargetDistance => _targetDistance;
        public Vector2 LookAngles => new(_yaw, _pitch);
        public bool IsFirstPerson => _isFirstPerson;

        public void Configure(Transform target)
        {
            _target = target;
            _targetOffset = new Vector3(0f, 1.6f, 0f);
            _distance = 9f;
            _shoulderOffset = 1f;
            _zoomLimits = new Vector2(5.5f, 12f);
            _pitch = 10f;
            CacheLocalBodyRenderers();
            SetFirstPerson(_startInFirstPerson);
            ResetDistance();
        }

        public void TogglePerspective()
        {
            SetFirstPerson(!_isFirstPerson);
        }

        public void SetFirstPerson(bool firstPerson)
        {
            _isFirstPerson = firstPerson;
            CacheLocalBodyRenderers();
            if (_localBodyRenderers == null)
            {
                return;
            }

            foreach (Renderer bodyRenderer in _localBodyRenderers)
            {
                if (bodyRenderer != null)
                {
                    bodyRenderer.forceRenderingOff = firstPerson;
                }
            }
        }

        public void ApplyLookDelta(Vector2 lookDelta, bool mouseInput)
        {
            float sensitivity = mouseInput
                ? _mouseSensitivity
                : _gamepadSensitivity * Time.deltaTime;
            _yaw += lookDelta.x * sensitivity;
            _pitch = Mathf.Clamp(
                _pitch - (lookDelta.y * sensitivity),
                _pitchLimits.x,
                _pitchLimits.y);
        }

        public void SetZoomDistance(float distance, bool immediate = false)
        {
            _targetDistance = Mathf.Clamp(distance, _zoomLimits.x, _zoomLimits.y);
            if (immediate)
            {
                _currentDistance = _targetDistance;
                _zoomVelocity = 0f;
            }
        }

        private void Awake()
        {
            _yaw = transform.eulerAngles.y;
            _isFirstPerson = _startInFirstPerson;
            ResetDistance();
        }

        private void OnDisable()
        {
            if (_localBodyRenderers == null)
            {
                return;
            }

            foreach (Renderer bodyRenderer in _localBodyRenderers)
            {
                if (bodyRenderer != null)
                {
                    bodyRenderer.forceRenderingOff = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            ReadAimInput();
            ReadPerspectiveInput();
            ReadZoomInput();

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            if (_isFirstPerson)
            {
                Vector3 firstPersonPosition = _target.position
                    + (Vector3.up * _firstPersonOffset.y)
                    + (rotation * new Vector3(_firstPersonOffset.x, 0f, _firstPersonOffset.z));
                transform.SetPositionAndRotation(firstPersonPosition, rotation);
                return;
            }

            _currentDistance = Mathf.SmoothDamp(
                _currentDistance,
                _targetDistance,
                ref _zoomVelocity,
                _zoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            Vector3 pivot = _target.position + _targetOffset;
            Vector3 desiredPosition = pivot + (rotation * new Vector3(
                _shoulderOffset,
                0f,
                -_currentDistance));
            Vector3 direction = desiredPosition - pivot;

            if (Physics.SphereCast(
                    pivot,
                    _collisionRadius,
                    direction.normalized,
                    out RaycastHit hit,
                    direction.magnitude,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                desiredPosition = pivot + (
                    direction.normalized * Mathf.Max(0.1f, hit.distance - _collisionRadius));
            }

            transform.SetPositionAndRotation(desiredPosition, rotation);
        }

        private void ReadAimInput()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                if (mouseDelta.sqrMagnitude > 0f)
                {
                    ApplyLookDelta(mouseDelta, true);
                }
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.001f)
                {
                    ApplyLookDelta(stick, false);
                }
            }
        }

        private void ReadZoomInput()
        {
            if (_isFirstPerson || Cursor.lockState != CursorLockMode.Locked || Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            float steps = Mathf.Clamp(scroll / 120f, -3f, 3f);
            SetZoomDistance(_targetDistance - (steps * _zoomStep));
        }

        private void ReadPerspectiveInput()
        {
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                TogglePerspective();
            }
        }

        private void CacheLocalBodyRenderers()
        {
            if (_target == null)
            {
                return;
            }

            BlockCharacterVisual visual = _target.GetComponentInChildren<BlockCharacterVisual>();
            _localBodyRenderers = visual != null ? visual.Renderers : null;
        }

        private void ResetDistance()
        {
            _zoomLimits.x = Mathf.Max(1f, _zoomLimits.x);
            _zoomLimits.y = Mathf.Max(_zoomLimits.x, _zoomLimits.y);
            _targetDistance = Mathf.Clamp(_distance, _zoomLimits.x, _zoomLimits.y);
            _currentDistance = _targetDistance;
            _zoomVelocity = 0f;
        }
    }
}
