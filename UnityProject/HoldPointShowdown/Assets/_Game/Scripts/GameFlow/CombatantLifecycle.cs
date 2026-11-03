using System.Collections;
using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.Player;
using HoldPointShowdown.Presentation;
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

        private CharacterController _characterController;
        private Coroutine _respawnRoutine;
        private AudioFeedback _audioFeedback;
        private CharacterCombatFeedback _combatFeedback;

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

        public void Configure(
            MatchPhaseController match,
            WeaponConfig starterWeapon,
            PrototypeInventory inventory,
            WeaponLoadout loadout,
            bool isHumanPlayer)
        {
            _match = match;
            _starterWeapon = starterWeapon;
            _inventory = inventory;
            _loadout = loadout;
            _isHumanPlayer = isHumanPlayer;
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
            TeleportToSpawn();
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

            CancelRespawn();
        }

        private void HandleDied(Damageable _)
        {
            Vector3 deathPosition = transform.position;
            _combatFeedback?.CaptureCorpse(deathPosition);
            if (_isHumanPlayer && _damageable != null && _damageable.LastInstigator != null)
            {
                Camera.main?.GetComponent<ThirdPersonCameraRig>()?.BeginDeathFocus(
                    _damageable.LastInstigator.transform,
                    Mathf.Max(0.65f, _match != null ? _match.Rules.GetRespawnDelay(_match.CurrentPhase) : 0.75f));
            }
            _inventory?.DropAllOnDeath(deathPosition);
            _loadout?.ResetToStarter();
            SetCollisionEnabled(false);
            TeleportToSpawn();

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
            TeleportToSpawn();
            SetCollisionEnabled(true);
            _loadout?.ResetToStarter();
            _damageable?.SetInvulnerable(false);
            _damageable?.ResetState();
            _damageable?.GrantInvulnerability(2f);
            _audioFeedback?.PlayRespawn();
            if (_isHumanPlayer)
            {
                Camera.main?.GetComponent<ThirdPersonCameraRig>()?.EndDeathFocus();
            }
        }

        private void TeleportToSpawn()
        {
            bool controllerWasEnabled = _characterController != null && _characterController.enabled;
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);

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
        }
    }
}
