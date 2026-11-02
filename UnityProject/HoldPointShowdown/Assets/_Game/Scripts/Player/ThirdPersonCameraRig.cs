using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Player
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _targetOffset = new(0f, 1.45f, 0f);
    [SerializeField, Min(1f)] private float _distance = 6.5f;
    [SerializeField] private float _shoulderOffset = 0.85f;
    [SerializeField, Min(0.01f)] private float _mouseSensitivity = 0.12f;
    [SerializeField, Min(1f)] private float _gamepadSensitivity = 120f;
    [SerializeField] private Vector2 _pitchLimits = new(-30f, 65f);
    [SerializeField, Min(0f)] private float _collisionRadius = 0.2f;

    private InputAction _lookAction;
    private float _yaw;
    private float _pitch = 10f;

    public void Configure(Transform target)
    {
        _target = target;
        _targetOffset = new Vector3(0f, 1.45f, 0f);
        _distance = 6.5f;
        _shoulderOffset = 0.85f;
        _pitch = 10f;
    }

    private void Awake()
    {
        _lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        _lookAction.AddBinding("<Mouse>/delta");
        _lookAction.AddBinding("<Gamepad>/rightStick");
        _yaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        _lookAction?.Enable();
    }

    private void OnDisable()
    {
        _lookAction?.Disable();
    }

    private void OnDestroy()
    {
        _lookAction?.Dispose();
    }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector2 look = _lookAction.ReadValue<Vector2>();
            bool usingMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f;
            float sensitivity = usingMouse ? _mouseSensitivity : _gamepadSensitivity * Time.deltaTime;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw += look.x * sensitivity;
                _pitch = Mathf.Clamp(_pitch - (look.y * sensitivity), _pitchLimits.x, _pitchLimits.y);
            }

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = _target.position + _targetOffset;
            Vector3 desiredPosition = pivot + (rotation * new Vector3(_shoulderOffset, 0f, -_distance));
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
                desiredPosition = pivot + (direction.normalized * Mathf.Max(0.1f, hit.distance - _collisionRadius));
            }

            transform.SetPositionAndRotation(desiredPosition, rotation);
        }
    }
}
