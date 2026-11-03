using System.Collections.Generic;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using UnityEngine;

namespace HoldPointShowdown.Gameplay
{
    public readonly struct LootDrop
    {
        public LootDrop(SupplyKind supply)
        {
            Supply = supply;
            Weapon = null;
        }

        public LootDrop(WeaponConfig weapon)
        {
            Supply = default;
            Weapon = weapon;
        }

        public SupplyKind Supply { get; }
        public WeaponConfig Weapon { get; }
        public bool IsWeapon => Weapon != null;
    }

    [DisallowMultipleComponent]
    public sealed class LootDirector : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private LootBudgetConfig _config;
        [SerializeField] private WeaponConfig[] _weapons;
        [SerializeField, Min(0)] private int _initialStaticItems = 9;

        private readonly List<WorldItemPickup> _pickups = new();
        private int _totalBudget;
        private int _availableBudget;
        private bool _initialized;

        public int TotalBudget => _totalBudget;
        public int AvailableBudget => _availableBudget;
        public MatchPhase CurrentPhase => _match != null ? _match.CurrentPhase : MatchPhase.Scavenge;

        public void Configure(
            MatchPhaseController match,
            LootBudgetConfig config,
            WeaponConfig[] weapons,
            int initialStaticItems = 9)
        {
            _match = match;
            _config = config;
            _weapons = weapons;
            _initialStaticItems = Mathf.Max(0, initialStaticItems);
        }

        public IReadOnlyList<LootDrop> RequestDrops(int requested)
        {
            EnsureInitialized();
            int activeWorldItems = CountAvailablePickups();
            int worldCap = Mathf.CeilToInt(_totalBudget * (_config != null ? _config.WorldCapMultiplier : 1.6f));
            int count = Mathf.Min(Mathf.Max(0, requested), _availableBudget, Mathf.Max(0, worldCap - activeWorldItems));
            List<LootDrop> drops = new(count);
            for (int index = 0; index < count; index++)
            {
                drops.Add(RollDrop(CurrentPhase));
            }
            _availableBudget -= count;
            return drops;
        }

        public void Register(WorldItemPickup pickup)
        {
            if (pickup != null && !_pickups.Contains(pickup))
            {
                _pickups.Add(pickup);
            }
        }

        public void Unregister(WorldItemPickup pickup)
        {
            _pickups.Remove(pickup);
        }

        public WorldItemPickup FindNearestAvailable(Vector3 position, float maxDistance)
        {
            WorldItemPickup best = null;
            float bestDistance = maxDistance * maxDistance;
            for (int index = _pickups.Count - 1; index >= 0; index--)
            {
                WorldItemPickup pickup = _pickups[index];
                if (pickup == null)
                {
                    _pickups.RemoveAt(index);
                    continue;
                }
                if (!pickup.IsAvailable)
                {
                    continue;
                }
                float distance = (pickup.transform.position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = pickup;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void OnEnable()
        {
            if (_match != null)
            {
                _match.PhaseChanged += HandlePhaseChanged;
            }
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (_match != null)
            {
                _match.PhaseChanged -= HandlePhaseChanged;
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            _totalBudget = _match != null && _match.Rules != null
                ? _match.Rules.LootBudget
                : Mathf.RoundToInt(6 * (_config != null ? _config.ItemsPerPlayer : 7.2f));
            float initialShare = _config != null ? _config.InitialShare : 0.6f;
            _availableBudget = Mathf.Max(0, Mathf.RoundToInt(_totalBudget * initialShare) - _initialStaticItems);
        }

        private void HandlePhaseChanged(MatchPhase _, MatchPhase next)
        {
            EnsureInitialized();
            float share = next switch
            {
                MatchPhase.Scavenge => _config != null ? _config.PhaseOneShare : 0.15f,
                MatchPhase.Raid => _config != null ? _config.PhaseTwoShare : 0.15f,
                MatchPhase.FinalBattle => _config != null ? _config.PhaseThreeShare : 0.1f,
                _ => 0f
            };
            if (next != MatchPhase.Complete)
            {
                _availableBudget += Mathf.RoundToInt(_totalBudget * share);
            }
        }

        private LootDrop RollDrop(MatchPhase phase)
        {
            float[] weights = phase switch
            {
                MatchPhase.Raid => new[] { 0.04f, 0.10f, 0.12f, 0.04f, 0.35f, 0.20f, 0.15f },
                MatchPhase.FinalBattle => new[] { 0f, 0.06f, 0.14f, 0.10f, 0.30f, 0.20f, 0.20f },
                _ => new[] { 0.10f, 0.13f, 0.05f, 0.01f, 0.41f, 0.18f, 0.12f }
            };
            float roll = Random.value;
            float cumulative = 0f;
            for (int index = 0; index < weights.Length; index++)
            {
                cumulative += weights[index];
                if (roll > cumulative)
                {
                    continue;
                }
                if (index <= 3)
                {
                    WeaponConfig weapon = FindWeapon((ItemRarity)index);
                    return weapon != null ? new LootDrop(weapon) : new LootDrop(SupplyKind.AmmoPack);
                }
                return new LootDrop((SupplyKind)(index - 4));
            }
            return new LootDrop(SupplyKind.AmmoPack);
        }

        private WeaponConfig FindWeapon(ItemRarity rarity)
        {
            if (_weapons == null)
            {
                return null;
            }
            foreach (WeaponConfig weapon in _weapons)
            {
                if (weapon != null && weapon.Rarity == rarity)
                {
                    return weapon;
                }
            }
            return null;
        }

        private int CountAvailablePickups()
        {
            int count = 0;
            for (int index = _pickups.Count - 1; index >= 0; index--)
            {
                if (_pickups[index] == null)
                {
                    _pickups.RemoveAt(index);
                }
                else if (_pickups[index].IsAvailable)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
