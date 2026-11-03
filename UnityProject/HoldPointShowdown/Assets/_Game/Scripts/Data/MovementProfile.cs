using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "MovementProfile", menuName = "HoldPoint Showdown/Movement Profile")]
    public sealed class MovementProfile : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _walkSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float _sprintSpeed = 7f;
        [SerializeField, Min(0.1f)] private float _crouchSpeed = 2.2f;
        [SerializeField, Min(0.1f)] private float _acceleration = 22f;
        [SerializeField, Min(0.1f)] private float _deceleration = 28f;
        [SerializeField, Range(0f, 1f)] private float _airControl = 0.35f;
        [SerializeField, Min(0.1f)] private float _jumpHeight = 1.1f;
        [SerializeField, Min(0f)] private float _jumpCooldown = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float _adsMoveMultiplier = 0.55f;
        [SerializeField, Min(1f)] private float _followDistance = 3.2f;
        [SerializeField, Min(0f)] private float _cameraHeight = 1.6f;
        [SerializeField, Range(40f, 80f)] private float _verticalFov = 60f;
        [SerializeField, Min(1f)] private float _yawSpeed = 180f;
        [SerializeField, Min(1f)] private float _pitchSpeed = 120f;
        [SerializeField, Range(0.1f, 1f)] private float _adsSensitivity = 0.6f;
        [SerializeField, Min(1f)] private float _aimAssistRange = 45f;
        [SerializeField, Range(1f, 30f)] private float _slowdownAngle = 12f;
        [SerializeField, Range(0f, 1f)] private float _slowdownFactor = 0.45f;
        [SerializeField, Range(1f, 15f)] private float _snapAngle = 6f;
        [SerializeField, Range(0f, 1f)] private float _snapRate = 0.24f;

        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float CrouchSpeed => _crouchSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float AirControl => _airControl;
        public float JumpHeight => _jumpHeight;
        public float JumpCooldown => _jumpCooldown;
        public float AdsMoveMultiplier => _adsMoveMultiplier;
        public float FollowDistance => _followDistance;
        public float CameraHeight => _cameraHeight;
        public float VerticalFov => _verticalFov;
        public float YawSpeed => _yawSpeed;
        public float PitchSpeed => _pitchSpeed;
        public float AdsSensitivity => _adsSensitivity;
        public float AimAssistRange => _aimAssistRange;
        public float SlowdownAngle => _slowdownAngle;
        public float SlowdownFactor => _slowdownFactor;
        public float SnapAngle => _snapAngle;
        public float SnapRate => _snapRate;

        public void SetRecommendedValues()
        {
            _walkSpeed = 4.5f;
            _sprintSpeed = 7f;
            _crouchSpeed = 2.2f;
            _acceleration = 22f;
            _deceleration = 28f;
            _airControl = 0.35f;
            _jumpHeight = 1.1f;
            _jumpCooldown = 0.45f;
            _adsMoveMultiplier = 0.55f;
            _followDistance = 3.2f;
            _cameraHeight = 1.6f;
            _verticalFov = 60f;
            _yawSpeed = 180f;
            _pitchSpeed = 120f;
            _adsSensitivity = 0.6f;
            _aimAssistRange = 45f;
            _slowdownAngle = 12f;
            _slowdownFactor = 0.45f;
            _snapAngle = 6f;
            _snapRate = 0.24f;
        }
    }
}
