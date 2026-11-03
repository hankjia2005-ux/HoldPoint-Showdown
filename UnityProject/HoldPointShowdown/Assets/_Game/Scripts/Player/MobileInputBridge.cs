using UnityEngine;
using HoldPointShowdown.Gameplay;

namespace HoldPointShowdown.Player
{
    public static class MobileInputBridge
    {
        public static bool IsEnabled { get; private set; }
        public static Vector2 Move { get; private set; }
        public static bool FireHeld { get; private set; }
        public static bool SprintHeld { get; private set; }

        private static Vector2 _lookDelta;
        private static bool _jumpPressed;
        private static bool _crouchPressed;
        private static bool _reloadPressed;
        private static bool _interactPressed;
        private static bool _switchWeaponPressed;
        private static int _supplyAction = -1;

        public static void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (!enabled) ResetContinuous();
        }

        public static void SetContinuous(Vector2 move, Vector2 lookDelta, bool fire, bool sprint)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            _lookDelta += lookDelta;
            FireHeld = fire;
            SprintHeld = sprint;
        }

        public static void PressJump() => _jumpPressed = true;
        public static void PressCrouch() => _crouchPressed = true;
        public static void PressReload() => _reloadPressed = true;
        public static void PressInteract() => _interactPressed = true;
        public static void PressSwitchWeapon() => _switchWeaponPressed = true;
        public static void PressUseSupply(SupplyKind kind) => _supplyAction = (int)kind;

        public static Vector2 ConsumeLookDelta()
        {
            Vector2 value = _lookDelta;
            _lookDelta = Vector2.zero;
            return value;
        }

        public static bool ConsumeJump() => Consume(ref _jumpPressed);
        public static bool ConsumeCrouch() => Consume(ref _crouchPressed);
        public static bool ConsumeReload() => Consume(ref _reloadPressed);
        public static bool ConsumeInteract() => Consume(ref _interactPressed);
        public static bool ConsumeSwitchWeapon() => Consume(ref _switchWeaponPressed);

        public static bool TryConsumeSupply(out SupplyKind kind)
        {
            if (_supplyAction < 0)
            {
                kind = default;
                return false;
            }
            kind = (SupplyKind)_supplyAction;
            _supplyAction = -1;
            return true;
        }

        public static void ResetContinuous()
        {
            Move = Vector2.zero;
            _lookDelta = Vector2.zero;
            FireHeld = false;
            SprintHeld = false;
        }

        private static bool Consume(ref bool value)
        {
            bool result = value;
            value = false;
            return result;
        }
    }
}
