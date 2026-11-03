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
        private float _stepPhase;
        private float _crouchBlend;
        private bool _isCrouching;

        public Transform Muzzle => _muzzle;
        public Transform WeaponSocket => _weaponSocket != null ? _weaponSocket : _muzzle;
        public Renderer[] Renderers => _renderers;

        public void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
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
        }

        private void LateUpdate()
        {
            if (_modelRoot == null || Time.deltaTime <= 0f)
            {
                return;
            }

            Vector3 displacement = Vector3.ProjectOnPlane(transform.position - _lastWorldPosition, Vector3.up);
            float speed = Mathf.Clamp(displacement.magnitude / Time.deltaTime, 0f, 7f);
            _lastWorldPosition = transform.position;
            _stepPhase += speed * Time.deltaTime * 2.4f;

            float activity = Mathf.InverseLerp(0.05f, 2f, speed);
            float swing = Mathf.Sin(_stepPhase * Mathf.PI) * 28f * activity;
            _crouchBlend = Mathf.MoveTowards(
                _crouchBlend,
                _isCrouching ? 1f : 0f,
                Time.deltaTime * 8f);
            SetPivotRotation(_leftArmPivot, swing);
            SetPivotRotation(_rightArmPivot, -swing * 0.55f);
            SetPivotRotation(_leftLegPivot, Mathf.Lerp(-swing, 32f - (swing * 0.25f), _crouchBlend));
            SetPivotRotation(_rightLegPivot, Mathf.Lerp(swing, 32f + (swing * 0.25f), _crouchBlend));

            float bob = Mathf.Abs(Mathf.Sin(_stepPhase * Mathf.PI)) * 0.045f * activity;
            _modelRoot.localPosition = Vector3.Lerp(
                _modelRoot.localPosition,
                _modelStartPosition + (Vector3.up * (bob - (0.22f * _crouchBlend))),
                1f - Mathf.Exp(-18f * Time.deltaTime));
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
