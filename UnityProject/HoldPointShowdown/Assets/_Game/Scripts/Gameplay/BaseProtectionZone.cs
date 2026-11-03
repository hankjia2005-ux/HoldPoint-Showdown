using System.Collections.Generic;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using UnityEngine;

namespace HoldPointShowdown.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class BaseProtectionZone : MonoBehaviour
    {
        [SerializeField] private TeamId _team;
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private Renderer _shieldRenderer;
        [SerializeField] private Vector3 _enemyEjectPoint;

        private BoxCollider _trigger;
        private readonly HashSet<Damageable> _protected = new();
        private readonly List<Damageable> _outsideZone = new();
        private float _nextInvasionAlertAt;

        public bool IsProtectionActive => _match != null
            && _match.IsRunning
            && _match.CurrentPhase == MatchPhase.Scavenge;
        public bool IsSpawnProtectionActive => _match != null
            && _match.IsRunning
            && (_match.CurrentPhase == MatchPhase.Raid || _match.CurrentPhase == MatchPhase.FinalBattle);

        public void Configure(
            TeamId team,
            MatchPhaseController match,
            Renderer shieldRenderer,
            Vector3 enemyEjectPoint,
            Vector3 spawnProtectionCenter)
        {
            _team = team;
            _match = match;
            _shieldRenderer = shieldRenderer;
            _enemyEjectPoint = enemyEjectPoint;
            EnsureTrigger();
            ApplyPhaseState();
        }

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnEnable()
        {
            if (_match == null)
            {
                _match = Object.FindAnyObjectByType<MatchPhaseController>();
            }

            if (_match != null)
            {
                _match.PhaseChanged += HandlePhaseChanged;
            }

            ApplyPhaseState();
        }

        private void OnDisable()
        {
            if (_match != null)
            {
                _match.PhaseChanged -= HandlePhaseChanged;
            }

            ClearProtection();
        }

        private void FixedUpdate()
        {
            if (_protected.Count == 0)
            {
                return;
            }

            _outsideZone.Clear();
            foreach (Damageable damageable in _protected)
            {
                if (damageable == null
                    || !ShouldProtect(damageable.transform.position))
                {
                    _outsideZone.Add(damageable);
                }
            }

            foreach (Damageable damageable in _outsideZone)
            {
                _protected.Remove(damageable);
                damageable?.SetInvulnerable(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_match == null || !_match.IsRunning)
            {
                return;
            }

            TeamMember member = other.GetComponentInParent<TeamMember>();
            if (member == null)
            {
                return;
            }

            Damageable damageable = member.GetComponent<Damageable>();
            if (member.Team == _team)
            {
                if (damageable != null && ShouldProtect(damageable.transform.position))
                {
                    damageable.SetInvulnerable(true);
                    _protected.Add(damageable);
                }
                return;
            }

            if (!IsProtectionActive)
            {
                if (Time.time >= _nextInvasionAlertAt)
                {
                    _nextInvasionAlertAt = Time.time + 3f;
                    _match.RaiseMessage($"{(_team == TeamId.Blue ? "蓝方" : "红方")}基地遭到入侵", 3f);
                }
                return;
            }

            CombatantLifecycle lifecycle = member.GetComponent<CombatantLifecycle>();
            if (lifecycle != null && lifecycle.IsEliminated)
            {
                return;
            }

            CharacterController controller = member.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null)
            {
                controller.enabled = false;
            }
            member.transform.position = _enemyEjectPoint;
            if (controller != null)
            {
                controller.enabled = wasEnabled;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Damageable damageable = other.GetComponentInParent<Damageable>();
            if (damageable != null && !ShouldProtect(damageable.transform.position) && _protected.Remove(damageable))
            {
                damageable.SetInvulnerable(false);
            }
        }

        private void HandlePhaseChanged(MatchPhase _, MatchPhase __)
        {
            ApplyPhaseState();
        }

        private void ApplyPhaseState()
        {
            bool active = IsProtectionActive;
            if (_shieldRenderer != null)
            {
                _shieldRenderer.enabled = active;
            }
            if (_trigger != null)
            {
                _trigger.enabled = _match == null || _match.IsRunning;
            }
            if (!active)
            {
                ClearProtection();
            }
        }

        private void ClearProtection()
        {
            foreach (Damageable damageable in _protected)
            {
                damageable?.SetInvulnerable(false);
            }
            _protected.Clear();
        }

        private void EnsureTrigger()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
        }

        private bool ShouldProtect(Vector3 position)
        {
            bool insideBaseShield = _trigger != null && _trigger.bounds.Contains(position);
            return IsProtectionActive && insideBaseShield;
        }
    }
}
