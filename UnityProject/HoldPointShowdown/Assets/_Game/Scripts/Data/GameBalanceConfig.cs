using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "HoldPoint Showdown/Game Balance Config")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Combat")]
        [SerializeField, Min(1f)] private float _playerMaxHealth = 100f;
        [SerializeField, Min(0f)] private float _playerMaxArmor = 50f;
        [SerializeField, Min(0f)] private float _playerStartingArmor;
        [SerializeField, Range(0f, 0.9f)] private float _armorAbsorptionFraction = 0.25f;

        [Header("Player Economy")]
        [SerializeField, Min(1)] private int _backpackSlots = 12;
        [SerializeField, Min(1)] private int _weaponSlotCost = 3;
        [SerializeField, Min(1)] private int _maxCarriedWeapons = 2;

        [Header("Consumables")]
        [SerializeField, Min(1)] private int _ammoPackAmount = 18;
        [SerializeField, Min(1f)] private float _medkitHealing = 40f;
        [SerializeField, Min(1f)] private float _armorPlateRestore = 30f;

        [Header("World Items")]
        [SerializeField, Min(0f)] private float _pickupRespawnSeconds = 22f;
        [SerializeField, Min(0.5f)] private float _pickupRadius = 1.35f;

        [Header("Enemy AI")]
        [SerializeField, Min(1f)] private float _aiDetectionRange = 72f;
        [SerializeField, Min(1f)] private float _aiAttackRange = 42f;
        [SerializeField, Range(0f, 12f)] private float _aiMinimumSpread = 1.4f;
        [SerializeField, Range(0f, 18f)] private float _aiMaximumSpread = 6.5f;
        [SerializeField, Range(0.1f, 1f)] private float _aiDamageScale = 0.75f;

        public float PlayerMaxHealth => _playerMaxHealth;
        public float PlayerMaxArmor => _playerMaxArmor;
        public float PlayerStartingArmor => _playerStartingArmor;
        public float ArmorAbsorptionFraction => _armorAbsorptionFraction;
        public int BackpackSlots => _backpackSlots;
        public int WeaponSlotCost => _weaponSlotCost;
        public int MaxCarriedWeapons => _maxCarriedWeapons;
        public int AmmoPackAmount => _ammoPackAmount;
        public float MedkitHealing => _medkitHealing;
        public float ArmorPlateRestore => _armorPlateRestore;
        public float PickupRespawnSeconds => _pickupRespawnSeconds;
        public float PickupRadius => _pickupRadius;
        public float AiDetectionRange => _aiDetectionRange;
        public float AiAttackRange => _aiAttackRange;
        public float AiMinimumSpread => _aiMinimumSpread;
        public float AiMaximumSpread => _aiMaximumSpread;
        public float AiDamageScale => _aiDamageScale;

        public void SetDemoValues(
            int backpackSlots = 12,
            int weaponSlotCost = 3,
            int maxCarriedWeapons = 2,
            int ammoPackAmount = 18,
            float medkitHealing = 40f,
            float armorPlateRestore = 30f,
            float pickupRespawnSeconds = 22f,
            float pickupRadius = 1.35f,
            float playerMaxHealth = 100f,
            float playerMaxArmor = 50f,
            float playerStartingArmor = 0f,
            float armorAbsorptionFraction = 0.25f,
            float aiDetectionRange = 72f,
            float aiAttackRange = 42f,
            float aiMinimumSpread = 1.4f,
            float aiMaximumSpread = 6.5f,
            float aiDamageScale = 0.75f)
        {
            _backpackSlots = Mathf.Max(1, backpackSlots);
            _weaponSlotCost = Mathf.Max(1, weaponSlotCost);
            _maxCarriedWeapons = Mathf.Max(1, maxCarriedWeapons);
            _ammoPackAmount = Mathf.Max(1, ammoPackAmount);
            _medkitHealing = Mathf.Max(1f, medkitHealing);
            _armorPlateRestore = Mathf.Max(1f, armorPlateRestore);
            _pickupRespawnSeconds = Mathf.Max(0f, pickupRespawnSeconds);
            _pickupRadius = Mathf.Max(0.5f, pickupRadius);
            _playerMaxHealth = Mathf.Max(1f, playerMaxHealth);
            _playerMaxArmor = Mathf.Max(0f, playerMaxArmor);
            _playerStartingArmor = Mathf.Clamp(playerStartingArmor, 0f, _playerMaxArmor);
            _armorAbsorptionFraction = Mathf.Clamp(armorAbsorptionFraction, 0f, 0.9f);
            _aiDetectionRange = Mathf.Max(1f, aiDetectionRange);
            _aiAttackRange = Mathf.Clamp(aiAttackRange, 1f, _aiDetectionRange);
            _aiMinimumSpread = Mathf.Clamp(aiMinimumSpread, 0f, 12f);
            _aiMaximumSpread = Mathf.Clamp(aiMaximumSpread, _aiMinimumSpread, 18f);
            _aiDamageScale = Mathf.Clamp(aiDamageScale, 0.1f, 1f);
        }

        private void OnValidate()
        {
            SetDemoValues(
                _backpackSlots,
                _weaponSlotCost,
                _maxCarriedWeapons,
                _ammoPackAmount,
                _medkitHealing,
                _armorPlateRestore,
                _pickupRespawnSeconds,
                _pickupRadius,
                _playerMaxHealth,
                _playerMaxArmor,
                _playerStartingArmor,
                _armorAbsorptionFraction,
                _aiDetectionRange,
                _aiAttackRange,
                _aiMinimumSpread,
                _aiMaximumSpread,
                _aiDamageScale);
        }
    }
}
