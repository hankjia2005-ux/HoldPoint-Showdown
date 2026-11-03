using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
using HoldPointShowdown.Runtime;
using UnityEngine;

namespace HoldPointShowdown.AI
{
    public enum EnemyAIIntent
    {
        Advance,
        Pursue,
        Engage,
        SearchLoot,
        StoreSupplies,
        RaidBase
    }

    [RequireComponent(typeof(CharacterController), typeof(Damageable), typeof(TeamMember))]
    [DisallowMultipleComponent]
    public sealed class PrototypeEnemyAI : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _detectionRange = 72f;
        [SerializeField, Min(1f)] private float _attackRange = 24f;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 6.2f;
        [SerializeField, Range(1, 5)] private int _burstSize = 3;
        [SerializeField, Min(0.05f)] private float _burstInterval = 0.12f;
        [SerializeField, Min(0.2f)] private float _burstCooldown = 1.3f;
        [SerializeField, Min(1f)] private float _damage = 9f;
        [SerializeField] private Transform _target;
        [SerializeField] private Damageable _targetDamageable;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private CombatantRuntimeSet _combatants;
        [SerializeField] private WeaponConfig _weaponConfig;
        [SerializeField] private StrategicHoldPoint _holdPoint;
        [SerializeField, Range(0f, 12f)] private float _minimumSpread = 1.4f;
        [SerializeField, Range(0f, 18f)] private float _maximumSpread = 6.5f;
        [SerializeField, Range(0.1f, 1f)] private float _damageScale = 0.75f;
        [SerializeField] private AIProfile _profile;
        [SerializeField] private LootDirector _lootDirector;
        [SerializeField] private SharedSupplyChest _friendlyChest;
        [SerializeField] private SharedSupplyChest _enemyChest;

        private Damageable _self;
        private TeamMember _teamMember;
        private CharacterController _controller;
        private PrototypeWeaponEffects _effects;
        private CombatantLifecycle _lifecycle;
        private MatchPhaseController _match;
        private Vector3 _spawnPosition;
        private Vector3 _patrolTarget;
        private float _nextShotAt;
        private float _nextBurstShotAt;
        private int _burstShotsRemaining;
        private float _nextTargetSearchAt;
        private float _gravityVelocity;
        private float _avoidanceSign;
        private AudioFeedback _audioFeedback;
        private readonly RaycastHit[] _shotHits = new RaycastHit[24];
        private PrototypeInventory _inventory;
        private WorldItemPickup _lootTarget;
        private float _targetAcquiredAt;
        private bool _raidDecisionMade;
        private bool _raidCommitted;

        public EnemyAIIntent CurrentIntent { get; private set; } = EnemyAIIntent.Advance;
        public bool HasTarget => IsTargetValid();
        public float MoveSpeed => _moveSpeed;
        public int BurstSize => _burstSize;

        public void Configure(Transform target, Damageable targetDamageable, Transform muzzle)
        {
            _target = target;
            _targetDamageable = targetDamageable;
            _muzzle = muzzle;
        }

        public void Configure(Transform muzzle)
        {
            _target = null;
            _targetDamageable = null;
            _muzzle = muzzle;
        }

