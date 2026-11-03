using System.Collections.Generic;
using HoldPointShowdown.Combat;
using UnityEngine;

namespace HoldPointShowdown.GameFlow
{
    [RequireComponent(typeof(Damageable))]
    [DisallowMultipleComponent]
    public sealed class CombatantStats : MonoBehaviour
    {
        private static readonly List<CombatantStats> Registry = new();
        private Damageable _damageable;
        private float _startedAt;

        public int Eliminations { get; private set; }
        public int Deaths { get; private set; }
        public float DamageDealt { get; private set; }
        public float SurvivalSeconds => Mathf.Max(0f, Time.time - _startedAt);
        public int Score => Mathf.RoundToInt(DamageDealt) + (Eliminations * 120) - (Deaths * 25);

        public static CombatantStats FindMvp()
        {
            CombatantStats best = null;
            foreach (CombatantStats candidate in Registry)
            {
                if (candidate != null && (best == null || candidate.Score > best.Score))
                {
                    best = candidate;
                }
            }
            return best;
        }

        public void ResetStats()
        {
            Eliminations = 0;
            Deaths = 0;
            DamageDealt = 0f;
            _startedAt = Time.time;
        }

        private void Awake()
        {
            _damageable = GetComponent<Damageable>();
            ResetStats();
        }

        private void OnEnable()
        {
            if (!Registry.Contains(this)) Registry.Add(this);
            _damageable ??= GetComponent<Damageable>();
            _damageable.Damaged += HandleDamaged;
            _damageable.Died += HandleDied;
        }

        private void OnDisable()
        {
            Registry.Remove(this);
            if (_damageable != null)
            {
                _damageable.Damaged -= HandleDamaged;
                _damageable.Died -= HandleDied;
            }
        }

        private void HandleDamaged(Damageable target, float amount, Vector3 _, GameObject instigator)
        {
            CombatantStats attacker = instigator != null ? instigator.GetComponent<CombatantStats>() : null;
            if (attacker == null || attacker == this)
            {
                return;
            }

            attacker.DamageDealt += Mathf.Max(0f, amount);
            if (!target.IsAlive)
            {
                attacker.Eliminations++;
            }
        }

        private void HandleDied(Damageable _)
        {
            Deaths++;
        }
    }
}
