using System;
using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using UnityEngine;

namespace HoldPointShowdown.GameFlow
{
    public sealed class MatchPhaseController : MonoBehaviour
    {
    [SerializeField] private GameRuleConfig _rules;
    [SerializeField] private bool _startOnAwake = true;

    private float _matchStartedAt;

    public event Action<MatchPhase, MatchPhase> PhaseChanged;

    public MatchPhase CurrentPhase { get; private set; } = MatchPhase.Scavenge;
    public float ElapsedSeconds { get; private set; }
    public bool IsRunning { get; private set; }
    public GameRuleConfig Rules => _rules;

    public void Configure(GameRuleConfig rules)
    {
        _rules = rules;
    }

    public void StartMatch()
    {
        if (_rules == null)
        {
            Debug.LogError("Cannot start match without a GameRuleConfig.", this);
            return;
        }

        _matchStartedAt = Time.time;
        ElapsedSeconds = 0f;
        CurrentPhase = _rules.ResolvePhase(0f);
        IsRunning = true;
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

            if (nextPhase != CurrentPhase)
            {
                MatchPhase previousPhase = CurrentPhase;
                CurrentPhase = nextPhase;
                PhaseChanged?.Invoke(previousPhase, nextPhase);
            }

            if (CurrentPhase == MatchPhase.Complete)
            {
                IsRunning = false;
            }
        }
    }
}
