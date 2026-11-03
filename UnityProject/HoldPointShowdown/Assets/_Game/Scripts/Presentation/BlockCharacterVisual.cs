using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BlockCharacterVisual : MonoBehaviour
    {
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private Transform _leftArmPivot;
        [SerializeField] private Transform _rightArmPivot;
        [SerializeField] private Transform _leftLegPivot;
        [SerializeField] private Transform _rightLegPivot;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _weaponSocket;
        [SerializeField] private Renderer[] _renderers;

        private Vector3 _lastWorldPosition;
        private Vector3 _modelStartPosition;
        private Quaternion _modelStartRotation;
        private float _stepPhase;
        private float _crouchBlend;
        private float _sprintBlend;
        private float _movementBlend;
        private float _smoothedSpeed;
        private bool _isCrouching;
        private bool _isSprinting;

        public Transform Muzzle => _muzzle;
        public Transform WeaponSocket => _weaponSocket != null ? _weaponSocket : _muzzle;
        public Renderer[] Renderers => _renderers;

        public void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
        }

        public void SetSprinting(bool sprinting)
        {
            _isSprinting = sprinting;
        }

        public void Configure(
            Transform modelRoot,
            Transform leftArmPivot,
            Transform rightArmPivot,
            Transform leftLegPivot,
            Transform rightLegPivot,
            Transform muzzle,
            Transform weaponSocket,
            Renderer[] renderers)
        {
            _modelRoot = modelRoot;
            _leftArmPivot = leftArmPivot;
            _rightArmPivot = rightArmPivot;
            _leftLegPivot = leftLegPivot;
            _rightLegPivot = rightLegPivot;
            _muzzle = muzzle;
            _weaponSocket = weaponSocket;
            _renderers = renderers;
        }

        public void Configure(
            Transform modelRoot,
            Transform leftArmPivot,
            Transform rightArmPivot,
            Transform leftLegPivot,
            Transform rightLegPivot,
            Transform muzzle,
            Renderer[] renderers)
        {
            Configure(modelRoot, leftArmPivot, rightArmPivot, leftLegPivot, rightLegPivot, muzzle, muzzle, renderers);
        }

        private void Awake()
        {
            _lastWorldPosition = transform.position;
            _modelStartPosition = _modelRoot != null ? _modelRoot.localPosition : Vector3.zero;
            _modelStartRotation = _modelRoot != null ? _modelRoot.localRotation : Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (_modelRoot == null || Time.deltaTime <= 0f)
            {
                return;
            }

            Vector3 displacement = Vector3.ProjectOnPlane(transform.position - _lastWorldPosition, Vector3.up);
            float speed = Mathf.Clamp(displacement.magnitude / Time.deltaTime, 0f, 9f);
            _lastWorldPosition = transform.position;
            _smoothedSpeed = Mathf.Lerp(
                _smoothedSpeed,
                speed,
                1f - Mathf.Exp(-10f * Time.deltaTime));
            float targetMovement = Mathf.InverseLerp(0.1f, 2.2f, _smoothedSpeed);
            _movementBlend = Mathf.MoveTowards(_movementBlend, targetMovement, Time.deltaTime * 6f);
            float targetSprint = (_isSprinting || _smoothedSpeed > 6f) && !_isCrouching ? 1f : 0f;
            _sprintBlend = Mathf.MoveTowards(_sprintBlend, targetSprint, Time.deltaTime * 5f);
            float cadence = Mathf.Lerp(7.2f, 10.4f, _sprintBlend);
            _stepPhase += cadence * Time.deltaTime * _movementBlend;

            float stride = Mathf.Sin(_stepPhase);
            float legAngle = Mathf.Lerp(23f, 36f, _sprintBlend) * stride * _movementBlend;
            _crouchBlend = Mathf.MoveTowards(
                _crouchBlend,
                _isCrouching ? 1f : 0f,
                Time.deltaTime * 8f);
            float leftArmAngle = -legAngle * Mathf.Lerp(0.62f, 0.78f, _sprintBlend);
            float weaponArmAngle = legAngle * Mathf.Lerp(0.12f, 0.2f, _sprintBlend);
            SetPivotRotation(_leftArmPivot, Mathf.Lerp(leftArmAngle, 18f, _crouchBlend));
            SetPivotRotation(_rightArmPivot, Mathf.Lerp(weaponArmAngle, 8f, _crouchBlend));
            SetPivotRotation(_leftLegPivot, Mathf.Lerp(legAngle, 30f - (legAngle * 0.2f), _crouchBlend));
            SetPivotRotation(_rightLegPivot, Mathf.Lerp(-legAngle, 30f + (legAngle * 0.2f), _crouchBlend));

            float bobHeight = Mathf.Lerp(0.045f, 0.085f, _sprintBlend);
            float bob = Mathf.Abs(Mathf.Sin(_stepPhase)) * bobHeight * _movementBlend;
            _modelRoot.localPosition = Vector3.Lerp(
                _modelRoot.localPosition,
                _modelStartPosition + (Vector3.up * (bob - (0.22f * _crouchBlend))),
                1f - Mathf.Exp(-18f * Time.deltaTime));

            float forwardLean = Mathf.Lerp(1.5f, 9f, _sprintBlend) * _movementBlend;
            float sideSway = Mathf.Sin(_stepPhase * 0.5f) * Mathf.Lerp(0.8f, 2.2f, _sprintBlend) * _movementBlend;
            Quaternion locomotionRotation = _modelStartRotation * Quaternion.Euler(forwardLean, 0f, sideSway);
            _modelRoot.localRotation = Quaternion.Slerp(
                _modelRoot.localRotation,
                locomotionRotation,
                1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        private static void SetPivotRotation(Transform pivot, float xAngle)
        {
            if (pivot != null)
            {
                pivot.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
            }
        }
    }
}
