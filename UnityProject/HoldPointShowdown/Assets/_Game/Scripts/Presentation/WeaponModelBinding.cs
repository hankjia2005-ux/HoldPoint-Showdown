using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WeaponModelBinding : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;

        public Transform Muzzle => _muzzle;

        public void Configure(Transform muzzle)
        {
            _muzzle = muzzle;
        }
    }
}
