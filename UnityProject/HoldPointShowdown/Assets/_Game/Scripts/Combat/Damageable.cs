using System;
using UnityEngine;

namespace HoldPointShowdown.Combat
{
    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour, IDamageable
    {
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField, Min(0f)] private float _maxArmor = 0f;
    [SerializeField] private bool _resetAfterDeath = true;
    [SerializeField, Min(0.1f)] private float _resetDelay = 2f;

    private float _resetAt;

    public event Action<Damageable> Died;
    public event Action<Damageable> ResetCompleted;
    public event Action<Damageable, float, Vector3, GameObject> Damaged;

    public float CurrentHealth { get; private set; }
    public float CurrentArmor { get; private set; }
    public float MaxHealth => _maxHealth;
    public float MaxArmor => _maxArmor;
    public bool IsAlive => CurrentHealth > 0f;

    public void Configure(float maxHealth, float maxArmor, bool resetAfterDeath = true)
    {
        _maxHealth = Mathf.Max(1f, maxHealth);
        _maxArmor = Mathf.Max(0f, maxArmor);
        _resetAfterDeath = resetAfterDeath;
        ResetState();
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, GameObject instigator)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        float healthBefore = CurrentHealth;
        float armorBefore = CurrentArmor;
        float armorDamage = Mathf.Min(CurrentArmor, amount);
        CurrentArmor -= armorDamage;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - (amount - armorDamage));
        float appliedDamage = (healthBefore - CurrentHealth) + (armorBefore - CurrentArmor);
        Damaged?.Invoke(this, appliedDamage, hitPoint, instigator);

        if (!IsAlive)
        {
            _resetAt = Time.time + _resetDelay;
            Died?.Invoke(this);
        }
    }

    public void ResetState()
    {
        CurrentHealth = _maxHealth;
        CurrentArmor = _maxArmor;
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
