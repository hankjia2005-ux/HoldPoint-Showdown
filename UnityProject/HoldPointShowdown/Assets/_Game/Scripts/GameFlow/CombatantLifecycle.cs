using System.Collections;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
using HoldPointShowdown.Runtime;
using UnityEngine;

namespace HoldPointShowdown.GameFlow
{
    [RequireComponent(typeof(Damageable), typeof(TeamMember))]
    [DisallowMultipleComponent]
    public sealed class CombatantLifecycle : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private Damageable _damageable;
        [SerializeField] private TeamMember _teamMember;
        [SerializeField] private PrototypeInventory _inventory;
        [SerializeField] private WeaponLoadout _loadout;
        [SerializeField] private WeaponConfig _starterWeapon;
        [SerializeField] private bool _isHumanPlayer;
        [SerializeField] private Vector3 _spawnPosition;
        [SerializeField] private Quaternion _spawnRotation = Quaternion.identity;
        [SerializeField] private bool _hasConfiguredSpawn;
        [SerializeField] private CameraDirectiveChannel _cameraDirectives;

        private CharacterController _characterController;
        private Coroutine _respawnRoutine;
        private AudioFeedback _audioFeedback;
        private CharacterCombatFeedback _combatFeedback;
        private PrototypeHitscanWeapon _weapon;
        private Vector3 _activeSpawnPosition;
        private float _spawnProtectionUntil;
        private bool _spawnProtectionActive;

        public TeamId Team => _teamMember != null ? _teamMember.Team : TeamId.Blue;
        public bool IsHumanPlayer => _isHumanPlayer;
        public bool IsEliminated { get; private set; }
        public int FinalLivesRemaining { get; private set; }
        public float RespawnAt { get; private set; }
        public float RespawnSecondsRemaining => Mathf.Max(0f, RespawnAt - Time.time);
        public bool IsWaitingToRespawn => _respawnRoutine != null;
        public bool CanAct => !IsEliminated
            && _damageable != null
            && _damageable.IsAlive
            && (_match == null || _match.IsRunning);
        public Damageable Damageable => _damageable;
        public Vector3 SpawnPosition => _spawnPosition;
        public bool IsSpawnProtected => _spawnProtectionActive && Time.time < _spawnProtectionUntil;

        public void Configure(
            MatchPhaseController match,
            WeaponConfig starterWeapon,
            PrototypeInventory inventory,
            WeaponLoadout loadout,
            bool isHumanPlayer,
            CameraDirectiveChannel cameraDirectives = null)
        {
            _match = match;
            _starterWeapon = starterWeapon;
            _inventory = inventory;
            _loadout = loadout;
            _isHumanPlayer = isHumanPlayer;
            _cameraDirectives = cameraDirectives;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _hasConfiguredSpawn = true;
            CacheReferences();
            _damageable?.SetAutoReset(false);
        }

        public void InitializeForMatch()
        {
            CancelRespawn();
            IsEliminated = false;
            FinalLivesRemaining = 0;
            RespawnAt = 0f;
            _spawnProtectionActive = false;
            TeleportToSpawn(false);
            SetCollisionEnabled(true);
            _loadout?.ResetToStarter();
            _damageable?.SetInvulnerable(false);
            _damageable?.ResetState();
        }

        public void PrepareForFinalBattle(int lives)
        {
            FinalLivesRemaining = Mathf.Max(1, lives);
            IsEliminated = false;
            if (_damageable != null && !_damageable.IsAlive)
            {
                ForceRespawn();
            }
        }

        public int ConsumeFinalLife()
        {
            FinalLivesRemaining = Mathf.Max(0, FinalLivesRemaining - 1);
            return FinalLivesRemaining;
        }

        public void BeginRespawn(float delay)
        {
            if (IsEliminated)
            {
                return;
            }

            CancelRespawn();
            RespawnAt = Time.time + Mathf.Max(0f, delay);
            _respawnRoutine = StartCoroutine(RespawnRoutine(Mathf.Max(0f, delay)));
        }

        public void Eliminate()
        {
            CancelRespawn();
            IsEliminated = true;
            FinalLivesRemaining = 0;
            RespawnAt = 0f;
            SetCollisionEnabled(false);
            if (_isHumanPlayer)
            {
                _cameraDirectives?.RaiseSpectate(_match != null ? _match.GetSpectatorTarget(Team) : null);
            }
        }

        public void StopForMatchEnd()
        {
            CancelRespawn();
            RespawnAt = 0f;
        }

        private void Awake()
        {
            CacheReferences();
            if (!_hasConfiguredSpawn)
            {
                _spawnPosition = transform.position;
                _spawnRotation = transform.rotation;
            }

            _damageable?.SetAutoReset(false);
        }

        private void OnEnable()
        {
            CacheReferences();
            if (_damageable != null)
            {
                _damageable.Died += HandleDied;
            }
            if (_weapon != null)
            {
                _weapon.Fired += HandleWeaponFired;
            }
        }