        public void Configure(
            Transform muzzle,
            CombatantRuntimeSet combatants,
            GameBalanceConfig balance,
            WeaponConfig weaponConfig,
            MatchPhaseController match,
            StrategicHoldPoint holdPoint = null,
            AIProfile profile = null,
            LootDirector lootDirector = null,
            SharedSupplyChest friendlyChest = null,
            SharedSupplyChest enemyChest = null)
        {
            Configure(muzzle);
            _combatants = combatants;
            _weaponConfig = weaponConfig;
            _match = match;
            _holdPoint = holdPoint;
            _profile = profile;
            _lootDirector = lootDirector;
            _friendlyChest = friendlyChest;
            _enemyChest = enemyChest;
            if (balance != null)
            {
                _detectionRange = balance.AiDetectionRange;
                _attackRange = Mathf.Min(balance.AiAttackRange, weaponConfig != null ? weaponConfig.Range : balance.AiAttackRange);
                _minimumSpread = balance.AiMinimumSpread;
                _maximumSpread = balance.AiMaximumSpread;
                _damageScale = balance.AiDamageScale;
            }

            if (weaponConfig != null)
            {
                _damage = weaponConfig.Damage;
                _burstInterval = 1f / weaponConfig.ShotsPerSecond;
                _burstSize = weaponConfig.FireMode == WeaponFireMode.Burst
                    ? weaponConfig.BurstCount
                    : Mathf.Clamp(Mathf.RoundToInt(weaponConfig.ShotsPerSecond * 0.4f), 2, 4);
            }
            if (profile != null)
            {
                _detectionRange = profile.DetectRadius;
                _attackRange = Mathf.Min(_attackRange, weaponConfig != null ? weaponConfig.Range : _attackRange);
                _minimumSpread = profile.AimErrorDegrees * 0.55f;
                _maximumSpread = profile.AimErrorDegrees;
                _damageScale = profile.DamageMultiplier;
                _burstCooldown = profile.BurstInterval;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _self = GetComponent<Damageable>();
            _teamMember = GetComponent<TeamMember>();
            _lifecycle = GetComponent<CombatantLifecycle>();
            _effects = GetComponent<PrototypeWeaponEffects>();
            _audioFeedback = GetComponent<AudioFeedback>();
            _inventory = GetComponent<PrototypeInventory>();
            _spawnPosition = transform.position;
            _patrolTarget = _spawnPosition + (transform.right * 3f);
            _avoidanceSign = transform.position.x >= 0f ? 1f : -1f;
        }

        private void OnEnable()
        {
            _self ??= GetComponent<Damageable>();
            _self.ResetCompleted += HandleReset;
        }

        private void OnDisable()
        {
            if (_self != null)
            {
                _self.ResetCompleted -= HandleReset;
            }
        }

        private void Update()
        {
            _lifecycle ??= GetComponent<CombatantLifecycle>();
            if (_self == null || !_self.IsAlive || (_lifecycle != null && !_lifecycle.CanAct))
            {
                return;
            }

            if (Time.time >= _nextTargetSearchAt || !IsTargetValid())
            {
                AcquireNearestOpponent();
            }

            if (_target == null || _targetDamageable == null)
            {
                PerformNonCombatIntent();
                return;
            }

            Vector3 toTarget = _target.position - transform.position;
            Vector3 planarToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            float distance = planarToTarget.magnitude;
            bool canSeeTarget = distance <= _detectionRange && HasLineOfSight();

            if (canSeeTarget && distance <= _attackRange)
            {
                CurrentIntent = EnemyAIIntent.Engage;
                FaceDirection(planarToTarget);
                if (Time.time - _targetAcquiredAt >= (_profile != null ? _profile.ReactionSeconds : 0.55f))
                {
                    TryShoot();
                }
                ApplyGravity();
                return;
            }

            CurrentIntent = EnemyAIIntent.Pursue;
            Move(ResolveSteering(planarToTarget.normalized));
        }

        private void AcquireNearestOpponent()
        {
            _nextTargetSearchAt = Time.time + 0.45f;
            Transform previousTarget = _target;
            _target = null;
            _targetDamageable = null;
            float bestDistance = float.MaxValue;
            float detectionSquared = _detectionRange * _detectionRange;

            if (_combatants == null)
            {
                return;
            }

            _combatants.RemoveMissingEntries();
            foreach (TeamMember candidate in _combatants.Items)
            {
                if (candidate == null || candidate == _teamMember || candidate.Team == _teamMember.Team)
                {
                    continue;
                }

                Damageable candidateDamageable = candidate.GetComponent<Damageable>();
                CombatantLifecycle candidateLifecycle = candidate.GetComponent<CombatantLifecycle>();
                if (candidateDamageable == null || !candidateDamageable.IsAlive
                    || (candidateLifecycle != null && candidateLifecycle.IsEliminated))
                {
                    continue;
                }

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance <= detectionSquared && distance < bestDistance)
                {
                    bestDistance = distance;
                    _target = candidate.transform;
                    _targetDamageable = candidateDamageable;
                }
            }
            if (_target != null && _target != previousTarget)
            {
                _targetAcquiredAt = Time.time;
            }
        }

        private bool IsTargetValid()
        {
            if (_target == null || _targetDamageable == null || !_targetDamageable.IsAlive)
            {
                return false;
            }

            TeamMember targetTeam = _target.GetComponent<TeamMember>();
            return targetTeam != null && _teamMember != null && targetTeam.Team != _teamMember.Team;
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + (Vector3.up * 1.7f);
            Vector3 destination = _target.position + (Vector3.up * 1.35f);
            Vector3 direction = destination - origin;
            if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider.GetComponentInParent<Damageable>() == _targetDamageable;
        }

