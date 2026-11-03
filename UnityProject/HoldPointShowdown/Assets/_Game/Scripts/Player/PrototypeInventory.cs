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
        private SupplyKind? _usingSupply;
        private float _useCompletesAt;

        public int AmmoPacks => _ammoPacks;
        public int Medkits => _medkits;
        public int ArmorPlates => _armorPlates;
        public int Capacity => _capacity;
        public int WeaponSlotsUsed => _loadout != null
            ? _loadout.CalculateSlotCost(_balance)
            : 0;
        public int UsedSlots => GetSupplySlots(SupplyKind.AmmoPack, _ammoPacks)
            + GetSupplySlots(SupplyKind.Medkit, _medkits)
            + GetSupplySlots(SupplyKind.ArmorPlate, _armorPlates)
            + WeaponSlotsUsed;
        public int FreeSlots => Mathf.Max(0, _capacity - UsedSlots);
        public WeaponLoadout Loadout => _loadout;
        public bool IsUsingConsumable => _usingSupply.HasValue;
        public bool CanUseWeapon => CanInteract && !_usingSupply.HasValue;
        public float ConsumableMoveSpeedLimit => _usingSupply switch
        {
            SupplyKind.Medkit => 0f,
            SupplyKind.ArmorPlate => _balance != null ? _balance.ArmorPlateMoveSpeed : 2f,
            _ => float.PositiveInfinity
        };
        public bool CanInteract => (_damageable == null || _damageable.IsAlive)
            && (_lifecycle == null || _lifecycle.CanAct);

        public void Configure(PrototypeHitscanWeapon weapon, Damageable damageable, int capacity = 8)
        {
            RebindDamageable(damageable);
            _weapon = weapon;
            _capacity = Mathf.Max(1, capacity);
            _lifecycle = GetComponent<CombatantLifecycle>();
        }

        public void Configure(
            PrototypeHitscanWeapon weapon,
            Damageable damageable,
            WeaponLoadout loadout,
            GameBalanceConfig balance)
        {
            RebindDamageable(damageable);
            _weapon = weapon;
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

            int slotCost = _balance != null ? _balance.GetWeaponSlotCost(config) : 3;
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
            if (amount <= 0 || GetUsedSlotsWith(kind, GetCount(kind) + amount) > _capacity)
            {
                return false;
            }

            SetCount(kind, GetCount(kind) + amount);
            return true;
        }

        public int TryAddUpTo(SupplyKind kind, int amount)
        {
            int added = 0;
            int requested = Mathf.Max(0, amount);
            while (added < requested && GetUsedSlotsWith(kind, GetCount(kind) + added + 1) <= _capacity)
            {
                added++;
            }
            if (added == 0)
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
            if (!CanInteract || _usingSupply.HasValue || GetCount(kind) <= 0)
            {
                return false;
            }

            if (kind != SupplyKind.AmmoPack)
            {
                if ((kind == SupplyKind.Medkit && (_damageable == null || _damageable.CurrentHealth >= _damageable.MaxHealth))
                    || (kind == SupplyKind.ArmorPlate && (_damageable == null || _damageable.CurrentArmor >= _damageable.MaxArmor)))
                {
                    return false;
                }
                float duration = kind == SupplyKind.Medkit
                    ? (_balance != null ? _balance.MedkitUseSeconds : 3f)
                    : (_balance != null ? _balance.ArmorPlateUseSeconds : 1.5f);
                _usingSupply = kind;
                _useCompletesAt = Time.time + duration;
                return true;
            }

            bool used = kind switch
            {
                SupplyKind.AmmoPack => AddActiveAmmo() > 0,
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
            CancelConsumableUse();
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
            if (_usingSupply.HasValue && Time.time >= _useCompletesAt)
            {
                CompleteConsumableUse();
            }
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

        private void OnEnable()
        {
            if (_damageable != null)
            {
                _damageable.Damaged += HandleDamaged;
            }
        }

        private void RebindDamageable(Damageable damageable)
        {
            if (_damageable == damageable)
            {
                return;
            }
            if (isActiveAndEnabled && _damageable != null)
            {
                _damageable.Damaged -= HandleDamaged;
            }
            _damageable = damageable;
            if (isActiveAndEnabled && _damageable != null)
            {
                _damageable.Damaged -= HandleDamaged;
                _damageable.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_damageable != null)
            {
                _damageable.Damaged -= HandleDamaged;
            }
            CancelConsumableUse();
        }

        private void HandleDamaged(Damageable _, float __, Vector3 ___, GameObject ____)
        {
            if (_usingSupply == SupplyKind.Medkit)
            {
                CancelConsumableUse();
            }
        }

        private void CompleteConsumableUse()
        {
            SupplyKind? pending = _usingSupply;
            _usingSupply = null;
            _useCompletesAt = 0f;
            if (!pending.HasValue || GetCount(pending.Value) <= 0 || _damageable == null || !_damageable.IsAlive)
            {
                return;
            }

            float restored = pending.Value == SupplyKind.Medkit
                ? _damageable.RestoreHealth(_balance != null ? _balance.MedkitHealing : 40f)
                : _damageable.RestoreArmor(_balance != null ? _balance.ArmorPlateRestore : 20f);
            if (restored > 0f)
            {
                TryRemove(pending.Value);
            }
        }

        private void CancelConsumableUse()
        {
            _usingSupply = null;
            _useCompletesAt = 0f;
        }

        private int GetSupplySlots(SupplyKind kind, int count)
        {
            int stack = _balance != null ? _balance.GetStackSize(kind) : 1;
            return count <= 0 ? 0 : Mathf.CeilToInt(count / (float)Mathf.Max(1, stack));
        }

        private int GetUsedSlotsWith(SupplyKind changedKind, int changedCount)
        {
            int ammo = changedKind == SupplyKind.AmmoPack ? changedCount : _ammoPacks;
            int medkits = changedKind == SupplyKind.Medkit ? changedCount : _medkits;
            int armor = changedKind == SupplyKind.ArmorPlate ? changedCount : _armorPlates;
            return WeaponSlotsUsed
                + GetSupplySlots(SupplyKind.AmmoPack, ammo)
                + GetSupplySlots(SupplyKind.Medkit, medkits)
                + GetSupplySlots(SupplyKind.ArmorPlate, armor);
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

        private void DropSupplyStack(
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
            float protection = _balance != null ? _balance.DropProtectionSeconds : 0.6f;
            float lifetime = _balance != null ? _balance.DroppedItemLifetime : 90f;
            WorldItemPickup.CreateSupplyDrop(kind, amount, deathPosition + offset, protection, lifetime);
            dropIndex++;
        }
    }
}
