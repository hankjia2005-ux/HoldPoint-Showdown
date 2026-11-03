using HoldPointShowdown.Core;
using HoldPointShowdown.Combat;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using HoldPointShowdown.Runtime;
using UnityEngine;

namespace HoldPointShowdown.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StrategicHoldPoint : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private CombatantRuntimeSet _combatants;
        [SerializeField] private SharedSupplyChest _blueChest;
        [SerializeField] private SharedSupplyChest _redChest;
        [SerializeField, Min(1f)] private float _radius = 7f;
        [SerializeField, Min(1f)] private float _captureSeconds = 9f;
        [SerializeField, Min(3f)] private float _supplyInterval = 15f;

        private float _signedProgress;
        private float _nextSupplyAt;
        private int _supplyIndex;

        public TeamId ControllingTeam { get; private set; } = TeamId.None;
        public bool IsContested { get; private set; }
        public int BlueOccupants { get; private set; }
        public int RedOccupants { get; private set; }
        public float SignedProgress => _signedProgress;
        public float Radius => _radius;
        public bool IsActive => _match != null
            && _match.IsRunning
            && (_match.CurrentPhase == MatchPhase.Raid || _match.CurrentPhase == MatchPhase.FinalBattle);

        public void Configure(
            MatchPhaseController match,
            CombatantRuntimeSet combatants,
            SharedSupplyChest blueChest,
            SharedSupplyChest redChest)
        {
            _match = match;
            _combatants = combatants;
            _blueChest = blueChest;
            _redChest = redChest;
        }

        public void ConfigureTimings(float captureSeconds, float supplyInterval)
        {
            _captureSeconds = Mathf.Max(0.05f, captureSeconds);
            _supplyInterval = Mathf.Max(0.1f, supplyInterval);
        }

        private void Update()
        {
            if (!IsActive)
            {
                BlueOccupants = 0;
                RedOccupants = 0;
                IsContested = false;
                return;
            }

            CountOccupants();
            IsContested = BlueOccupants > 0 && RedOccupants > 0;
            if (!IsContested && BlueOccupants != RedOccupants)
            {
                float direction = BlueOccupants > 0 ? 1f : -1f;
                float strength = Mathf.Clamp(Mathf.Abs(BlueOccupants - RedOccupants), 1, 3);
                _signedProgress = Mathf.Clamp(
                    _signedProgress + (direction * strength * Time.deltaTime / _captureSeconds),
                    -1f,
                    1f);
                ResolveControlChange();
            }

            if (ControllingTeam != TeamId.None && Time.time >= _nextSupplyAt)
            {
                AwardSupply();
                _nextSupplyAt = Time.time + _supplyInterval;
            }
        }

        private void CountOccupants()
        {
            BlueOccupants = 0;
            RedOccupants = 0;
            float radiusSquared = _radius * _radius;
            if (_combatants != null)
            {
                _combatants.RemoveMissingEntries();
                if (_combatants.Count > 0)
                {
                    foreach (TeamMember member in _combatants.Items)
                    {
                        CountOccupant(member, radiusSquared);
                    }
                    return;
                }
            }

            if (_match == null)
            {
                return;
            }

            foreach (CombatantLifecycle lifecycle in _match.Combatants)
            {
                CountOccupant(lifecycle != null ? lifecycle.GetComponent<TeamMember>() : null, radiusSquared);
            }
        }

        private void CountOccupant(TeamMember member, float radiusSquared)
        {
            if (member == null || (member.transform.position - transform.position).sqrMagnitude > radiusSquared)
            {
                return;
            }

            Damageable damageable = member.GetComponent<Damageable>();
            CombatantLifecycle lifecycle = member.GetComponent<CombatantLifecycle>();
            if (damageable == null || !damageable.IsAlive || (lifecycle != null && lifecycle.IsEliminated))
            {
                return;
            }

            if (member.Team == TeamId.Blue)
            {
                BlueOccupants++;
            }
            else if (member.Team == TeamId.Red)
            {
                RedOccupants++;
            }
        }

        private void ResolveControlChange()
        {
            if (ControllingTeam == TeamId.Blue && _signedProgress <= 0f)
            {
                LoseControl(TeamId.Blue);
            }
            else if (ControllingTeam == TeamId.Red && _signedProgress >= 0f)
            {
                LoseControl(TeamId.Red);
            }

            TeamId next = _signedProgress >= 1f
                ? TeamId.Blue
                : _signedProgress <= -1f
                    ? TeamId.Red
                    : TeamId.None;
            if (next == TeamId.None || next == ControllingTeam)
            {
                return;
            }

            ControllingTeam = next;
            _nextSupplyAt = Time.time + 3f;
            _match?.RaiseMessage($"{(next == TeamId.Blue ? "蓝方" : "红方")}已控制中央据点：补给开始运往基地", 4f);
        }

        private void LoseControl(TeamId previous)
        {
            ControllingTeam = TeamId.None;
            _nextSupplyAt = float.PositiveInfinity;
            _match?.RaiseMessage($"{(previous == TeamId.Blue ? "蓝方" : "红方")}已失去中央据点控制", 3f);
        }

        private void AwardSupply()
        {
            SupplyKind kind = (SupplyKind)(_supplyIndex % 3);
            _supplyIndex++;
            SharedSupplyChest chest = ControllingTeam == TeamId.Blue ? _blueChest : _redChest;
            if (chest != null && chest.AddSupply(kind, 1) > 0)
            {
                _match?.RaiseMessage($"据点补给：{FormatSupply(kind)}已送达{(ControllingTeam == TeamId.Blue ? "蓝方" : "红方")}基地", 3f);
            }
        }

        private static string FormatSupply(SupplyKind kind)
        {
            return kind switch
            {
                SupplyKind.AmmoPack => "弹药包",
                SupplyKind.Medkit => "医疗包",
                _ => "护甲片"
            };
        }
    }
}
