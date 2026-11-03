using HoldPointShowdown.Combat;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
using UnityEngine;

namespace HoldPointShowdown.AI
{
    [RequireComponent(typeof(CharacterController), typeof(Damageable), typeof(TeamMember))]
    [DisallowMultipleComponent]
    public sealed class PrototypeEnemyAI : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _detectionRange = 32f;
        [SerializeField, Min(1f)] private float _attackRange = 15f;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2.8f;
        [SerializeField, Min(0.1f)] private float _shotsPerSecond = 1.25f;
        [SerializeField, Min(1f)] private float _damage = 8f;
        [SerializeField] private Transform _target;
        [SerializeField] private Damageable _targetDamageable;
        [SerializeField] private Transform _muzzle;

        private Damageable _self;
        private TeamMember _teamMember;
        private CharacterController _controller;
        private PrototypeWeaponEffects _effects;
        private CombatantLifecycle _lifecycle;
        private Vector3 _spawnPosition;
        private Vector3 _patrolTarget;
        private float _nextShotAt;
        private float _nextTargetSearchAt;
        private float _gravityVelocity;

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

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _self = GetComponent<Damageable>();
            _teamMember = GetComponent<TeamMember>();
            _lifecycle = GetComponent<CombatantLifecycle>();
            _effects = GetComponent<PrototypeWeaponEffects>();
            _spawnPosition = transform.position;
            _patrolTarget = _spawnPosition + (transform.right * 3f);
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
                Patrol();
                return;
            }

            Vector3 toTarget = _target.position - transform.position;
            Vector3 planarToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            float distance = planarToTarget.magnitude;
            bool canSeeTarget = distance <= _detectionRange && HasLineOfSight();

            if (canSeeTarget && distance <= _attackRange)
            {
                FaceDirection(planarToTarget);
                TryShoot();
                ApplyGravity();
                return;
            }

            if (distance <= _detectionRange)
            {
                Move(planarToTarget.normalized);
                return;
            }

            Patrol();
        }

        private void AcquireNearestOpponent()
        {
            _nextTargetSearchAt = Time.time + 0.45f;
            _target = null;
            _targetDamageable = null;
            float bestDistance = float.MaxValue;

            foreach (TeamMember candidate in Object.FindObjectsByType<TeamMember>(FindObjectsInactive.Exclude))
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
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    _target = candidate.transform;
                    _targetDamageable = candidateDamageable;
                }
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
            if (Time.time < _nextShotAt || !IsTargetValid())
            {
                return;
            }

            _nextShotAt = Time.time + (1f / _shotsPerSecond);
            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + (Vector3.up * 1.7f);
            Vector3 targetPoint = _target.position + (Vector3.up * 1.35f);
            Vector3 direction = (targetPoint - origin).normalized;
            Vector3 endPoint = origin + (direction * _attackRange);
            bool didHit = HasLineOfSight();

            if (didHit)
            {
                endPoint = targetPoint;
                _targetDamageable.ApplyDamage(_damage, targetPoint, gameObject);
            }
            else if (Physics.Raycast(origin, direction, out RaycastHit hit, _attackRange,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;
            }

            _effects?.PlayExternalShot(origin, endPoint, didHit);
        }

        private void Patrol()
        {
            Vector3 toPatrol = Vector3.ProjectOnPlane(_patrolTarget - transform.position, Vector3.up);
            if (toPatrol.sqrMagnitude < 0.5f)
            {
                Vector3 offset = _patrolTarget - _spawnPosition;
                _patrolTarget = _spawnPosition - offset;
                toPatrol = Vector3.ProjectOnPlane(_patrolTarget - transform.position, Vector3.up);
            }

            Move(toPatrol.normalized * 0.55f);
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
            _nextTargetSearchAt = 0f;
        }
    }
}
