using HoldPointShowdown.Core;
using UnityEngine;

namespace HoldPointShowdown.Player
{
    [DisallowMultipleComponent]
    public sealed class TeamMember : MonoBehaviour
    {
        [SerializeField] private TeamId _team = TeamId.None;

        public TeamId Team => _team;

        public void Configure(TeamId team)
        {
            _team = team;
        }
    }
}
