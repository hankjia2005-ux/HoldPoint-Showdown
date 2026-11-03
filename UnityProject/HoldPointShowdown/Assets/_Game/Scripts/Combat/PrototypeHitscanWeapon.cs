using System;
using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
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
        private int _burstShotsRemaining;
        private float _currentSpreadBloom;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[32];
        private AudioFeedback _audioFeedback;
        private CharacterController _ownerController;
        private ThirdPersonPlayerController _playerController;
        private ThirdPersonCameraRig _cameraRig;

        public WeaponConfig Config => _config;
        public int MagazineAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading => _reloadCompleteAt > 0f;
        public float CurrentSpreadDegrees => EvaluateCurrentSpread();
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
            ReserveAmmo = config == null ? 0 : Mathf.Clamp(reserveAmmo, 0, config.ReserveAmmoCap);
            _nextShotAt = Mathf.Min(_nextShotAt, Time.time + 0.12f);
            _burstShotsRemaining = 0;
            _currentSpreadBloom = 0f;
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
            _audioFeedback?.PlayGunshot(_config);

            Ray centerRay = sourceCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float spreadDegrees = EvaluateCurrentSpread();
            for (int pellet = 0; pellet < _config.Pellets; pellet++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
                Vector3 direction = (
                    centerRay.direction +
                    (sourceCamera.transform.right * spread.x) +
                    (sourceCamera.transform.up * spread.y)).normalized;
                FireRay(new Ray(centerRay.origin, direction));
            }

            _currentSpreadBloom = Mathf.Min(
                Mathf.Max(0f, _config.MaxSpreadDegrees - _config.SpreadDegrees),
                _currentSpreadBloom + _config.SpreadPerShot);
            _cameraRig?.ApplyRecoil(
                _config.RecoilVertical,
                UnityEngine.Random.Range(-_config.RecoilHorizontal, _config.RecoilHorizontal));

            return true;
        }

        public bool ProcessTrigger(bool pressedThisFrame, bool held)
        {
            if (_config == null)
            {
                return false;
            }

            switch (_config.FireMode)
            {
                case WeaponFireMode.Automatic:
                    return held && TryFire();
                case WeaponFireMode.Burst:
                    if (pressedThisFrame && _burstShotsRemaining <= 0)
                    {
                        _burstShotsRemaining = _config.BurstCount;
                    }
                    return TryContinueBurst();
                default:
                    return pressedThisFrame && TryFire();
            }
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

            int before = ReserveAmmo;
            ReserveAmmo = Mathf.Min(_config != null ? _config.ReserveAmmoCap : int.MaxValue, ReserveAmmo + amount);
            AmmoChanged?.Invoke();
            return ReserveAmmo - before;
        }

        private void FireRay(Ray ray)
        {
            Vector3 endPoint = ray.origin + (ray.direction * _config.Range);
            Vector3 hitNormal = -ray.direction;
            bool didHit = false;

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _hitBuffer,
                _config.Range,
                _hitMask,
                QueryTriggerInteraction.Collide);
            bool foundBlockingHit = false;
            RaycastHit hit = default;
            float nearestDistance = _config.Range + 1f;
            Damageable selfDamageable = GetComponent<Damageable>();
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = _hitBuffer[index];
                IDamageable candidateDamageable = candidate.collider.GetComponentInParent<IDamageable>();
                if (candidateDamageable != null && ReferenceEquals(candidateDamageable, selfDamageable))
                {
                    continue;
                }
                if (candidate.collider.isTrigger && candidateDamageable == null)
                {
                    continue;
                }

                if (candidate.distance >= nearestDistance)
                {
                    continue;
                }

                foundBlockingHit = true;
                nearestDistance = candidate.distance;
                hit = candidate;
            }

            if (foundBlockingHit)
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
                    float multiplier = _config.EvaluateFalloff(hit.distance);
                    if (damageable is Damageable concreteDamageable)
                    {
                        multiplier *= concreteDamageable.EvaluateHitMultiplier(hit.collider, hit.point);
                    }
                    damageable?.ApplyDamage(_config.Damage * multiplier, hit.point, gameObject);
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
            _audioFeedback = GetComponent<AudioFeedback>();
            _ownerController = GetComponent<CharacterController>();
            _playerController = GetComponent<ThirdPersonPlayerController>();
            Camera sourceCamera = _aimCamera != null ? _aimCamera : Camera.main;
            _cameraRig = sourceCamera != null ? sourceCamera.GetComponent<ThirdPersonCameraRig>() : null;
            ResetAmmo();
        }

        private void Update()
        {
            if (IsReloading && Time.time >= _reloadCompleteAt)
            {
                CompleteReload();
            }

            if (_config != null && _config.FireMode == WeaponFireMode.Burst && _burstShotsRemaining > 0)
            {
                TryContinueBurst();
            }

            if (_config != null && _currentSpreadBloom > 0f)
            {
                _currentSpreadBloom = Mathf.MoveTowards(
                    _currentSpreadBloom,
                    0f,
                    _config.SpreadRecovery * Time.deltaTime);
            }
        }

        private float EvaluateCurrentSpread()
        {
            if (_config == null)
            {
                return 0f;
            }

            float movementRatio = _ownerController != null
                ? Mathf.Clamp01(Vector3.ProjectOnPlane(_ownerController.velocity, Vector3.up).magnitude / 7f)
                : 0f;
            float spread = _config.SpreadDegrees + _currentSpreadBloom + (_config.MoveSpreadPenalty * movementRatio);
            if (_playerController != null && _playerController.IsAiming)
            {
                spread *= _config.AdsSpreadMultiplier;
            }
            return Mathf.Clamp(spread, 0f, _config.MaxSpreadDegrees + _config.MoveSpreadPenalty);
        }

        private bool TryContinueBurst()
        {
            if (_burstShotsRemaining <= 0 || !TryFire())
            {
                return false;
            }

            _burstShotsRemaining--;
            return true;
        }
    }
}
