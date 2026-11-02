using System.Collections;
using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
using UnityEngine;

namespace HoldPointShowdown.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public sealed class WorldItemPickup : MonoBehaviour
    {
        [SerializeField] private SupplyKind _supplyKind;
        [SerializeField, Min(1)] private int _amount = 1;
        [SerializeField] private WeaponConfig _weapon;
        [SerializeField, Min(0f)] private float _respawnSeconds = 22f;
        [SerializeField] private GameObject _visualRoot;

        private SphereCollider _trigger;
        private bool _available = true;

        public bool IsWeapon => _weapon != null;
        public bool IsAvailable => _available;
        public WeaponConfig Weapon => _weapon;
        public SupplyKind SupplyKind => _supplyKind;

        public void ConfigureSupply(SupplyKind kind, int amount, float respawnSeconds, GameObject visualRoot)
        {
            _supplyKind = kind;
            _amount = Mathf.Max(1, amount);
            _weapon = null;
            _respawnSeconds = Mathf.Max(0f, respawnSeconds);
            _visualRoot = visualRoot;
            EnsureTrigger();
        }

        public void ConfigureWeapon(WeaponConfig weapon, float respawnSeconds, GameObject visualRoot)
        {
            _weapon = weapon;
            _amount = 1;
            _respawnSeconds = Mathf.Max(0f, respawnSeconds);
            _visualRoot = visualRoot;
            EnsureTrigger();
        }

        public bool TryCollect(PrototypeInventory inventory)
        {
            if (!_available || inventory == null)
            {
                return false;
            }

            bool collected = IsWeapon
                ? inventory.TryAddWeapon(_weapon)
                : inventory.TryAdd(_supplyKind, _amount);
            if (!collected)
            {
                return false;
            }

            if (isActiveAndEnabled)
            {
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                SetAvailable(false);
            }

            return true;
        }

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            PrototypeInventory inventory = other.GetComponentInParent<PrototypeInventory>();
            TryCollect(inventory);
        }

        private IEnumerator RespawnRoutine()
        {
            SetAvailable(false);
            if (_respawnSeconds <= 0f)
            {
                yield break;
            }

            yield return new WaitForSeconds(_respawnSeconds);
            SetAvailable(true);
        }

        private void SetAvailable(bool available)
        {
            _available = available;
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(available);
            }

            if (_trigger != null)
            {
                _trigger.enabled = available;
            }
        }

        private void EnsureTrigger()
        {
            _trigger = GetComponent<SphereCollider>();
            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<SphereCollider>();
            }

            _trigger.isTrigger = true;
            _trigger.radius = 1.35f;
        }
    }
}
