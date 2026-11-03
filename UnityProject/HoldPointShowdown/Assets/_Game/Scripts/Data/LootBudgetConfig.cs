using UnityEngine;

namespace HoldPointShowdown.Data
{
    [CreateAssetMenu(fileName = "LootBudgetConfig", menuName = "HoldPoint Showdown/Loot Budget Config")]
    public sealed class LootBudgetConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float _itemsPerPlayer = 7.2f;
        [SerializeField, Range(0f, 1f)] private float _initialShare = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _phaseOneShare = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _phaseTwoShare = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _phaseThreeShare = 0.1f;
        [SerializeField, Min(1f)] private float _pointsPerPlayer = 4.8f;
        [SerializeField, Min(1f)] private float _worldCapMultiplier = 1.6f;

        public float ItemsPerPlayer => _itemsPerPlayer;
        public float InitialShare => _initialShare;
        public float PhaseOneShare => _phaseOneShare;
        public float PhaseTwoShare => _phaseTwoShare;
        public float PhaseThreeShare => _phaseThreeShare;
        public float PointsPerPlayer => _pointsPerPlayer;
        public float WorldCapMultiplier => _worldCapMultiplier;

        public void SetRecommendedValues()
        {
            _itemsPerPlayer = 7.2f;
            _initialShare = 0.6f;
            _phaseOneShare = 0.15f;
            _phaseTwoShare = 0.15f;
            _phaseThreeShare = 0.1f;
            _pointsPerPlayer = 4.8f;
            _worldCapMultiplier = 1.6f;
        }
    }
}
