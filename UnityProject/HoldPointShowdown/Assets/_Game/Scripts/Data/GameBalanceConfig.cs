using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "HoldPoint Showdown/Game Balance Config")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Economy")]
        [SerializeField, Min(1)] private int _backpackSlots = 12;
        [SerializeField, Min(1)] private int _weaponSlotCost = 3;
        [SerializeField, Min(1)] private int _maxCarriedWeapons = 2;

        [Header("Consumables")]
        [SerializeField, Min(1)] private int _ammoPackAmount = 30;
        [SerializeField, Min(1f)] private float _medkitHealing = 40f;
        [SerializeField, Min(1f)] private float _armorPlateRestore = 30f;

        [Header("World Items")]
        [SerializeField, Min(0f)] private float _pickupRespawnSeconds = 22f;
        [SerializeField, Min(0.5f)] private float _pickupRadius = 1.35f;

        public int BackpackSlots => _backpackSlots;
        public int WeaponSlotCost => _weaponSlotCost;
        public int MaxCarriedWeapons => _maxCarriedWeapons;
        public int AmmoPackAmount => _ammoPackAmount;
        public float MedkitHealing => _medkitHealing;
        public float ArmorPlateRestore => _armorPlateRestore;
        public float PickupRespawnSeconds => _pickupRespawnSeconds;
        public float PickupRadius => _pickupRadius;

        public void SetDemoValues(
            int backpackSlots = 12,
            int weaponSlotCost = 3,
            int maxCarriedWeapons = 2,
            int ammoPackAmount = 30,
            float medkitHealing = 40f,
            float armorPlateRestore = 30f,
            float pickupRespawnSeconds = 22f,
            float pickupRadius = 1.35f)
        {
            _backpackSlots = Mathf.Max(1, backpackSlots);
            _weaponSlotCost = Mathf.Max(1, weaponSlotCost);
            _maxCarriedWeapons = Mathf.Max(1, maxCarriedWeapons);
            _ammoPackAmount = Mathf.Max(1, ammoPackAmount);
            _medkitHealing = Mathf.Max(1f, medkitHealing);
            _armorPlateRestore = Mathf.Max(1f, armorPlateRestore);
            _pickupRespawnSeconds = Mathf.Max(0f, pickupRespawnSeconds);
            _pickupRadius = Mathf.Max(0.5f, pickupRadius);
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
                _pickupRadius);
        }
    }
}
