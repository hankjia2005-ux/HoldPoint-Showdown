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
        [SerializeField] private int _weaponMagazine = -1;
        [SerializeField] private int _weaponReserve = -1;

        private SphereCollider _trigger;
        private bool _available = true;
        private float _collectableAt;

        public bool IsWeapon => _weapon != null;
        public bool IsAvailable => _available;
        public WeaponConfig Weapon => _weapon;
        public SupplyKind SupplyKind => _supplyKind;

        public void ConfigureSupply(SupplyKind kind, int amount, float respawnSeconds, GameObject visualRoot)
        {
            _supplyKind = kind;
            _amount = Mathf.Max(1, amount);
            _weapon = null;
            _weaponMagazine = -1;
            _weaponReserve = -1;
            _respawnSeconds = Mathf.Max(0f, respawnSeconds);
            _visualRoot = visualRoot;
            _collectableAt = 0f;
            EnsureTrigger();
        }

        public void ConfigureWeapon(WeaponConfig weapon, float respawnSeconds, GameObject visualRoot)
        {
            _weapon = weapon;
            _amount = 1;
            _weaponMagazine = -1;
            _weaponReserve = -1;
            _respawnSeconds = Mathf.Max(0f, respawnSeconds);
            _visualRoot = visualRoot;
            _collectableAt = 0f;
            EnsureTrigger();
        }

        public void ConfigureDroppedWeapon(
            WeaponConfig weapon,
            int magazineAmmo,
            int reserveAmmo,
            GameObject visualRoot,
            float pickupDelay = 0.8f)
        {
            _weapon = weapon;
            _amount = 1;
            _weaponMagazine = Mathf.Max(0, magazineAmmo);
            _weaponReserve = Mathf.Max(0, reserveAmmo);
            _respawnSeconds = 0f;
            _visualRoot = visualRoot;
            _collectableAt = Time.time + Mathf.Max(0f, pickupDelay);
            EnsureTrigger();
        }

        public bool TryCollect(PrototypeInventory inventory)
        {
            if (!_available || inventory == null || Time.time < _collectableAt)
            {
                return false;
            }

            bool collected = IsWeapon
                ? inventory.TryAddWeapon(_weapon, _weaponMagazine, _weaponReserve)
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
                Destroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(_respawnSeconds);
            _collectableAt = 0f;
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