        private void Start()
        {
            if (_match == null)
            {
                _match = Object.FindAnyObjectByType<MatchPhaseController>();
            }

            _match?.RegisterCombatant(this);
        }

        private void OnDisable()
        {
            if (_damageable != null)
            {
                _damageable.Died -= HandleDied;
            }
            if (_weapon != null)
            {
                _weapon.Fired -= HandleWeaponFired;
            }

            CancelRespawn();
        }

        private void HandleDied(Damageable _)
        {
            Vector3 deathPosition = transform.position;
            _combatFeedback?.CaptureCorpse(deathPosition);
            if (_isHumanPlayer && _damageable != null && _damageable.LastInstigator != null)
            {
                _cameraDirectives?.RaiseDeathFocus(
                    _damageable.LastInstigator.transform,
                    Mathf.Clamp(_match != null ? _match.Rules.GetRespawnDelay(_match.CurrentPhase) : 0.75f, 0.65f, 1.6f));
            }
            _inventory?.DropAllOnDeath(deathPosition);
            _loadout?.ResetToStarter();
            SetCollisionEnabled(false);
            CancelSpawnProtection();
            TeleportToSpawn(false);

            if (_match != null)
            {
                _match.ReportDeath(this);
            }
            else
            {
                BeginRespawn(0.75f);
            }
        }

        private IEnumerator RespawnRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }

            _respawnRoutine = null;
            if (IsEliminated || (_match != null && !_match.IsRunning))
            {
                yield break;
            }

            ForceRespawn();
        }

        private void ForceRespawn()
        {
            CancelRespawn();
            IsEliminated = false;
            RespawnAt = 0f;
            TeleportToSpawn(true);
            SetCollisionEnabled(true);
            _loadout?.ResetToStarter();
            _damageable?.SetInvulnerable(false);
            _damageable?.ResetState();
            float protectionSeconds = _match != null && _match.Rules != null
                ? _match.Rules.GetSpawnProtectionSeconds(_match.CurrentPhase)
                : 1.5f;
            BeginSpawnProtection(protectionSeconds);
            _audioFeedback?.PlayRespawn();
            if (_isHumanPlayer)
            {
                _cameraDirectives?.RaiseReturnToPlayer(transform);
            }
        }

        private void TeleportToSpawn(bool chooseAlternate)
        {
            bool controllerWasEnabled = _characterController != null && _characterController.enabled;
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            _activeSpawnPosition = chooseAlternate ? ChooseSpawnPosition() : _spawnPosition;
            transform.SetPositionAndRotation(_activeSpawnPosition, _spawnRotation);

            if (_characterController != null && controllerWasEnabled && (_damageable == null || _damageable.IsAlive))
            {
                _characterController.enabled = true;
            }
        }

        private void SetCollisionEnabled(bool enabled)
        {
            if (_characterController != null)
            {
                _characterController.enabled = enabled;
            }
        }

        private void CancelRespawn()
        {
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }
        }

        private void CacheReferences()
        {
            _damageable ??= GetComponent<Damageable>();
            _teamMember ??= GetComponent<TeamMember>();
            _characterController ??= GetComponent<CharacterController>();
            _inventory ??= GetComponent<PrototypeInventory>();
            _loadout ??= GetComponent<WeaponLoadout>();
            _audioFeedback ??= GetComponent<AudioFeedback>();
            _combatFeedback ??= GetComponentInChildren<CharacterCombatFeedback>();
            _weapon ??= GetComponent<PrototypeHitscanWeapon>();
        }

        private void Update()
        {
            if (!_spawnProtectionActive)
            {
                return;
            }

            float radius = _match != null && _match.Rules != null
                ? _match.Rules.SpawnProtectionRadius
                : 3f;
            if (Time.time >= _spawnProtectionUntil
                || (transform.position - _activeSpawnPosition).sqrMagnitude > radius * radius)
            {
                CancelSpawnProtection();
            }
        }

        private void BeginSpawnProtection(float seconds)
        {
            float duration = Mathf.Max(0f, seconds);
            _spawnProtectionActive = duration > 0f;
            _spawnProtectionUntil = Time.time + duration;
            _damageable?.GrantInvulnerability(duration);
        }

        public void CancelSpawnProtection()
        {
            if (!_spawnProtectionActive)
            {
                return;
            }
            _spawnProtectionActive = false;
            _spawnProtectionUntil = 0f;
            _damageable?.ClearTemporaryInvulnerability();
        }

        private void HandleWeaponFired(HitscanShot _)
        {
            CancelSpawnProtection();
        }

        private Vector3 ChooseSpawnPosition()
        {
            if (_match != null && _match.CurrentPhase == MatchPhase.Scavenge)
            {
                return _spawnPosition;
            }
            int count = _match != null && _match.Rules != null ? _match.Rules.SpawnPointCount : 1;
            if (count <= 1)
            {
                return _spawnPosition;
            }

            int index = Random.Range(0, count);
            float centeredIndex = index - ((count - 1) * 0.5f);
            return _spawnPosition + (_spawnRotation * Vector3.right * centeredIndex * 2.4f);
        }
    }
}
