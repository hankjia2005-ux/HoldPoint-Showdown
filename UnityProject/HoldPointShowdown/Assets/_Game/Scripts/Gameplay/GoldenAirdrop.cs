using HoldPointShowdown.Core;
using HoldPointShowdown.Data;
using HoldPointShowdown.GameFlow;
using UnityEngine;

namespace HoldPointShowdown.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GoldenAirdrop : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private WeaponConfig _goldenWeapon;
        [SerializeField] private Vector3 _dropPosition = new(0f, 1.2f, 0f);
        private bool _deployed;

        public bool IsDeployed => _deployed;

        public void Configure(MatchPhaseController match, WeaponConfig goldenWeapon, Vector3 dropPosition)
        {
            _match = match;
            _goldenWeapon = goldenWeapon;
            _dropPosition = dropPosition;
        }

        private void OnEnable()
        {
            if (_match != null) _match.PhaseChanged += HandlePhaseChanged;
        }

        private void Start()
        {
            if (_match == null) _match = Object.FindAnyObjectByType<MatchPhaseController>();
            if (_match != null)
            {
                _match.PhaseChanged -= HandlePhaseChanged;
                _match.PhaseChanged += HandlePhaseChanged;
                if (_match.CurrentPhase == MatchPhase.FinalBattle) Deploy();
            }
        }

        private void OnDisable()
        {
            if (_match != null) _match.PhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(MatchPhase _, MatchPhase current)
        {
            if (current == MatchPhase.FinalBattle) Deploy();
        }

        public void Deploy()
        {
            if (_deployed || _goldenWeapon == null) return;
            _deployed = true;
            WorldItemPickup.CreateWeaponDrop(
                _goldenWeapon,
                _goldenWeapon.MagazineSize,
                _goldenWeapon.StartingReserveAmmo,
                _dropPosition + (Vector3.up * 0.8f),
                0f,
                0f);
            WorldItemPickup.CreateSupplyDrop(SupplyKind.AmmoPack, 2, _dropPosition + new Vector3(1.6f, 0f, 0f));
            WorldItemPickup.CreateSupplyDrop(SupplyKind.ArmorPlate, 2, _dropPosition + new Vector3(-1.6f, 0f, 0f));
            _match?.RaiseMessage("终局金色空投已落在中央广场！", 6f);
        }
    }
}
