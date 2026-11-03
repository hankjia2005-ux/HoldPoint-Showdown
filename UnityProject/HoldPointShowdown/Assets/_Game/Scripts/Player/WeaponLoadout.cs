using System;
using System.Collections.Generic;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Data;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Player
{
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [Serializable]
        private sealed class WeaponSlot
        {
            public WeaponConfig Config;
            public int Magazine;
            public int Reserve;
        }

        [SerializeField] private PrototypeHitscanWeapon _weapon;
        [SerializeField, Min(1)] private int _maxWeapons = 2;
        [SerializeField] private List<WeaponSlot> _slots = new();
        [SerializeField] private int _activeIndex = -1;
        [SerializeField] private WeaponConfig _starterWeapon;
        private Damageable _damageable;
        private CombatantLifecycle _lifecycle;

        public int WeaponCount => _slots.Count;
        public int MaxWeapons => _maxWeapons;
        public int ActiveIndex => _activeIndex;
        public WeaponConfig ActiveWeapon => _activeIndex >= 0 && _activeIndex < _slots.Count ? _slots[_activeIndex].Config : null;
        public event Action<WeaponConfig> ActiveWeaponChanged;

        public bool TryRemoveActiveForStorage(
            out WeaponConfig config,
            out int magazineAmmo,
            out int reserveAmmo)
        {
            config = null;
            magazineAmmo = 0;
            reserveAmmo = 0;
            if (_activeIndex < 0 || _activeIndex >= _slots.Count || _slots.Count <= 1)
            {
                return false;
            }

            SaveActiveAmmo();
            int storedIndex = _activeIndex;
            WeaponSlot storedSlot = _slots[storedIndex];
            if (storedSlot.Config == null || IsStarter(storedSlot.Config))
            {
                return false;
            }

            config = storedSlot.Config;
            magazineAmmo = storedSlot.Magazine;
            reserveAmmo = storedSlot.Reserve;
            _slots.RemoveAt(storedIndex);
            _activeIndex = -1;
            EquipIndex(Mathf.Clamp(storedIndex, 0, _slots.Count - 1));
            return true;
        }

        public void Configure(PrototypeHitscanWeapon weapon, int maxWeapons, params WeaponConfig[] startingWeapons)
        {
            _weapon = weapon;
            _maxWeapons = Mathf.Max(1, maxWeapons);
            _slots.Clear();
            _activeIndex = -1;
            _starterWeapon = startingWeapons != null && startingWeapons.Length > 0 ? startingWeapons[0] : null;

            if (startingWeapons != null)
            {
                foreach (WeaponConfig startingWeapon in startingWeapons)
                {
                    if (startingWeapon != null && _slots.Count < _maxWeapons)
                    {
                        AddSlot(startingWeapon);
                    }
                }
            }

            if (_slots.Count > 0)
            {
                EquipIndex(0);
            }
        }

        public bool Contains(WeaponConfig config)
        {
            return config != null && _slots.Exists(slot => slot.Config == config || slot.Config.ItemId == config.ItemId);
        }

        public bool TryAddWeapon(
            WeaponConfig config,
            bool autoEquip = true,
            int magazineAmmo = -1,
            int reserveAmmo = -1)
        {
            if (config == null)
            {
                return false;
            }

            int existing = _slots.FindIndex(slot => slot.Config == config || slot.Config.ItemId == config.ItemId);
            if (existing >= 0)
            {
                int ammoToAdd = magazineAmmo >= 0 || reserveAmmo >= 0
                    ? Mathf.Max(0, magazineAmmo) + Mathf.Max(0, reserveAmmo)
                    : config.AmmoPerPickup;
                _slots[existing].Reserve += ammoToAdd;
                if (existing == _activeIndex && _weapon != null)
                {
                    _weapon.AddReserveAmmo(ammoToAdd);
                }

                return true;
            }

            if (_slots.Count >= _maxWeapons)
            {
                return false;
            }

            AddSlot(config, magazineAmmo, reserveAmmo);
            if (autoEquip || _activeIndex < 0)
            {
                EquipIndex(_slots.Count - 1);
            }

            return true;
        }

        public bool TryDropActive()
        {
            if (_activeIndex < 0 || _activeIndex >= _slots.Count || _slots.Count <= 1)
            {
                return false;
            }

            SaveActiveAmmo();
            int droppedIndex = _activeIndex;
            WeaponSlot droppedSlot = _slots[droppedIndex];
            if (IsStarter(droppedSlot.Config))
            {
                return false;
            }

            _slots.RemoveAt(droppedIndex);
            _activeIndex = -1;
            EquipIndex(Mathf.Clamp(droppedIndex, 0, _slots.Count - 1));
            SpawnDroppedWeapon(
                droppedSlot.Config,
                droppedSlot.Magazine,
                droppedSlot.Reserve,
                transform.position + (transform.forward * 1.4f) + (Vector3.up * 0.75f),
                0.8f);
            return true;
        }

        public void DropAllLootWeapons(Vector3 deathPosition, ref int dropIndex)
        {
            SaveActiveAmmo();
            foreach (WeaponSlot slot in _slots)
            {
                if (slot.Config == null || IsStarter(slot.Config))
                {
                    continue;
                }

                float angle = (dropIndex * 57f) * Mathf.Deg2Rad;
                Vector3 offset = new(Mathf.Cos(angle) * 1.1f, 0.75f, Mathf.Sin(angle) * 1.1f);
                SpawnDroppedWeapon(slot.Config, slot.Magazine, slot.Reserve, deathPosition + offset, 0.6f);
                dropIndex++;
            }

            ResetToStarter();
        }

        public void ResetToStarter()
        {
            WeaponConfig starter = _starterWeapon;
            if (starter == null && _slots.Count > 0)
            {
                starter = _slots[0].Config;
                _starterWeapon = starter;
            }

            if (starter == null || _weapon == null)
            {
                return;
            }

            _slots.Clear();
            _activeIndex = -1;
            AddSlot(starter);
            EquipIndex(0);
        }

        public bool EquipIndex(int index)
        {
            if (_weapon == null || index < 0 || index >= _slots.Count || index == _activeIndex)
            {
                return false;
            }

            SaveActiveAmmo();
            _activeIndex = index;
            WeaponSlot slot = _slots[index];
            _weapon.Equip(slot.Config, slot.Magazine, slot.Reserve);
            ActiveWeaponChanged?.Invoke(slot.Config);
            return true;
        }

        public int AddAmmoForActiveWeapon(int fallbackAmount)
        {
            if (_weapon == null || ActiveWeapon == null)
            {
                return 0;
            }

            int amount = ActiveWeapon.AmmoPerPickup > 0 ? ActiveWeapon.AmmoPerPickup : fallbackAmount;
            int added = _weapon.AddReserveAmmo(amount);
            SaveActiveAmmo();
            return added;
        }

        private void AddSlot(WeaponConfig config, int magazineAmmo = -1, int reserveAmmo = -1)
        {
            _slots.Add(new WeaponSlot
            {
                Config = config,
                Magazine = magazineAmmo >= 0 ? Mathf.Clamp(magazineAmmo, 0, config.MagazineSize) : config.MagazineSize,
                Reserve = reserveAmmo >= 0 ? reserveAmmo : config.StartingReserveAmmo
            });
        }

        private void SaveActiveAmmo()
        {
            if (_weapon == null || _activeIndex < 0 || _activeIndex >= _slots.Count)
            {
                return;
            }

            _slots[_activeIndex].Magazine = _weapon.MagazineAmmo;
            _slots[_activeIndex].Reserve = _weapon.ReserveAmmo;
        }

        private void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _lifecycle = GetComponent<CombatantLifecycle>();
            if (_slots.Count == 0 && _weapon != null && _weapon.Config != null)
            {
                AddSlot(_weapon.Config);
                _starterWeapon ??= _weapon.Config;
            }

            if (_slots.Count > 0 && (_activeIndex < 0 || _activeIndex >= _slots.Count))
            {
                _activeIndex = -1;
                EquipIndex(0);
            }
        }

        private void SpawnDroppedWeapon(
            WeaponConfig config,
            int magazineAmmo,
            int reserveAmmo,
            Vector3 position,
            float pickupDelay)
        {
            WorldItemPickup.CreateWeaponDrop(config, magazineAmmo, reserveAmmo, position, pickupDelay, 60f);
        }

        private void Update()
        {
            _lifecycle ??= GetComponent<CombatantLifecycle>();
            if ((_damageable != null && !_damageable.IsAlive) || (_lifecycle != null && !_lifecycle.CanAct))
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                EquipIndex(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                EquipIndex(1);
            }
            else if (keyboard.gKey.wasPressedThisFrame)
            {
                TryDropActive();
            }
        }

        private bool IsStarter(WeaponConfig config)
        {
            return config != null && _starterWeapon != null
                && (config == _starterWeapon || config.ItemId == _starterWeapon.ItemId);
        }
    }
}
