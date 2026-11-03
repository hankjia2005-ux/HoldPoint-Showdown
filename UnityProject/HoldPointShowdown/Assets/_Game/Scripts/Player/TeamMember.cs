using HoldPointShowdown.Core;
using HoldPointShowdown.Runtime;
using UnityEngine;

namespace HoldPointShowdown.Player
{
    [DisallowMultipleComponent]
    public sealed class TeamMember : MonoBehaviour
    {
        [SerializeField] private TeamId _team = TeamId.None;
        [SerializeField] private CombatantRuntimeSet _runtimeSet;

        public TeamId Team => _team;

        public void Configure(TeamId team, CombatantRuntimeSet runtimeSet = null)
        {
            _runtimeSet?.Remove(this);
            _team = team;
            _runtimeSet = runtimeSet;
            if (isActiveAndEnabled)
            {
                _runtimeSet?.Add(this);
            }
        }

        private void OnEnable()
        {
            _runtimeSet?.Add(this);
        }

        private void OnDisable()
        {
            _runtimeSet?.Remove(this);
        }
    }
}
