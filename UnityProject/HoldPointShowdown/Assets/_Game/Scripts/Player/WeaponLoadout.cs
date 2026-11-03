using System;
using System.Collections.Generic;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Data;
using HoldPointShowdown.Gameplay;
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

        public int WeaponCount => _slots.Count;
        public int MaxWeapons => _maxWeapons;
        public int ActiveIndex => _activeIndex;
        public WeaponConfig ActiveWeapon => _activeIndex >= 0 && _activeIndex < _slots.Count ? _slots[_activeIndex].Config : null;
        public event Action<WeaponConfig> ActiveWeaponChanged;

        public void Configure(PrototypeHitscanWeapon weapon, int maxWeapons, params WeaponConfig[] startingWeapons)
        {
            _weapon = weapon;
            _maxWeapons = Mathf.Max(1, maxWeapons);
            _slots.Clear();
            _activeIndex = -1;

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
            _slots.RemoveAt(droppedIndex);
            _activeIndex = -1;
            EquipIndex(Mathf.Clamp(droppedIndex, 0, _slots.Count - 1));
            SpawnDroppedWeapon(droppedSlot.Config, droppedSlot.Magazine, droppedSlot.Reserve);
            return true;
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
            if (_slots.Count == 0 && _weapon != null && _weapon.Config != null)
            {
                AddSlot(_weapon.Config);
            }

            if (_slots.Count > 0 && (_activeIndex < 0 || _activeIndex >= _slots.Count))
            {
                _activeIndex = -1;
                EquipIndex(0);
            }
        }

        private void SpawnDroppedWeapon(WeaponConfig config, int magazineAmmo, int reserveAmmo)
        {
            if (config == null || config.ModelPrefab == null)
            {
                return;
            }

            GameObject root = new($"掉落 · {config.DisplayName}");
            root.transform.position = transform.position + (transform.forward * 1.4f) + (Vector3.up * 0.75f);
            GameObject visual = Instantiate(config.ModelPrefab, root.transform);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            visual.transform.localScale *= 1.15f;
            visual.AddComponent<ShowcasePropMotion>().Configure(48f, 0.14f, 1.15f);
            WorldItemPickup pickup = root.AddComponent<WorldItemPickup>();
            pickup.ConfigureDroppedWeapon(config, magazineAmmo, reserveAmmo, visual);
        }

        private void Update()
        {
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
    }
}
