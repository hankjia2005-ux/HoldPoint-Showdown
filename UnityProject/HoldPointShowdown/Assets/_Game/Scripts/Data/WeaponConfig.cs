using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "HoldPoint Showdown/Weapon Config")]
    public sealed class WeaponConfig : ScriptableObject
    {
    [SerializeField] private string _displayName = "Prototype Pistol";
    [SerializeField, Min(1f)] private float _damage = 20f;
    [SerializeField, Min(0.1f)] private float _shotsPerSecond = 4f;
    [SerializeField, Min(1)] private int _magazineSize = 12;
    [SerializeField, Min(0)] private int _startingReserveAmmo = 30;
    [SerializeField, Min(1f)] private float _range = 80f;

    public string DisplayName => _displayName;
    public float Damage => _damage;
    public float ShotsPerSecond => _shotsPerSecond;
    public int MagazineSize => _magazineSize;
    public int StartingReserveAmmo => _startingReserveAmmo;
    public float Range => _range;

        public void SetPrototypeValues(
            string displayName,
            float damage,
            float shotsPerSecond,
            int magazineSize,
            int startingReserveAmmo,
            float range)
        {
            _displayName = displayName;
            _damage = Mathf.Max(1f, damage);
            _shotsPerSecond = Mathf.Max(0.1f, shotsPerSecond);
            _magazineSize = Mathf.Max(1, magazineSize);
            _startingReserveAmmo = Mathf.Max(0, startingReserveAmmo);
            _range = Mathf.Max(1f, range);
        }
    }
}
