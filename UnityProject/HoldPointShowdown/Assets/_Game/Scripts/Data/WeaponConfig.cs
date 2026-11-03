using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "HoldPoint Showdown/Weapon Config")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _itemId = "weapon.pistol.common";
        [SerializeField] private string _displayName = "白色基础手枪";
        [SerializeField] private WeaponArchetype _archetype = WeaponArchetype.Pistol;
        [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
        [SerializeField] private AmmoKind _ammoKind = AmmoKind.Light;

        [Header("Ballistics")]
        [SerializeField, Min(1f)] private float _damage = 20f;
        [SerializeField, Min(0.1f)] private float _shotsPerSecond = 4f;
        [SerializeField, Min(1)] private int _pellets = 1;
        [SerializeField, Range(0f, 12f)] private float _spreadDegrees = 0.8f;
        [SerializeField, Min(1f)] private float _fullDamageRange = 15f;
        [SerializeField, Min(1f)] private float _midDamageRange = 45f;
        [SerializeField, Min(1f)] private float _farDamageRange = 70f;
        [SerializeField, Min(1f)] private float _range = 84f;
        [SerializeField, Range(0.1f, 1f)] private float _midDamageMultiplier = 0.82f;
        [SerializeField, Range(0.1f, 1f)] private float _farDamageMultiplier = 0.65f;
        [SerializeField, Range(0.1f, 1f)] private float _beyondDamageMultiplier = 0.35f;

        [Header("Handling")]
        [SerializeField] private WeaponFireMode _fireMode = WeaponFireMode.SemiAutomatic;
        [SerializeField, Range(2, 5)] private int _burstCount = 3;
        [SerializeField, Min(1)] private int _magazineSize = 12;
        [SerializeField, Min(0)] private int _startingReserveAmmo = 36;
        [SerializeField, Min(0)] private int _reserveAmmoCap = 48;
        [SerializeField, Min(0.1f)] private float _reloadSeconds = 1.4f;
        [SerializeField, Min(1)] private int _ammoPerPickup = 30;
        [SerializeField, Min(0f)] private float _spreadPerShot = 0.35f;
        [SerializeField, Range(0f, 15f)] private float _maxSpreadDegrees = 2.4f;
        [SerializeField, Min(0f)] private float _spreadRecovery = 12f;
        [SerializeField, Min(0f)] private float _recoilVertical = 0.9f;
        [SerializeField, Min(0f)] private float _recoilHorizontal = 0.3f;
        [SerializeField, Min(0f)] private float _moveSpreadPenalty = 1.5f;
        [SerializeField, Range(0.1f, 2f)] private float _adsSpreadMultiplier = 1f;

        [Header("Presentation")]
        [SerializeField] private GameObject _modelPrefab;
        [SerializeField] private Color _rarityColor = Color.white;

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public WeaponArchetype Archetype => _archetype;
        public ItemRarity Rarity => _rarity;
        public AmmoKind AmmoKind => _ammoKind;
        public float Damage => _damage;
        public float ShotsPerSecond => _shotsPerSecond;
        public int Pellets => _pellets;
        public float SpreadDegrees => _spreadDegrees;
        public WeaponFireMode FireMode => _fireMode;
        public int BurstCount => _burstCount;
        public int MagazineSize => _magazineSize;
        public int StartingReserveAmmo => _startingReserveAmmo;
        public int ReserveAmmoCap => _reserveAmmoCap;
        public float ReloadSeconds => _reloadSeconds;
        public int AmmoPerPickup => _ammoPerPickup;
        public float Range => _range;
        public float FullDamageRange => _fullDamageRange;
        public float MidDamageRange => _midDamageRange;
        public float FarDamageRange => _farDamageRange;
        public float SpreadPerShot => _spreadPerShot;
        public float MaxSpreadDegrees => _maxSpreadDegrees;
        public float SpreadRecovery => _spreadRecovery;
        public float RecoilVertical => _recoilVertical;
        public float RecoilHorizontal => _recoilHorizontal;
        public float MoveSpreadPenalty => _moveSpreadPenalty;
        public float AdsSpreadMultiplier => _adsSpreadMultiplier;
        public GameObject ModelPrefab => _modelPrefab;
        public Color RarityColor => _rarityColor;
        public float BodyDps => CombatBalance.CalculateBodyDps(this);
        public float IdealArmoredTtk => CombatBalance.CalculateIdealTimeToEliminate(this);

        public void SetPrototypeValues(
            string displayName,
            float damage,
            float shotsPerSecond,
            int magazineSize,
            int startingReserveAmmo,
            float range)
        {
            SetBalancedValues(
                "weapon.prototype",
                displayName,
                WeaponArchetype.Pistol,
                ItemRarity.Common,
                AmmoKind.Light,
                damage,
                shotsPerSecond,
                1,
                0.8f,
                magazineSize,
                startingReserveAmmo,
                1.4f,
                30,
                range,
                _modelPrefab,
                Color.white);
        }

        public void SetBalancedValues(
            string itemId,
            string displayName,
            WeaponArchetype archetype,
            ItemRarity rarity,
            AmmoKind ammoKind,
            float damage,
            float shotsPerSecond,
            int pellets,
            float spreadDegrees,
            int magazineSize,
            int startingReserveAmmo,
            float reloadSeconds,
            int ammoPerPickup,
            float range,
            GameObject modelPrefab,
            Color rarityColor,
            WeaponFireMode fireMode = WeaponFireMode.SemiAutomatic,
            int burstCount = 3,
            int reserveAmmoCap = -1,
            float fullDamageRange = -1f,
            float midDamageRange = -1f,
            float farDamageRange = -1f,
            float spreadPerShot = 0.35f,
            float maxSpreadDegrees = 2.4f,
            float spreadRecovery = 12f,
            float recoilVertical = 0.9f,
            float recoilHorizontal = 0.3f,
            float moveSpreadPenalty = 1.5f,
            float adsSpreadMultiplier = 1f)
        {
            _itemId = string.IsNullOrWhiteSpace(itemId) ? name : itemId;
            _displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            _archetype = archetype;
            _rarity = rarity;
            _ammoKind = ammoKind;
            _damage = Mathf.Max(1f, damage);
            _shotsPerSecond = Mathf.Max(0.1f, shotsPerSecond);
            _pellets = Mathf.Max(1, pellets);
            _spreadDegrees = Mathf.Clamp(spreadDegrees, 0f, 12f);
            _fireMode = fireMode;
            _burstCount = Mathf.Clamp(burstCount, 2, 5);
            _magazineSize = Mathf.Max(1, magazineSize);
            _reserveAmmoCap = reserveAmmoCap >= 0 ? Mathf.Max(0, reserveAmmoCap) : Mathf.Max(0, startingReserveAmmo);
            _startingReserveAmmo = Mathf.Clamp(startingReserveAmmo, 0, _reserveAmmoCap);
            _reloadSeconds = Mathf.Max(0.1f, reloadSeconds);
            _ammoPerPickup = Mathf.Max(1, ammoPerPickup);
            _range = Mathf.Max(1f, range);
            _fullDamageRange = Mathf.Clamp(fullDamageRange > 0f ? fullDamageRange : _range * 0.25f, 1f, _range);
            _midDamageRange = Mathf.Clamp(midDamageRange > 0f ? midDamageRange : _range * 0.6f, _fullDamageRange, _range);
            _farDamageRange = Mathf.Clamp(farDamageRange > 0f ? farDamageRange : _range * 0.85f, _midDamageRange, _range);
            _spreadPerShot = Mathf.Max(0f, spreadPerShot);
            _maxSpreadDegrees = Mathf.Clamp(maxSpreadDegrees, _spreadDegrees, 15f);
            _spreadRecovery = Mathf.Max(0f, spreadRecovery);
            _recoilVertical = Mathf.Max(0f, recoilVertical);
            _recoilHorizontal = Mathf.Max(0f, recoilHorizontal);
            _moveSpreadPenalty = Mathf.Max(0f, moveSpreadPenalty);
            _adsSpreadMultiplier = Mathf.Clamp(adsSpreadMultiplier, 0.1f, 2f);
            _modelPrefab = modelPrefab;
            _rarityColor = rarityColor;
        }

        public float EvaluateFalloff(float distance)
        {
            distance = Mathf.Max(0f, distance);
            if (distance <= _fullDamageRange)
            {
                return 1f;
            }

            if (distance <= _midDamageRange)
            {
                return Mathf.Lerp(1f, _midDamageMultiplier,
                    Mathf.InverseLerp(_fullDamageRange, _midDamageRange, distance));
            }

            if (distance <= _farDamageRange)
            {
                return Mathf.Lerp(_midDamageMultiplier, _farDamageMultiplier,
                    Mathf.InverseLerp(_midDamageRange, _farDamageRange, distance));
            }

            return _beyondDamageMultiplier;
        }
    }
}
