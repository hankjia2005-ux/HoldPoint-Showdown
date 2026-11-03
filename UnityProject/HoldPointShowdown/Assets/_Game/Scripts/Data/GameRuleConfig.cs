using HoldPointShowdown.Core;
using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "GameRuleConfig", menuName = "HoldPoint Showdown/Game Rule Config")]
    public sealed class GameRuleConfig : ScriptableObject
    {
        [Header("Match Timeline")]
        [SerializeField, Min(1f)] private float _scavengeEndsAt = 120f;
        [SerializeField, Min(1f)] private float _raidEndsAt = 270f;
        [SerializeField, Min(1f)] private float _matchEndsAt = 540f;

        [Header("Respawn")]
        [SerializeField, Min(0f)] private float _scavengeRespawnDelay = 0.75f;
        [SerializeField, Min(0f)] private float _raidRespawnDelay = 5f;
        [SerializeField, Min(0f)] private float _finalRespawnDelay = 5f;
        [SerializeField, Min(1)] private int _finalBattleLives = 2;

        public float ScavengeEndsAt => _scavengeEndsAt;
        public float RaidEndsAt => _raidEndsAt;
        public float MatchEndsAt => _matchEndsAt;
        public float ScavengeRespawnDelay => _scavengeRespawnDelay;
        public float RaidRespawnDelay => _raidRespawnDelay;
        public float FinalRespawnDelay => _finalRespawnDelay;
        public int FinalBattleLives => _finalBattleLives;
        public MatchPhase ResolvePhase(float elapsedSeconds)
        {
            if (elapsedSeconds < _scavengeEndsAt)
            {
                return MatchPhase.Scavenge;
            }

            if (elapsedSeconds < _raidEndsAt)
            {
                return MatchPhase.Raid;
            }

            return elapsedSeconds < _matchEndsAt
                ? MatchPhase.FinalBattle
                : MatchPhase.Complete;
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

        public void SetDemoValues(
            float scavengeEndsAt = 120f,
            float raidEndsAt = 270f,
            float matchEndsAt = 540f,
            float scavengeRespawnDelay = 0.75f,
            float raidRespawnDelay = 5f,
            float finalRespawnDelay = 5f,
            int finalBattleLives = 2)
        {
            _scavengeEndsAt = Mathf.Max(1f, scavengeEndsAt);
            _raidEndsAt = Mathf.Max(_scavengeEndsAt + 0.01f, raidEndsAt);
            _matchEndsAt = Mathf.Max(_raidEndsAt + 0.01f, matchEndsAt);
            _scavengeRespawnDelay = Mathf.Max(0f, scavengeRespawnDelay);
            _raidRespawnDelay = Mathf.Max(0f, raidRespawnDelay);
            _finalRespawnDelay = Mathf.Max(0f, finalRespawnDelay);
            _finalBattleLives = Mathf.Max(1, finalBattleLives);
        }

        private void OnValidate()
        {
            _scavengeEndsAt = Mathf.Max(1f, _scavengeEndsAt);
            _raidEndsAt = Mathf.Max(_scavengeEndsAt + 1f, _raidEndsAt);
            _matchEndsAt = Mathf.Max(_raidEndsAt + 1f, _matchEndsAt);
            _scavengeRespawnDelay = Mathf.Max(0f, _scavengeRespawnDelay);
            _raidRespawnDelay = Mathf.Max(0f, _raidRespawnDelay);
            _finalRespawnDelay = Mathf.Max(0f, _finalRespawnDelay);
            _finalBattleLives = Mathf.Max(1, _finalBattleLives);
        }
    }
}
