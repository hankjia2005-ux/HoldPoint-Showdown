using HoldPointShowdown.Combat;
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

        public int AmmoPacks => _ammoPacks;
        public int Medkits => _medkits;
        public int ArmorPlates => _armorPlates;
        public int Capacity => _capacity;
        public int UsedSlots => _ammoPacks + _medkits + _armorPlates;

        public void Configure(PrototypeHitscanWeapon weapon, Damageable damageable, int capacity = 8)
        {
            _weapon = weapon;
            _damageable = damageable;
            _capacity = Mathf.Max(1, capacity);
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
            if (GetCount(kind) <= 0)
            {
                return false;
            }

            bool used = kind switch
            {
                SupplyKind.AmmoPack => _weapon != null && _weapon.AddReserveAmmo(18) > 0,
                SupplyKind.Medkit => _damageable != null && _damageable.RestoreHealth(40f) > 0f,
                SupplyKind.ArmorPlate => _damageable != null && _damageable.RestoreArmor(30f) > 0f,
                _ => false
            };

            if (used)
            {
                TryRemove(kind);
            }

            return used;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
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
    }
}
