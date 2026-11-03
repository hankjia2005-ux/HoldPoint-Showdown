using System;
using System.Collections;
using System.Collections.Generic;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using UnityEngine;

namespace HoldPointShowdown.GameFlow
{
    public sealed class MatchPhaseController : MonoBehaviour
    {
        [SerializeField] private GameRuleConfig _rules;
        [SerializeField] private bool _startOnAwake = true;
        [SerializeField] private List<CombatantLifecycle> _combatants = new();

        private float _matchStartedAt;
        private Coroutine _resultCheckRoutine;

        public event Action<MatchPhase, MatchPhase> PhaseChanged;
        public event Action<MatchResult> MatchEnded;
        public event Action<string> MessageRaised;

        public MatchPhase CurrentPhase { get; private set; } = MatchPhase.Scavenge;
        public MatchResult Result { get; private set; } = MatchResult.None;
        public float ElapsedSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public GameRuleConfig Rules => _rules;
        public string LatestMessage { get; private set; } = string.Empty;
        public float MessageVisibleUntil { get; private set; }
        public IReadOnlyList<CombatantLifecycle> Combatants => _combatants;

        public float RemainingPhaseSeconds
        {
            get
            {
                if (_rules == null)
                {
                    return 0f;
                }

                float end = CurrentPhase switch
                {
                    MatchPhase.Scavenge => _rules.ScavengeEndsAt,
                    MatchPhase.Raid => _rules.RaidEndsAt,
                    MatchPhase.FinalBattle => _rules.MatchEndsAt,
                    _ => ElapsedSeconds
                };
                return Mathf.Max(0f, end - ElapsedSeconds);
            }
        }

        public void Configure(GameRuleConfig rules)
        {
            _rules = rules;
        }

        public void RegisterCombatant(CombatantLifecycle combatant)
        {
            if (combatant != null && !_combatants.Contains(combatant))
            {
                _combatants.Add(combatant);
            }
        }

        public void StartMatch()
        {
            if (_rules == null)
            {
                Debug.LogError("Cannot start match without a GameRuleConfig.", this);
                return;
            }

            RefreshCombatants();
            foreach (CombatantLifecycle combatant in _combatants)
            {
                combatant?.InitializeForMatch();
                combatant?.GetComponent<CombatantStats>()?.ResetStats();
            }

            _matchStartedAt = Time.time;
            ElapsedSeconds = 0f;
            CurrentPhase = MatchPhase.Scavenge;
            Result = MatchResult.None;
            IsRunning = true;
            PhaseChanged?.Invoke(MatchPhase.Complete, MatchPhase.Scavenge);
            RaiseMessage("物资发育开始：搜集装备并及时存入基地", 4f);
        }

        public void ReportDeath(CombatantLifecycle combatant)
        {
            if (!IsRunning || combatant == null)
            {
                return;
            }

            if (CurrentPhase == MatchPhase.FinalBattle)
            {
                int remaining = combatant.ConsumeFinalLife();
                if (remaining <= 0)
                {
                    combatant.Eliminate();
                    RaiseMessage($"{FormatTeam(combatant.Team)}成员永久出局", 3f);
                    QueueResultCheck();
                    return;
                }

                RaiseMessage($"终局命数 -1，剩余 {remaining} 条", 3f);
            }
            else
            {
                RaiseMessage(
                    CurrentPhase == MatchPhase.Scavenge
                        ? "装备已掉落，正在基地重新部署"
                        : "装备已掉落，5 秒后在基地复活",
                    3f);
            }

            combatant.BeginRespawn(_rules.GetRespawnDelay(CurrentPhase));
        }

        public int GetRemainingTeamMembers(TeamId team)
        {
            int count = 0;
            foreach (CombatantLifecycle combatant in _combatants)
            {
                if (combatant != null && combatant.Team == team && !combatant.IsEliminated)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetTeamSize(TeamId team)
        {
            int count = 0;
            foreach (CombatantLifecycle combatant in _combatants)
            {
                if (combatant != null && combatant.Team == team)
                {
                    count++;
                }
            }

            return count;
        }

        public CombatantLifecycle GetHumanPlayer()
        {
            return _combatants.Find(combatant => combatant != null && combatant.IsHumanPlayer);
        }

        public void RaiseMessage(string message, float duration = 3f)
        {
            LatestMessage = message ?? string.Empty;
            MessageVisibleUntil = Time.time + Mathf.Max(0.1f, duration);
            MessageRaised?.Invoke(LatestMessage);
        }

        private void Start()
        {
            if (_startOnAwake)
            {
                StartMatch();
            }
        }

        private void Update()
        {
            if (!IsRunning || _rules == null)
            {
                return;
            }

            ElapsedSeconds = Time.time - _matchStartedAt;
            MatchPhase nextPhase = _rules.ResolvePhase(ElapsedSeconds);
            if (nextPhase == MatchPhase.Complete)
            {
                EndMatch(MatchResult.Draw);
                return;
            }

            if (nextPhase != CurrentPhase)
            {
                ChangePhase(nextPhase);
            }
        }

        private void ChangePhase(MatchPhase nextPhase)
        {
            MatchPhase previousPhase = CurrentPhase;
            CurrentPhase = nextPhase;

            if (nextPhase == MatchPhase.Raid)
            {
                RaiseMessage("保护罩已消失：基地可被入侵", 5f);
            }
            else if (nextPhase == MatchPhase.FinalBattle)
            {
                foreach (CombatantLifecycle combatant in _combatants)
                {
                    combatant?.PrepareForFinalBattle(_rules.FinalBattleLives);
                }

                RaiseMessage("终局之战开启：每人仅剩 2 条命", 5f);
            }

            PhaseChanged?.Invoke(previousPhase, nextPhase);
        }

        private void QueueResultCheck()
        {
            if (_resultCheckRoutine == null)
            {
                _resultCheckRoutine = StartCoroutine(CheckResultAfterFrame());
            }
        }

        private IEnumerator CheckResultAfterFrame()
        {
            yield return null;
            _resultCheckRoutine = null;

            int blue = GetRemainingTeamMembers(TeamId.Blue);
            int red = GetRemainingTeamMembers(TeamId.Red);
            if (blue <= 0 && red <= 0)
            {
                EndMatch(MatchResult.Draw);
            }
            else if (red <= 0)
            {
                EndMatch(MatchResult.BlueVictory);
            }
            else if (blue <= 0)
            {
                EndMatch(MatchResult.RedVictory);
            }
        }

        private void EndMatch(MatchResult result)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            Result = result;
            MatchPhase previous = CurrentPhase;
            CurrentPhase = MatchPhase.Complete;
            foreach (CombatantLifecycle combatant in _combatants)
            {
                combatant?.StopForMatchEnd();
            }

            RaiseMessage(result switch
            {
                MatchResult.BlueVictory => "蓝方胜利",
                MatchResult.RedVictory => "红方胜利",
                _ => "本局平局"
            }, 999f);
            PhaseChanged?.Invoke(previous, MatchPhase.Complete);
            MatchEnded?.Invoke(result);
        }

        private void RefreshCombatants()
        {
            _combatants.Clear();
            _combatants.AddRange(UnityEngine.Object.FindObjectsByType<CombatantLifecycle>(FindObjectsInactive.Exclude));
        }

        private static string FormatTeam(TeamId team)
        {
            return team == TeamId.Blue ? "蓝方" : "红方";
        }
    }
}
