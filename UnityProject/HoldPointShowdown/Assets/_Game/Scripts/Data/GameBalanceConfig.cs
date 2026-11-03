using HoldPointShowdown.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "HoldPoint Showdown/Game Balance Config")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Combat")]
        [SerializeField, Min(1f)] private float _playerMaxHealth = 100f;
        [SerializeField, Min(0f)] private float _playerMaxArmor = 40f;
        [SerializeField, Min(0f)] private float _playerStartingArmor;
        [FormerlySerializedAs("_armorAbsorptionFraction")]
        [SerializeField, Range(0.1f, 1f)] private float _armorDamageRatio = 1f;
        [SerializeField, Min(1f)] private float _headMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float _torsoMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float _limbMultiplier = 0.8f;
        [SerializeField, Range(0.1f, 1f)] private float _falloffMid = 0.82f;
        [SerializeField, Range(0.1f, 1f)] private float _falloffFar = 0.65f;
        [SerializeField, Range(0.1f, 1f)] private float _falloffBeyond = 0.35f;

        [Header("Player Economy")]
        [SerializeField, Min(1)] private int _backpackSlots = 12;
        [SerializeField, Min(1)] private int _weaponSlotCost = 3;
        [SerializeField, Min(1)] private int _pistolSlotCost = 2;
        [SerializeField, Min(1)] private int _maxCarriedWeapons = 2;
        [SerializeField, Min(1)] private int _ammoStack = 3;
        [SerializeField, Min(1)] private int _medkitStack = 3;
        [SerializeField, Min(1)] private int _armorPlateStack = 3;

        [Header("Consumables")]
        [SerializeField, Min(1)] private int _ammoPackAmount = 24;
        [SerializeField, Min(1f)] private float _medkitHealing = 40f;
        [SerializeField, Min(0f)] private float _medkitUseSeconds = 3f;
        [SerializeField, Min(1f)] private float _armorPlateRestore = 20f;
        [SerializeField, Min(0f)] private float _armorPlateUseSeconds = 1.5f;
        [SerializeField, Min(0f)] private float _armorPlateMoveSpeed = 2f;

        [Header("World Items")]
        [SerializeField, Min(0.5f)] private float _pickupRadius = 1.35f;
        [SerializeField, Min(0f)] private float _dropProtectionSeconds = 0.6f;
        [SerializeField, Min(1f)] private float _droppedItemLifetime = 90f;
        [SerializeField, Min(1)] private int _chestCapAmmo = 8;
        [SerializeField, Min(1)] private int _chestCapMedkit = 4;
        [SerializeField, Min(1)] private int _chestCapArmor = 4;
        [SerializeField, Min(1)] private int _chestCapWeapon = 4;
        [SerializeField, Min(1f)] private float _baseAmmoResupplySeconds = 90f;
        [SerializeField, Min(1)] private int _baseAmmoResupplyCap = 2;

        [Header("Enemy AI Fallbacks")]
        [SerializeField, Min(1f)] private float _aiDetectionRange = 55f;
        [SerializeField, Min(1f)] private float _aiAttackRange = 45f;
        [SerializeField, Range(0f, 12f)] private float _aiMinimumSpread = 1.4f;
        [SerializeField, Range(0f, 18f)] private float _aiMaximumSpread = 6.5f;
        [SerializeField, Range(0.1f, 1.5f)] private float _aiDamageScale = 1f;

        public float PlayerMaxHealth => _playerMaxHealth;
        public float PlayerMaxArmor => _playerMaxArmor;
        public float PlayerStartingArmor => _playerStartingArmor;
        public float ArmorDamageRatio => _armorDamageRatio;
        public float HeadMultiplier => _headMultiplier;
        public float TorsoMultiplier => _torsoMultiplier;
        public float LimbMultiplier => _limbMultiplier;
        public float FalloffMid => _falloffMid;
        public float FalloffFar => _falloffFar;
        public float FalloffBeyond => _falloffBeyond;
        public int BackpackSlots => _backpackSlots;
        public int WeaponSlotCost => _weaponSlotCost;
        public int PistolSlotCost => _pistolSlotCost;
        public int MaxCarriedWeapons => _maxCarriedWeapons;
        public int AmmoStack => _ammoStack;
        public int MedkitStack => _medkitStack;
        public int ArmorPlateStack => _armorPlateStack;
        public int AmmoPackAmount => _ammoPackAmount;
        public float MedkitHealing => _medkitHealing;
        public float MedkitUseSeconds => _medkitUseSeconds;
        public float ArmorPlateRestore => _armorPlateRestore;
        public float ArmorPlateUseSeconds => _armorPlateUseSeconds;
        public float ArmorPlateMoveSpeed => _armorPlateMoveSpeed;
        public float PickupRadius => _pickupRadius;
        public float PickupRespawnSeconds => 0f;
        public float DropProtectionSeconds => _dropProtectionSeconds;
        public float DroppedItemLifetime => _droppedItemLifetime;
        public int ChestCapAmmo => _chestCapAmmo;
        public int ChestCapMedkit => _chestCapMedkit;
        public int ChestCapArmor => _chestCapArmor;
        public int ChestCapWeapon => _chestCapWeapon;
        public float BaseAmmoResupplySeconds => _baseAmmoResupplySeconds;
        public int BaseAmmoResupplyCap => _baseAmmoResupplyCap;
        public float AiDetectionRange => _aiDetectionRange;
        public float AiAttackRange => _aiAttackRange;
        public float AiMinimumSpread => _aiMinimumSpread;
        public float AiMaximumSpread => _aiMaximumSpread;
        public float AiDamageScale => _aiDamageScale;

        public int GetWeaponSlotCost(WeaponConfig weapon)
        {
            return weapon != null && weapon.Archetype == WeaponArchetype.Pistol
                ? _pistolSlotCost
                : _weaponSlotCost;
        }

        public int GetStackSize(SupplyKind kind)
        {
            return kind switch
            {
                SupplyKind.AmmoPack => _ammoStack,
                SupplyKind.Medkit => _medkitStack,
                SupplyKind.ArmorPlate => _armorPlateStack,
                _ => 1
            };
        }

        public int GetChestCapacity(SupplyKind kind)
        {
            return kind switch
            {
                SupplyKind.AmmoPack => _chestCapAmmo,
                SupplyKind.Medkit => _chestCapMedkit,
                SupplyKind.ArmorPlate => _chestCapArmor,
                _ => 0
            };
        }

        public void SetDemoValues(
            int backpackSlots = 12,
            int weaponSlotCost = 3,
            int maxCarriedWeapons = 2,
            int ammoPackAmount = 24,
            float medkitHealing = 40f,
            float armorPlateRestore = 20f,
            float pickupRespawnSeconds = 0f,
            float pickupRadius = 1.35f,
            float playerMaxHealth = 100f,
            float playerMaxArmor = 40f,
            float playerStartingArmor = 0f,
            float armorAbsorptionFraction = 1f,
            float aiDetectionRange = 55f,
            float aiAttackRange = 45f,
            float aiMinimumSpread = 1.4f,
            float aiMaximumSpread = 6.5f,
            float aiDamageScale = 1f)
        {
            _backpackSlots = Mathf.Max(1, backpackSlots);
            _weaponSlotCost = Mathf.Max(1, weaponSlotCost);
            _pistolSlotCost = 2;
            _maxCarriedWeapons = Mathf.Max(1, maxCarriedWeapons);
            _ammoStack = 3;
            _medkitStack = 3;
            _armorPlateStack = 3;
            _ammoPackAmount = Mathf.Max(1, ammoPackAmount);
            _medkitHealing = Mathf.Max(1f, medkitHealing);
            _medkitUseSeconds = 3f;
            _armorPlateRestore = Mathf.Max(1f, armorPlateRestore);
            _armorPlateUseSeconds = 1.5f;
            _armorPlateMoveSpeed = 2f;
            _pickupRadius = Mathf.Max(0.5f, pickupRadius);
            _dropProtectionSeconds = 0.6f;
            _droppedItemLifetime = 90f;
            _chestCapAmmo = 8;
            _chestCapMedkit = 4;
            _chestCapArmor = 4;
            _chestCapWeapon = 4;
            _baseAmmoResupplySeconds = 90f;
            _baseAmmoResupplyCap = 2;
            _playerMaxHealth = Mathf.Max(1f, playerMaxHealth);
            _playerMaxArmor = Mathf.Max(0f, playerMaxArmor);
            _playerStartingArmor = Mathf.Clamp(playerStartingArmor, 0f, _playerMaxArmor);
            _armorDamageRatio = Mathf.Clamp(armorAbsorptionFraction, 0.1f, 1f);
            _headMultiplier = 1.5f;
            _torsoMultiplier = 1f;
            _limbMultiplier = 0.8f;
            _falloffMid = 0.82f;
            _falloffFar = 0.65f;
            _falloffBeyond = 0.35f;
            _aiDetectionRange = Mathf.Max(1f, aiDetectionRange);
            _aiAttackRange = Mathf.Clamp(aiAttackRange, 1f, _aiDetectionRange);
            _aiMinimumSpread = Mathf.Clamp(aiMinimumSpread, 0f, 12f);
            _aiMaximumSpread = Mathf.Clamp(aiMaximumSpread, _aiMinimumSpread, 18f);
            _aiDamageScale = Mathf.Clamp(aiDamageScale, 0.1f, 1.5f);
        }

        private void OnValidate()
        {
            _backpackSlots = Mathf.Max(1, _backpackSlots);
            _weaponSlotCost = Mathf.Max(1, _weaponSlotCost);
            _pistolSlotCost = Mathf.Max(1, _pistolSlotCost);
            _maxCarriedWeapons = Mathf.Max(1, _maxCarriedWeapons);
            _ammoStack = Mathf.Max(1, _ammoStack);
            _medkitStack = Mathf.Max(1, _medkitStack);
            _armorPlateStack = Mathf.Max(1, _armorPlateStack);
            _ammoPackAmount = Mathf.Max(1, _ammoPackAmount);
            _medkitHealing = Mathf.Max(1f, _medkitHealing);
            _armorPlateRestore = Mathf.Max(1f, _armorPlateRestore);
            _pickupRadius = Mathf.Max(0.5f, _pickupRadius);
            _droppedItemLifetime = Mathf.Max(1f, _droppedItemLifetime);
            _playerMaxHealth = Mathf.Max(1f, _playerMaxHealth);
            _playerMaxArmor = Mathf.Max(0f, _playerMaxArmor);
            _playerStartingArmor = Mathf.Clamp(_playerStartingArmor, 0f, _playerMaxArmor);
            _armorDamageRatio = Mathf.Clamp(_armorDamageRatio, 0.1f, 1f);
            _aiDetectionRange = Mathf.Max(1f, _aiDetectionRange);
            _aiAttackRange = Mathf.Clamp(_aiAttackRange, 1f, _aiDetectionRange);
            _aiMaximumSpread = Mathf.Max(_aiMinimumSpread, _aiMaximumSpread);
        }
    }
}