        private void TryShoot()
        {
            if (!IsTargetValid())
            {
                return;
            }

            WeaponConfig carried = _inventory != null && _inventory.Loadout != null
                ? _inventory.Loadout.ActiveWeapon
                : null;
            if (carried != null && carried != _weaponConfig)
            {
                _weaponConfig = carried;
                _damage = carried.Damage;
                _attackRange = Mathf.Min(_detectionRange, carried.Range);
                _burstInterval = 1f / carried.ShotsPerSecond;
                _burstSize = carried.FireMode == WeaponFireMode.Burst
                    ? carried.BurstCount
                    : Mathf.Clamp(Mathf.RoundToInt(carried.ShotsPerSecond * 0.4f), 2, 4);
            }

            if (_burstShotsRemaining <= 0)
            {
                if (Time.time < _nextShotAt)
                {
                    return;
                }
                _burstShotsRemaining = _burstSize;
                _nextBurstShotAt = Time.time;
                _nextShotAt = Time.time + _burstCooldown;
            }
            if (Time.time < _nextBurstShotAt)
            {
                return;
            }
            _burstShotsRemaining--;
            _nextBurstShotAt = Time.time + _burstInterval;
            _lifecycle?.CancelSpawnProtection();
            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + (Vector3.up * 1.7f);
            Vector3 targetPoint = _target.position + (Vector3.up * 1.35f);
            Vector3 direct = targetPoint - origin;
            float distance = direct.magnitude;
            float distanceFactor = Mathf.InverseLerp(6f, _attackRange, distance);
            float spreadDegrees = Mathf.Lerp(_minimumSpread, _maximumSpread, distanceFactor);
            Vector2 spread = Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            Vector3 direction = (direct.normalized
                + (transform.right * spread.x)
                + (transform.up * spread.y)).normalized;
            Vector3 endPoint = origin + (direction * _attackRange);
            bool didHit = false;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _shotHits,
                _attackRange,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.MaxValue;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = _shotHits[index];
                Damageable candidateDamageable = candidate.collider.GetComponentInParent<Damageable>();
                if (candidateDamageable == _self || candidate.distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = candidate.distance;
            }

            if (nearestDistance < float.MaxValue)
            {
                endPoint = nearest.point;
                Damageable hitDamageable = nearest.collider.GetComponentInParent<Damageable>();
                TeamMember hitTeam = nearest.collider.GetComponentInParent<TeamMember>();
                bool enemy = hitDamageable != null
                    && hitTeam != null
                    && _teamMember != null
                    && hitTeam.Team != _teamMember.Team;
                if (enemy)
                {
                    float hitChance = EvaluateHitChance(distance);
                    if (Random.value <= hitChance)
                    {
                        didHit = true;
                        float falloff = _weaponConfig != null ? _weaponConfig.EvaluateFalloff(nearest.distance) : 1f;
                        float hitMultiplier = hitDamageable.EvaluateHitMultiplier(nearest.collider, nearest.point);
                        hitDamageable.ApplyDamage(_damage * _damageScale * falloff * hitMultiplier, nearest.point, gameObject);
                    }
                }
            }

            _effects?.PlayExternalShot(origin, endPoint, didHit);
            _audioFeedback?.PlayExternalGunshot(_damage);
        }

        private void AdvanceTowardBattle()
        {
            CurrentIntent = EnemyAIIntent.Advance;
            float advanceDirection = _teamMember != null && _teamMember.Team == HoldPointShowdown.Core.TeamId.Red
                ? -1f
                : 1f;
            Vector3 destination = new(
                _spawnPosition.x * 0.35f,
                transform.position.y,
                (_match != null && _match.CurrentPhase == HoldPointShowdown.Core.MatchPhase.Scavenge)
                    ? 0f
                    : _spawnPosition.z + (advanceDirection * 120f));
            if (_holdPoint != null
                && _match != null
                && _match.CurrentPhase == MatchPhase.Raid
                && _holdPoint.ControllingTeam != _teamMember.Team)
            {
                destination = _holdPoint.transform.position;
                destination.y = transform.position.y;
            }
            Vector3 toBattle = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (toBattle.sqrMagnitude < 1f)
            {
                Vector3 offset = _patrolTarget - _spawnPosition;
                _patrolTarget = _spawnPosition - offset;
                toBattle = Vector3.ProjectOnPlane(_patrolTarget - transform.position, Vector3.up);
            }

            Move(ResolveSteering(toBattle.normalized));
        }

