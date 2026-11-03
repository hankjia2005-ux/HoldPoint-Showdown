using System;
using System.Collections.Generic;
using HoldPointShowdown.Core;
using UnityEngine;

namespace HoldPointShowdown.Data
{
    [Serializable]
    public struct MatchPreset
    {
        [Min(2)] public int TeamSize;
        [Min(1f)] public float ScavengeEndsAt;
        [Min(1f)] public float RaidEndsAt;
        [Min(1f)] public float MatchEndsAt;
        [Min(1)] public int LootPointCount;
        [Min(1)] public int LootBudget;

        public MatchPreset(
            int teamSize,
            float scavengeEndsAt,
            float raidEndsAt,
            float matchEndsAt,
            int lootPointCount,
            int lootBudget)
        {
            TeamSize = teamSize;
            ScavengeEndsAt = scavengeEndsAt;
            RaidEndsAt = raidEndsAt;
            MatchEndsAt = matchEndsAt;
            LootPointCount = lootPointCount;
            LootBudget = lootBudget;
        }
    }

    [CreateAssetMenu(fileName = "GameRuleConfig", menuName = "HoldPoint Showdown/Game Rule Config")]
    public sealed class GameRuleConfig : ScriptableObject
    {
        [Header("Match Presets")]
        [SerializeField, Range(2, 5)] private int _activeTeamSize = 3;
        [SerializeField] private MatchPreset[] _presets = CreateRecommendedPresets();

        [Header("Runtime / Custom Room Override")]
        [SerializeField, Min(1f)] private float _scavengeEndsAt = 120f;
        [SerializeField, Min(1f)] private float _raidEndsAt = 240f;
        [SerializeField, Min(1f)] private float _matchEndsAt = 540f;
        [SerializeField] private bool _useCustomOverride;

        [Header("Respawn")]
        [SerializeField, Min(0f)] private float _scavengeRespawnDelay = 0.5f;
        [SerializeField, Min(0f)] private float _raidRespawnDelay = 4f;
        [SerializeField, Min(0f)] private float _finalRespawnDelay = 5f;
        [SerializeField, Min(0f)] private float _scavengeSpawnProtection = 1.5f;
        [SerializeField, Min(0f)] private float _raidSpawnProtection = 2f;
        [SerializeField, Min(0f)] private float _finalSpawnProtection = 2.5f;
        [SerializeField, Min(1f)] private float _spawnProtectionRadius = 3f;
        [SerializeField, Range(1, 5)] private int _spawnPointCount = 3;
        [SerializeField, Min(1)] private int _finalBattleLives = 2;

        public int ActiveTeamSize => _activeTeamSize;
        public IReadOnlyList<MatchPreset> Presets => _presets;
        public float ScavengeEndsAt => _useCustomOverride ? _scavengeEndsAt : ActivePreset.ScavengeEndsAt;
        public float RaidEndsAt => _useCustomOverride ? _raidEndsAt : ActivePreset.RaidEndsAt;
        public float MatchEndsAt => _useCustomOverride ? _matchEndsAt : ActivePreset.MatchEndsAt;
        public int LootPointCount => ActivePreset.LootPointCount;
        public int LootBudget => ActivePreset.LootBudget;
        public float ScavengeRespawnDelay => _scavengeRespawnDelay;
        public float RaidRespawnDelay => _raidRespawnDelay;
        public float FinalRespawnDelay => _finalRespawnDelay;
        public float SpawnProtectionRadius => _spawnProtectionRadius;
        public int SpawnPointCount => _spawnPointCount;
        public int FinalBattleLives => _finalBattleLives;

        private MatchPreset ActivePreset
        {
            get
            {
                if (_presets != null)
                {
                    foreach (MatchPreset preset in _presets)
                    {
                        if (preset.TeamSize == _activeTeamSize)
                        {
                            return preset;
                        }
                    }
                }

                return new MatchPreset(3, 120f, 240f, 540f, 29, 43);
            }
        }

        public MatchPhase ResolvePhase(float elapsedSeconds)
        {
            if (elapsedSeconds < ScavengeEndsAt) return MatchPhase.Scavenge;
            if (elapsedSeconds < RaidEndsAt) return MatchPhase.Raid;
            return elapsedSeconds < MatchEndsAt ? MatchPhase.FinalBattle : MatchPhase.Complete;
        }

