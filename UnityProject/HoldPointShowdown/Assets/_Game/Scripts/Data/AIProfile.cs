using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "AIProfile", menuName = "HoldPoint Showdown/AI Profile")]
    public sealed class AIProfile : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _reactionSeconds = 0.55f;
        [SerializeField, Range(0f, 18f)] private float _aimErrorDegrees = 4.5f;
        [SerializeField, Range(0f, 1f)] private float _hitChanceAt10m = 0.55f;
        [SerializeField, Min(1f)] private float _detectRadius = 55f;
        [SerializeField, Min(1f)] private float _chaseRadius = 30f;
        [SerializeField, Min(0.05f)] private float _burstInterval = 0.3f;
        [SerializeField, Min(0.1f)] private float _reloadSeconds = 2.4f;
        [SerializeField, Range(0.1f, 1.5f)] private float _damageMultiplier = 1f;
        [SerializeField] private bool _canStoreInChest = true;
        [SerializeField, Range(0f, 1f)] private float _raidBaseChance = 0.2f;

        public float ReactionSeconds => _reactionSeconds;
        public float AimErrorDegrees => _aimErrorDegrees;
        public float HitChanceAt10m => _hitChanceAt10m;
        public float DetectRadius => _detectRadius;
        public float ChaseRadius => _chaseRadius;
        public float BurstInterval => _burstInterval;
        public float ReloadSeconds => _reloadSeconds;
        public float DamageMultiplier => _damageMultiplier;
        public bool CanStoreInChest => _canStoreInChest;
        public float RaidBaseChance => _raidBaseChance;

        public void SetValues(
            float reactionSeconds,
            float aimErrorDegrees,
            float hitChanceAt10m,
            float detectRadius,
            float chaseRadius,
            float burstInterval,
            float reloadSeconds,
            float damageMultiplier,
            bool canStoreInChest,
            float raidBaseChance)
        {
            _reactionSeconds = Mathf.Max(0f, reactionSeconds);
            _aimErrorDegrees = Mathf.Clamp(aimErrorDegrees, 0f, 18f);
            _hitChanceAt10m = Mathf.Clamp01(hitChanceAt10m);
            _detectRadius = Mathf.Max(1f, detectRadius);
            _chaseRadius = Mathf.Clamp(chaseRadius, 1f, _detectRadius);
            _burstInterval = Mathf.Max(0.05f, burstInterval);
            _reloadSeconds = Mathf.Max(0.1f, reloadSeconds);
            _damageMultiplier = Mathf.Clamp(damageMultiplier, 0.1f, 1.5f);
            _canStoreInChest = canStoreInChest;
            _raidBaseChance = Mathf.Clamp01(raidBaseChance);
        }
    }
}