        private void PerformNonCombatIntent()
        {
            if (_profile != null && _profile.CanStoreInChest && _inventory != null
                && _inventory.UsedSlots >= 8 && _friendlyChest != null)
            {
                CurrentIntent = EnemyAIIntent.StoreSupplies;
                if (MoveToward(_friendlyChest.transform.position, 2.8f))
                {
                    int stored = 0;
                    foreach (SupplyKind kind in new[] { SupplyKind.AmmoPack, SupplyKind.Medkit, SupplyKind.ArmorPlate })
                    {
                        while (stored < 3 && _friendlyChest.TryDepositSupply(_inventory, _teamMember, kind))
                        {
                            stored++;
                        }
                    }
                }
                return;
            }

            if (_match != null && _match.CurrentPhase == MatchPhase.Raid && !_raidDecisionMade)
            {
                _raidDecisionMade = true;
                _raidCommitted = _enemyChest != null
                    && Random.value <= (_profile != null ? _profile.RaidBaseChance : 0.2f);
            }
            if (_match != null && _match.CurrentPhase != MatchPhase.Raid)
            {
                _raidDecisionMade = false;
                _raidCommitted = false;
            }
            if (_raidCommitted && _enemyChest != null && _inventory != null)
            {
                CurrentIntent = EnemyAIIntent.RaidBase;
                if (MoveToward(_enemyChest.transform.position, 2.8f))
                {
                    bool stolen = _enemyChest.TryWithdrawSupply(_inventory, _teamMember, SupplyKind.AmmoPack)
                        || _enemyChest.TryWithdrawSupply(_inventory, _teamMember, SupplyKind.Medkit)
                        || _enemyChest.TryWithdrawSupply(_inventory, _teamMember, SupplyKind.ArmorPlate);
                    _raidCommitted = !stolen;
                }
                return;
            }

            if (_lootDirector != null && _inventory != null && _inventory.FreeSlots > 0
                && (_match == null || _match.CurrentPhase != MatchPhase.FinalBattle))
            {
                if (_lootTarget == null || !_lootTarget.IsAvailable)
                {
                    _lootTarget = _lootDirector.FindNearestAvailable(transform.position, 32f);
                }
                if (_lootTarget != null)
                {
                    CurrentIntent = EnemyAIIntent.SearchLoot;
                    if (MoveToward(_lootTarget.transform.position, 1.2f))
                    {
                        _lootTarget.TryCollect(_inventory);
                        _lootTarget = null;
                    }
                    return;
                }
            }

            AdvanceTowardBattle();
        }

        private bool MoveToward(Vector3 destination, float stopDistance)
        {
            Vector3 direction = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (direction.magnitude <= stopDistance)
            {
                ApplyGravity();
                return true;
            }
            Move(ResolveSteering(direction.normalized));
            return false;
        }

        private float EvaluateHitChance(float distance)
        {
            float chanceAt10m = _profile != null ? _profile.HitChanceAt10m : 0.55f;
            float denominator = Mathf.Max(1f, _detectionRange - 10f);
            float distanceFactor = Mathf.Clamp(1f - ((Mathf.Max(10f, distance) - 10f) / denominator * 0.55f), 0.25f, 1f);
            return Mathf.Clamp01(chanceAt10m * distanceFactor);
        }

        private Vector3 ResolveSteering(Vector3 desiredDirection)
        {
            if (desiredDirection.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 origin = transform.position + (Vector3.up * 0.8f) + (desiredDirection * 0.7f);
            if (!Physics.SphereCast(
                    origin,
                    0.38f,
                    desiredDirection,
                    out RaycastHit hit,
                    1.4f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return desiredDirection;
            }

            Damageable blockingDamageable = hit.collider.GetComponentInParent<Damageable>();
            if (blockingDamageable != null && blockingDamageable == _targetDamageable)
            {
                return desiredDirection;
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, desiredDirection) * _avoidanceSign;
            return ((desiredDirection * 0.25f) + lateral).normalized;
        }

        private void Move(Vector3 direction)
        {
            if (_controller == null || !_controller.enabled)
            {
                return;
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                FaceDirection(direction);
            }

            if (_controller.isGrounded)
            {
                _gravityVelocity = -2f;
            }
            else
            {
                _gravityVelocity -= 24f * Time.deltaTime;
            }

            Vector3 velocity = (direction * _moveSpeed) + (Vector3.up * _gravityVelocity);
            _controller.Move(velocity * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            Move(Vector3.zero);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                1f - Mathf.Exp(-9f * Time.deltaTime));
        }

        private void HandleReset(Damageable _)
        {
            if (_lifecycle == null)
            {
                transform.position = _spawnPosition;
            }

            _nextShotAt = Time.time + 0.6f;
            _burstShotsRemaining = 0;
            _nextTargetSearchAt = 0f;
        }
    }
}
