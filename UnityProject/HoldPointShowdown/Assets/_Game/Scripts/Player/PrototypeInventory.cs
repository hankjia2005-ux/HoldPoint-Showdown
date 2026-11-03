using HoldPointShowdown.Combat;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.Player
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventory : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _ammoPacks;
        [SerializeField, Min(0)] private int _medkits;
        [SerializeField, Min(0)] private int _armorPlates;
        [SerializeField, Min(1)] private int _capacity = 8;

        [SerializeField] private PrototypeHitscanWeapon _weapon;
        [SerializeField] private Damageable _damageable;
        [SerializeField] private WeaponLoadout _loadout;
        [SerializeField] private GameBalanceConfig _balance;
        private CombatantLifecycle _lifecycle;

        public int AmmoPacks => _ammoPacks;
        public int Medkits => _medkits;
        public int ArmorPlates => _armorPlates;
        public int Capacity => _capacity;
        public int WeaponSlotsUsed => _loadout != null && _balance != null
            ? _loadout.WeaponCount * _balance.WeaponSlotCost
            : 0;
        public int UsedSlots => _ammoPacks + _medkits + _armorPlates + WeaponSlotsUsed;
        public int FreeSlots => Mathf.Max(0, _capacity - UsedSlots);
        public WeaponLoadout Loadout => _loadout;
        public bool CanInteract => (_damageable == null || _damageable.IsAlive)
            && (_lifecycle == null || _lifecycle.CanAct);

        public void Configure(PrototypeHitscanWeapon weapon, Damageable damageable, int capacity = 8)
        {
            _weapon = weapon;
            _damageable = damageable;
            _capacity = Mathf.Max(1, capacity);
            _lifecycle = GetComponent<CombatantLifecycle>();
        }

        public void Configure(
            PrototypeHitscanWeapon weapon,
            Damageable damageable,
            WeaponLoadout loadout,
            GameBalanceConfig balance)
        {
            _weapon = weapon;
            _damageable = damageable;
            _loadout = loadout;
            _balance = balance;
            _capacity = balance != null ? balance.BackpackSlots : 12;
            _lifecycle = GetComponent<CombatantLifecycle>();
        }

        public bool TryAddWeapon(WeaponConfig config)
        {
            return TryAddWeapon(config, -1, -1);
        }

        public bool TryAddWeapon(WeaponConfig config, int magazineAmmo, int reserveAmmo)
        {
            if (_loadout == null || config == null)
            {
                return false;
            }

            if (_loadout.Contains(config))
            {
                return _loadout.TryAddWeapon(config, true, magazineAmmo, reserveAmmo);
            }

            int slotCost = _balance != null ? _balance.WeaponSlotCost : 3;
            return FreeSlots >= slotCost && _loadout.TryAddWeapon(config, true, magazineAmmo, reserveAmmo);
        }

        public int GetCount(SupplyKind kind)
        {
            return kind switch
            {
                SupplyKind.AmmoPack => _ammoPacks,
                SupplyKind.Medkit => _medkits,
                SupplyKind.ArmorPlate => _armorPlates,
                _ => 0
            };
        }

        public bool TryAdd(SupplyKind kind, int amount = 1)
        {
            if (amount <= 0 || UsedSlots + amount > _capacity)
            {
                return false;
            }

            SetCount(kind, GetCount(kind) + amount);
            return true;
        }

        public int TryAddUpTo(SupplyKind kind, int amount)
        {
            int added = Mathf.Min(Mathf.Max(0, amount), FreeSlots);
            if (added <= 0)
            {
                return 0;
            }

            SetCount(kind, GetCount(kind) + added);
            return added;
        }

        public bool TryRemove(SupplyKind kind, int amount = 1)
        {
            if (amount <= 0 || GetCount(kind) < amount)
            {
                return false;
            }

            SetCount(kind, GetCount(kind) - amount);
            return true;
        }

        public bool TryUse(SupplyKind kind)
        {
            if (!CanInteract || GetCount(kind) <= 0)
            {
                return false;
            }

            bool used = kind switch
            {
                SupplyKind.AmmoPack => AddActiveAmmo() > 0,
                SupplyKind.Medkit => _damageable != null && _damageable.RestoreHealth(
                    _balance != null ? _balance.MedkitHealing : 40f) > 0f,
                SupplyKind.ArmorPlate => _damageable != null && _damageable.RestoreArmor(
                    _balance != null ? _balance.ArmorPlateRestore : 30f) > 0f,
                _ => false
            };

            if (used)
            {
                TryRemove(kind);
            }

            return used;
        }

        public void DropAllOnDeath(Vector3 deathPosition)
        {
            int dropIndex = 0;
            DropSupplyStack(SupplyKind.AmmoPack, _ammoPacks, deathPosition, ref dropIndex);
            DropSupplyStack(SupplyKind.Medkit, _medkits, deathPosition, ref dropIndex);
            DropSupplyStack(SupplyKind.ArmorPlate, _armorPlates, deathPosition, ref dropIndex);
            _ammoPacks = 0;
            _medkits = 0;
            _armorPlates = 0;
            _loadout?.DropAllLootWeapons(deathPosition, ref dropIndex);
        }

        private int AddActiveAmmo()
        {
            int fallback = _balance != null ? _balance.AmmoPackAmount : 30;
            if (_loadout != null)
            {
                return _loadout.AddAmmoForActiveWeapon(fallback);
            }

            return _weapon != null ? _weapon.AddReserveAmmo(fallback) : 0;
        }

        private void Update()
        {
            _lifecycle ??= GetComponent<CombatantLifecycle>();
            if (!CanInteract)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (MobileInputBridge.TryConsumeSupply(out SupplyKind mobileSupply))
            {
                TryUse(mobileSupply);
            }
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.zKey.wasPressedThisFrame)
            {
                TryUse(SupplyKind.AmmoPack);
            }
            else if (keyboard.xKey.wasPressedThisFrame)
            {
                TryUse(SupplyKind.Medkit);
            }
            else if (keyboard.cKey.wasPressedThisFrame)
            {
                TryUse(SupplyKind.ArmorPlate);
            }
        }

        private void SetCount(SupplyKind kind, int value)
        {
            value = Mathf.Max(0, value);
            switch (kind)
            {
                case SupplyKind.AmmoPack:
                    _ammoPacks = value;
                    break;
                case SupplyKind.Medkit:
                    _medkits = value;
                    break;
                case SupplyKind.ArmorPlate:
                    _armorPlates = value;
                    break;
            }
        }

        private static void DropSupplyStack(
            SupplyKind kind,
            int amount,
            Vector3 deathPosition,
            ref int dropIndex)
        {
            if (amount <= 0)
            {
                return;
            }

            float angle = dropIndex * 120f * Mathf.Deg2Rad;
            Vector3 offset = new(Mathf.Cos(angle) * 0.8f, 0.15f, Mathf.Sin(angle) * 0.8f);
            WorldItemPickup.CreateSupplyDrop(kind, amount, deathPosition + offset);
            dropIndex++;
        }
    }
}
