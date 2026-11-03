using HoldPointShowdown.Combat;
using HoldPointShowdown.Data;
using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EquippedWeaponView : MonoBehaviour
    {
        [SerializeField] private PrototypeHitscanWeapon _weapon;
        [SerializeField] private Transform _socket;
        [SerializeField] private PrototypeWeaponEffects _effects;
        private GameObject _instance;

        public Transform CurrentMuzzle { get; private set; }

        public void Configure(PrototypeHitscanWeapon weapon, Transform socket, PrototypeWeaponEffects effects)
        {
            Unsubscribe();
            _weapon = weapon;
            _socket = socket;
            _effects = effects;
            Subscribe();
            RefreshModel(_weapon != null ? _weapon.Config : null);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void RefreshModel(WeaponConfig config)
        {
            if (_instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_instance);
                }
                else
                {
                    DestroyImmediate(_instance);
                }
            }

            CurrentMuzzle = _socket;
            if (config != null && config.ModelPrefab != null && _socket != null)
            {
                _instance = Instantiate(config.ModelPrefab, _socket);
                _instance.name = config.DisplayName;
                _instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                WeaponModelBinding binding = _instance.GetComponent<WeaponModelBinding>();
                if (binding != null && binding.Muzzle != null)
                {
                    CurrentMuzzle = binding.Muzzle;
                }
            }

            _effects?.SetMuzzle(CurrentMuzzle);
        }

        private void Subscribe()
        {
            if (_weapon != null)
            {
                _weapon.ConfigChanged -= RefreshModel;
                _weapon.ConfigChanged += RefreshModel;
            }
        }

        private void Unsubscribe()
        {
            if (_weapon != null)
            {
                _weapon.ConfigChanged -= RefreshModel;
            }
        }
    }
}
