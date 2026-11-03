using HoldPointShowdown.Player;
using HoldPointShowdown.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HoldPointShowdown.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileControlsOverlay : MonoBehaviour
    {
        [SerializeField] private bool _showInEditor;
        [SerializeField, Min(40f)] private float _joystickRadius = 92f;

        private int _moveTouchId = -1;
        private int _lookTouchId = -1;
        private Vector2 _moveOrigin;
        private Vector2 _lookPrevious;
        private Vector2 _move;
        private Vector2 _lookDelta;
        private bool _fireHeld;
        private bool _sprintHeld;
        private Texture2D _padTexture;
        private Texture2D _buttonTexture;
        private GUIStyle _buttonLabel;

        public bool IsVisible => Application.isMobilePlatform || _showInEditor;

        private void OnEnable()
        {
            MobileInputBridge.SetEnabled(IsVisible);
        }

        private void OnDisable()
        {
            MobileInputBridge.SetEnabled(false);
        }

        private void Update()
        {
            if (!IsVisible || Touchscreen.current == null)
            {
                MobileInputBridge.SetEnabled(false);
                return;
            }

            MobileInputBridge.SetEnabled(true);
            _lookDelta = Vector2.zero;
            _fireHeld = false;
            _sprintHeld = false;

            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                int id = touch.touchId.ReadValue();
                if (!touch.press.isPressed)
                {
                    if (id == _moveTouchId) { _moveTouchId = -1; _move = Vector2.zero; }
                    if (id == _lookTouchId) _lookTouchId = -1;
                    continue;
                }

                Vector2 position = ToGuiPosition(touch.position.ReadValue());
                bool began = touch.press.wasPressedThisFrame;
                if (Contains(FireRect(), position)) { _fireHeld = true; continue; }
                if (Contains(SprintRect(), position)) { _sprintHeld = true; continue; }
                if (began && Contains(JumpRect(), position)) { MobileInputBridge.PressJump(); continue; }
                if (began && Contains(CrouchRect(), position)) { MobileInputBridge.PressCrouch(); continue; }
                if (began && Contains(ReloadRect(), position)) { MobileInputBridge.PressReload(); continue; }
                if (began && Contains(InteractRect(), position)) { MobileInputBridge.PressInteract(); continue; }
                if (began && Contains(SwitchRect(), position)) { MobileInputBridge.PressSwitchWeapon(); continue; }
                if (began && Contains(MedkitRect(), position)) { MobileInputBridge.PressUseSupply(SupplyKind.Medkit); continue; }
                if (began && Contains(ArmorRect(), position)) { MobileInputBridge.PressUseSupply(SupplyKind.ArmorPlate); continue; }
                if (began && Contains(PauseRect(), position)) { PauseMenuController.TogglePaused(); continue; }

                if (id == _moveTouchId)
                {
                    Vector2 delta = position - _moveOrigin;
                    _move = Vector2.ClampMagnitude(new Vector2(delta.x, -delta.y) / _joystickRadius, 1f);
                    continue;
                }
                if (id == _lookTouchId)
                {
                    Vector2 delta = position - _lookPrevious;
                    _lookDelta += new Vector2(delta.x, -delta.y) * 0.65f;
                    _lookPrevious = position;
                    continue;
                }
                if (!began) continue;

                if (position.x < Screen.width * 0.45f && position.y > Screen.height * 0.42f && _moveTouchId < 0)
                {
                    _moveTouchId = id;
                    _moveOrigin = position;
                    _move = Vector2.zero;
                }
                else if (_lookTouchId < 0)
                {
                    _lookTouchId = id;
                    _lookPrevious = position;
                }
            }

            MobileInputBridge.SetContinuous(_move, _lookDelta, _fireHeld, _sprintHeld);
        }

        private void OnGUI()
        {
            if (!IsVisible) return;
            EnsureStyles();
            DrawPad(_moveTouchId >= 0 ? _moveOrigin : new Vector2(150f, Screen.height - 155f), _move);
            DrawButton(FireRect(), "开火");
            DrawButton(SprintRect(), "疾跑");
            DrawButton(JumpRect(), "跳");
            DrawButton(CrouchRect(), "蹲");
            DrawButton(ReloadRect(), "换弹");
            DrawButton(InteractRect(), "交互");
            DrawButton(SwitchRect(), "切枪");
            DrawButton(MedkitRect(), "医疗");
            DrawButton(ArmorRect(), "护甲");
            DrawButton(PauseRect(), "暂停");
        }

        private void DrawPad(Vector2 center, Vector2 input)
        {
            Rect outer = new(center.x - _joystickRadius, center.y - _joystickRadius, _joystickRadius * 2f, _joystickRadius * 2f);
            GUI.DrawTexture(outer, _padTexture);
            Vector2 knob = center + new Vector2(input.x, -input.y) * (_joystickRadius * 0.55f);
            GUI.DrawTexture(new Rect(knob.x - 34f, knob.y - 34f, 68f, 68f), _buttonTexture);
        }

        private void DrawButton(Rect rect, string label)
        {
            GUI.DrawTexture(rect, _buttonTexture);
            GUI.Label(rect, label, _buttonLabel);
        }

        private void EnsureStyles()
        {
            if (_padTexture != null) return;
            _padTexture = MakeTexture(new Color(0.08f, 0.16f, 0.24f, 0.34f));
            _buttonTexture = MakeTexture(new Color(0.12f, 0.62f, 1f, 0.48f));
            _buttonLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private static Vector2 ToGuiPosition(Vector2 screenPosition) => new(screenPosition.x, Screen.height - screenPosition.y);
        private static bool Contains(Rect rect, Vector2 point) => rect.Contains(point);
        private static Rect FireRect() => new(Screen.width - 150f, Screen.height - 185f, 116f, 116f);
        private static Rect JumpRect() => new(Screen.width - 280f, Screen.height - 255f, 88f, 88f);
        private static Rect CrouchRect() => new(Screen.width - 380f, Screen.height - 155f, 82f, 82f);
        private static Rect ReloadRect() => new(Screen.width - 250f, Screen.height - 355f, 78f, 64f);
        private static Rect InteractRect() => new(Screen.width - 370f, Screen.height - 270f, 86f, 68f);
        private static Rect SprintRect() => new(255f, Screen.height - 285f, 92f, 68f);
        private static Rect SwitchRect() => new(Screen.width - 475f, Screen.height - 360f, 82f, 62f);
        private static Rect MedkitRect() => new(Screen.width - 570f, Screen.height - 270f, 82f, 62f);
        private static Rect ArmorRect() => new(Screen.width - 570f, Screen.height - 345f, 82f, 62f);
        private static Rect PauseRect() => new(Screen.width - 104f, 22f, 82f, 52f);

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
