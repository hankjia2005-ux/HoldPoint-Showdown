using System;
using HoldPointShowdown.Data;
using UnityEngine;

namespace HoldPointShowdown.Combat
{
    public readonly struct HitscanShot
    {
        public HitscanShot(Vector3 origin, Vector3 endPoint, Vector3 normal, bool hit)
        {
            Origin = origin;
            EndPoint = endPoint;
            Normal = normal;
            Hit = hit;
        }

        public Vector3 Origin { get; }
        public Vector3 EndPoint { get; }
        public Vector3 Normal { get; }
        public bool Hit { get; }
    }

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
    public event Action<HitscanShot> Fired;

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
        Vector3 hitNormal = -ray.direction;
        bool didHit = false;

        if (Physics.Raycast(ray, out RaycastHit hit, _config.Range, _hitMask, QueryTriggerInteraction.Ignore))
        {
            didHit = true;
            endPoint = hit.point;
            hitNormal = hit.normal;
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.ApplyDamage(_config.Damage, hit.point, gameObject);
        }

        Debug.DrawLine(ray.origin, endPoint, Color.yellow, 0.2f);
        Fired?.Invoke(new HitscanShot(ray.origin, endPoint, hitNormal, didHit));
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

    public int AddReserveAmmo(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        ReserveAmmo += amount;
        return amount;
    }

        private void Awake()
        {
            ResetAmmo();
        }
    }
}
