using HoldPointShowdown.Core;
using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "GameRuleConfig", menuName = "HoldPoint Showdown/Game Rule Config")]
    public sealed class GameRuleConfig : ScriptableObject
    {
    [Header("Match Timeline")]
    [SerializeField, Min(1f)] private float _scavengeEndsAt = 180f;
    [SerializeField, Min(1f)] private float _raidEndsAt = 300f;
    [SerializeField, Min(1f)] private float _matchEndsAt = 720f;

    [Header("Respawn")]
    [SerializeField, Min(0f)] private float _raidRespawnDelay = 5f;
    [SerializeField, Min(1)] private int _finalBattleLives = 2;

    [Header("Player")]
    [SerializeField, Min(1f)] private float _playerMaxHealth = 100f;
    [SerializeField, Min(0f)] private float _playerMaxArmor = 50f;
    [SerializeField, Min(1)] private int _backpackSlots = 12;

    public float ScavengeEndsAt => _scavengeEndsAt;
    public float RaidEndsAt => _raidEndsAt;
    public float MatchEndsAt => _matchEndsAt;
    public float RaidRespawnDelay => _raidRespawnDelay;
    public int FinalBattleLives => _finalBattleLives;
    public float PlayerMaxHealth => _playerMaxHealth;
    public float PlayerMaxArmor => _playerMaxArmor;
    public int BackpackSlots => _backpackSlots;

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
        return phase == MatchPhase.Raid ? _raidRespawnDelay : 0f;
    }

        private void OnValidate()
        {
            _scavengeEndsAt = Mathf.Max(1f, _scavengeEndsAt);
            _raidEndsAt = Mathf.Max(_scavengeEndsAt + 1f, _raidEndsAt);
            _matchEndsAt = Mathf.Max(_raidEndsAt + 1f, _matchEndsAt);
            _raidRespawnDelay = Mathf.Max(0f, _raidRespawnDelay);
            _finalBattleLives = Mathf.Max(1, _finalBattleLives);
            _playerMaxHealth = Mathf.Max(1f, _playerMaxHealth);
            _playerMaxArmor = Mathf.Max(0f, _playerMaxArmor);
            _backpackSlots = Mathf.Max(1, _backpackSlots);
        }
    }
}