        public float GetRespawnDelay(MatchPhase phase)
        {
            return phase switch
            {
                MatchPhase.Scavenge => _scavengeRespawnDelay,
                MatchPhase.Raid => _raidRespawnDelay,
                MatchPhase.FinalBattle => _finalRespawnDelay,
                _ => 0f
            };
        }

        public float GetSpawnProtectionSeconds(MatchPhase phase)
        {
            return phase switch
            {
                MatchPhase.Scavenge => _scavengeSpawnProtection,
                MatchPhase.Raid => _raidSpawnProtection,
                MatchPhase.FinalBattle => _finalSpawnProtection,
                _ => 0f
            };
        }

        public void SelectTeamSize(int teamSize)
        {
            _activeTeamSize = Mathf.Clamp(teamSize, 2, 5);
            _useCustomOverride = false;
        }

        public void SetRecommendedPresets(int activeTeamSize = 3)
        {
            _presets = CreateRecommendedPresets();
            SelectTeamSize(activeTeamSize);
            SetRespawnDefaults();
        }

        public void SetDemoValues(
            float scavengeEndsAt = 120f,
            float raidEndsAt = 240f,
            float matchEndsAt = 540f,
            float scavengeRespawnDelay = 0.5f,
            float raidRespawnDelay = 4f,
            float finalRespawnDelay = 5f,
            int finalBattleLives = 2)
        {
            _scavengeEndsAt = Mathf.Max(0.01f, scavengeEndsAt);
            _raidEndsAt = Mathf.Max(_scavengeEndsAt + 0.01f, raidEndsAt);
            _matchEndsAt = Mathf.Max(_raidEndsAt + 0.01f, matchEndsAt);
            _scavengeRespawnDelay = Mathf.Max(0f, scavengeRespawnDelay);
            _raidRespawnDelay = Mathf.Max(0f, raidRespawnDelay);
            _finalRespawnDelay = Mathf.Max(0f, finalRespawnDelay);
            _finalBattleLives = Mathf.Max(1, finalBattleLives);
            _useCustomOverride = true;
        }

        private void OnValidate()
        {
            if (_presets == null || _presets.Length != 4)
            {
                _presets = CreateRecommendedPresets();
            }
            _activeTeamSize = Mathf.Clamp(_activeTeamSize, 2, 5);
            _scavengeEndsAt = Mathf.Max(0.01f, _scavengeEndsAt);
            _raidEndsAt = Mathf.Max(_scavengeEndsAt + 0.01f, _raidEndsAt);
            _matchEndsAt = Mathf.Max(_raidEndsAt + 0.01f, _matchEndsAt);
            _scavengeRespawnDelay = Mathf.Max(0f, _scavengeRespawnDelay);
            _raidRespawnDelay = Mathf.Max(0f, _raidRespawnDelay);
            _finalRespawnDelay = Mathf.Max(0f, _finalRespawnDelay);
            _spawnProtectionRadius = Mathf.Max(1f, _spawnProtectionRadius);
            _spawnPointCount = Mathf.Clamp(_spawnPointCount, 1, 5);
            _finalBattleLives = Mathf.Max(1, _finalBattleLives);
        }

        private void SetRespawnDefaults()
        {
            _scavengeRespawnDelay = 0.5f;
            _raidRespawnDelay = 4f;
            _finalRespawnDelay = 5f;
            _scavengeSpawnProtection = 1.5f;
            _raidSpawnProtection = 2f;
            _finalSpawnProtection = 2.5f;
            _spawnProtectionRadius = 3f;
            _spawnPointCount = 3;
            _finalBattleLives = 2;
        }

        private static MatchPreset[] CreateRecommendedPresets()
        {
            return new[]
            {
                new MatchPreset(2, 90f, 210f, 480f, 19, 29),
                new MatchPreset(3, 120f, 240f, 540f, 29, 43),
                new MatchPreset(4, 150f, 270f, 630f, 38, 58),
                new MatchPreset(5, 180f, 300f, 720f, 48, 72)
            };
        }
    }
}
