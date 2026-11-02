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
        [SerializeField, Min(1f)] private float _range = 80f;

        [Header("Handling")]
        [SerializeField, Min(1)] private int _magazineSize = 12;
        [SerializeField, Min(0)] private int _startingReserveAmmo = 36;
        [SerializeField, Min(0.1f)] private float _reloadSeconds = 1.4f;
        [SerializeField, Min(1)] private int _ammoPerPickup = 30;

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
        public int MagazineSize => _magazineSize;
        public int StartingReserveAmmo => _startingReserveAmmo;
        public float ReloadSeconds => _reloadSeconds;
        public int AmmoPerPickup => _ammoPerPickup;
        public float Range => _range;
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
            Color rarityColor)
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
            _magazineSize = Mathf.Max(1, magazineSize);
            _startingReserveAmmo = Mathf.Max(0, startingReserveAmmo);
            _reloadSeconds = Mathf.Max(0.1f, reloadSeconds);
            _ammoPerPickup = Mathf.Max(1, ammoPerPickup);
            _range = Mathf.Max(1f, range);
            _modelPrefab = modelPrefab;
            _rarityColor = rarityColor;
        }
    }
}
