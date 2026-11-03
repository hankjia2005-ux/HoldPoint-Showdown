using System;
using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
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
        private float _reloadCompleteAt;
        private int _pendingReloadAmount;

        public WeaponConfig Config => _config;
        public int MagazineAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading => _reloadCompleteAt > 0f;
        public event Action<HitscanShot> Fired;
        public event Action<WeaponConfig> ConfigChanged;
        public event Action AmmoChanged;

        public void Configure(WeaponConfig config, Camera aimCamera)
        {
            _aimCamera = aimCamera;
            Equip(config, config != null ? config.MagazineSize : 0, config != null ? config.StartingReserveAmmo : 0);
        }

        public void Equip(WeaponConfig config, int magazineAmmo, int reserveAmmo)
        {
            _config = config;
            CancelReload();
            MagazineAmmo = config == null ? 0 : Mathf.Clamp(magazineAmmo, 0, config.MagazineSize);
            ReserveAmmo = Mathf.Max(0, reserveAmmo);
            _nextShotAt = Mathf.Min(_nextShotAt, Time.time + 0.12f);
            ConfigChanged?.Invoke(_config);
            AmmoChanged?.Invoke();
        }

        public bool TryFire()
        {
            if (_config == null || IsReloading || Time.time < _nextShotAt || MagazineAmmo <= 0)
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
            AmmoChanged?.Invoke();

            Ray centerRay = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            for (int pellet = 0; pellet < _config.Pellets; pellet++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * Mathf.Tan(_config.SpreadDegrees * Mathf.Deg2Rad);
                Vector3 direction = (
                    centerRay.direction +
                    (sourceCamera.transform.right * spread.x) +
                    (sourceCamera.transform.up * spread.y)).normalized;
                FireRay(new Ray(centerRay.origin, direction));
            }

            return true;
        }

        public bool Reload()
        {
            if (_config == null || IsReloading || MagazineAmmo >= _config.MagazineSize || ReserveAmmo <= 0)
            {
                return false;
            }

            _pendingReloadAmount = Mathf.Min(_config.MagazineSize - MagazineAmmo, ReserveAmmo);
            _reloadCompleteAt = Time.time + _config.ReloadSeconds;
            return _pendingReloadAmount > 0;
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
            CancelReload();
            AmmoChanged?.Invoke();
        }

        public int AddReserveAmmo(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            ReserveAmmo += amount;
            AmmoChanged?.Invoke();
            return amount;
        }

        private void FireRay(Ray ray)
        {
            Vector3 endPoint = ray.origin + (ray.direction * _config.Range);
            Vector3 hitNormal = -ray.direction;
            bool didHit = false;

            if (Physics.Raycast(ray, out RaycastHit hit, _config.Range, _hitMask, QueryTriggerInteraction.Ignore))
            {
                didHit = true;
                endPoint = hit.point;
                hitNormal = hit.normal;
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                TeamMember sourceTeam = GetComponent<TeamMember>();
                TeamMember targetTeam = hit.collider.GetComponentInParent<TeamMember>();
                bool friendly = sourceTeam != null && targetTeam != null && sourceTeam.Team == targetTeam.Team;
                if (!friendly)
                {
                    damageable?.ApplyDamage(_config.Damage, hit.point, gameObject);
                }
            }

            Debug.DrawLine(ray.origin, endPoint, _config.RarityColor, 0.2f);
            Fired?.Invoke(new HitscanShot(ray.origin, endPoint, hitNormal, didHit));
        }

        private void CompleteReload()
        {
            MagazineAmmo += _pendingReloadAmount;
            ReserveAmmo -= _pendingReloadAmount;
            CancelReload();
            AmmoChanged?.Invoke();
        }

        private void CancelReload()
        {
            _reloadCompleteAt = 0f;
            _pendingReloadAmount = 0;
        }

        private void Awake()
        {
            ResetAmmo();
        }

        private void Update()
        {
            if (IsReloading && Time.time >= _reloadCompleteAt)
            {
                CompleteReload();
            }
        }
    }
}
