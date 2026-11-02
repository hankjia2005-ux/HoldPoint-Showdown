using HoldPointShowdown.Data;
using UnityEngine;

namespace HoldPointShowdown.Combat
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHitscanWeapon : MonoBehaviour
    {
    [SerializeField] private WeaponConfig _config;
    [SerializeField] private Camera _aimCamera;
    [SerializeField] private LayerMask _hitMask = Physics.DefaultRaycastLayers;

    private float _nextShotAt;

    public WeaponConfig Config => _config;
    public int MagazineAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }

    public void Configure(WeaponConfig config, Camera aimCamera)
    {
        _config = config;
        _aimCamera = aimCamera;
        ResetAmmo();
    }

    public bool TryFire()
    {
        if (_config == null || Time.time < _nextShotAt || MagazineAmmo <= 0)
        {
            return false;
        }

        Camera sourceCamera = _aimCamera != null ? _aimCamera : Camera.main;
        if (sourceCamera == null)
        {
            return false;
        }

        MagazineAmmo--;
        _nextShotAt = Time.time + (1f / _config.ShotsPerSecond);

        Ray ray = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 endPoint = ray.origin + (ray.direction * _config.Range);

        if (Physics.Raycast(ray, out RaycastHit hit, _config.Range, _hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.ApplyDamage(_config.Damage, hit.point, gameObject);
        }

        Debug.DrawLine(ray.origin, endPoint, Color.yellow, 0.2f);
        return true;
    }

    public bool Reload()
    {
        if (_config == null || MagazineAmmo >= _config.MagazineSize || ReserveAmmo <= 0)
        {
            return false;
        }

        int requested = _config.MagazineSize - MagazineAmmo;
        int loaded = Mathf.Min(requested, ReserveAmmo);
        MagazineAmmo += loaded;
        ReserveAmmo -= loaded;
        return true;
    }

    public void ResetAmmo()
    {
        if (_config == null)
        {
            MagazineAmmo = 0;
            ReserveAmmo = 0;
            return;
        }

        MagazineAmmo = _config.MagazineSize;
        ReserveAmmo = _config.StartingReserveAmmo;
    }

        private void Awake()
        {
            ResetAmmo();
        }
    }
}
