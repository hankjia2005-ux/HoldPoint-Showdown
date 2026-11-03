using System;
using HoldPointShowdown.Data;
using HoldPointShowdown.Presentation;
using UnityEngine;

namespace HoldPointShowdown.Combat
{
    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _maxArmor;
        [SerializeField, Min(0f)] private float _startingArmor;
        [SerializeField, Range(0f, 0.9f)] private float _armorAbsorptionFraction = 0.25f;
        [SerializeField] private bool _resetAfterDeath = true;
        [SerializeField, Min(0.1f)] private float _resetDelay = 2f;

        private float _resetAt;
        private bool _manualInvulnerable;
        private float _invulnerableUntil;
        private AudioFeedback _audioFeedback;

        public event Action<Damageable> Died;
        public event Action<Damageable> ResetCompleted;
        public event Action<Damageable, float, Vector3, GameObject> Damaged;
        public event Action<float> InvulnerabilityGranted;

        public float CurrentHealth { get; private set; }
        public float CurrentArmor { get; private set; }
        public float MaxHealth => _maxHealth;
        public float MaxArmor => _maxArmor;
        public bool IsAlive => CurrentHealth > 0f;
        public bool IsInvulnerable => _manualInvulnerable || Time.time < _invulnerableUntil;
        public GameObject LastInstigator { get; private set; }
        public Vector3 LastHitPoint { get; private set; }

        public void Configure(
            float maxHealth,
            float maxArmor,
            bool resetAfterDeath = true,
            float startingArmor = 0f)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            _maxArmor = Mathf.Max(0f, maxArmor);
            _startingArmor = Mathf.Clamp(startingArmor, 0f, _maxArmor);
            _resetAfterDeath = resetAfterDeath;
            ResetState();
        }

        public void Configure(
            GameBalanceConfig balance,
            bool resetAfterDeath = true)
        {
            if (balance == null)
            {
                Configure(100f, 50f, resetAfterDeath, 0f);
                return;
            }

            _armorAbsorptionFraction = balance.ArmorAbsorptionFraction;
            Configure(
                balance.PlayerMaxHealth,
                balance.PlayerMaxArmor,
                resetAfterDeath,
                balance.PlayerStartingArmor);
        }

        public void SetAutoReset(bool enabled, float delay = 2f)
        {
            _resetAfterDeath = enabled;
            _resetDelay = Mathf.Max(0.1f, delay);
        }

        public void SetInvulnerable(bool invulnerable)
        {
            _manualInvulnerable = invulnerable;
        }

        public void GrantInvulnerability(float seconds)
        {
            float duration = Mathf.Max(0f, seconds);
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + duration);
            if (duration > 0f)
            {
                InvulnerabilityGranted?.Invoke(duration);
            }
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, GameObject instigator)
        {
            if (!IsAlive || IsInvulnerable || amount <= 0f)
            {
                return;
            }

            float healthBefore = CurrentHealth;
            float armorAbsorption = CurrentArmor > 0f
                ? Mathf.Min(CurrentArmor, amount * _armorAbsorptionFraction)
                : 0f;
            CurrentArmor -= armorAbsorption;
            float healthDamage = amount - armorAbsorption;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - healthDamage);
            float appliedDamage = healthBefore - CurrentHealth;
            LastInstigator = instigator;
            LastHitPoint = hitPoint;
            Damaged?.Invoke(this, appliedDamage, hitPoint, instigator);
            _audioFeedback?.PlayImpact(appliedDamage);

            if (!IsAlive)
            {
                _resetAt = Time.time + _resetDelay;
                _audioFeedback?.PlayDeath();
                Died?.Invoke(this);
            }
        }

        public void ResetState()
        {
            CurrentHealth = _maxHealth;
            CurrentArmor = _startingArmor;
            _resetAt = 0f;
            LastInstigator = null;
            LastHitPoint = Vector3.zero;
            ResetCompleted?.Invoke(this);
        }

        public float RestoreHealth(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float before = CurrentHealth;
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            return CurrentHealth - before;
        }

        public float RestoreArmor(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float before = CurrentArmor;
            CurrentArmor = Mathf.Min(_maxArmor, CurrentArmor + amount);
            return CurrentArmor - before;
        }

        private void Awake()
        {
            _audioFeedback = GetComponent<AudioFeedback>();
            ResetState();
        }

        private void Update()
        {
            if (!IsAlive && _resetAfterDeath && Time.time >= _resetAt)
            {
                ResetState();
            }
        }
    }
}
