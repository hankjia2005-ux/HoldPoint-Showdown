using System;
using UnityEngine;

namespace HoldPointShowdown.Runtime
{
    public enum CameraDirectiveType
    {
        DeathFocus,
        ReturnToPlayer,
        Spectate
    }

    public readonly struct CameraDirective
    {
        public CameraDirective(CameraDirectiveType type, Transform target, float duration)
        {
            Type = type;
            Target = target;
            Duration = duration;
        }

        public CameraDirectiveType Type { get; }
        public Transform Target { get; }
        public float Duration { get; }
    }

    [CreateAssetMenu(fileName = "CameraDirectiveChannel", menuName = "HoldPoint Showdown/Events/Camera Directive Channel")]
    public sealed class CameraDirectiveChannel : ScriptableObject
    {
        public event Action<CameraDirective> Raised;

        public void RaiseDeathFocus(Transform killer, float duration)
        {
            Raised?.Invoke(new CameraDirective(CameraDirectiveType.DeathFocus, killer, duration));
        }

        public void RaiseReturnToPlayer(Transform player)
        {
            Raised?.Invoke(new CameraDirective(CameraDirectiveType.ReturnToPlayer, player, 0f));
        }

        public void RaiseSpectate(Transform target)
        {
            Raised?.Invoke(new CameraDirective(CameraDirectiveType.Spectate, target, 0f));
        }

        private void OnDisable()
        {
            Raised = null;
        }
    }
}
