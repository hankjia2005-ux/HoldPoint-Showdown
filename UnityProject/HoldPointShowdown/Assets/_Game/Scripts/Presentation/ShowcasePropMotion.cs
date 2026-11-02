using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ShowcasePropMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField, Min(0f)] private float _bobHeight = 0.2f;
        [SerializeField, Min(0f)] private float _bobFrequency = 1.5f;

        private Vector3 _startLocalPosition;
        private float _phaseOffset;

        public void Configure(float rotationSpeed, float bobHeight, float bobFrequency)
        {
            _rotationSpeed = rotationSpeed;
            _bobHeight = Mathf.Max(0f, bobHeight);
            _bobFrequency = Mathf.Max(0f, bobFrequency);
        }

        private void Awake()
        {
            _startLocalPosition = transform.localPosition;
            _phaseOffset = Mathf.Abs(transform.position.x + transform.position.z) * 0.17f;
        }

        private void Update()
        {
            transform.Rotate(_rotationAxis, _rotationSpeed * Time.deltaTime, Space.Self);

            float bob = Mathf.Sin((Time.time + _phaseOffset) * _bobFrequency * Mathf.PI * 2f) * _bobHeight;
            transform.localPosition = _startLocalPosition + (Vector3.up * bob);
        }
    }
}
