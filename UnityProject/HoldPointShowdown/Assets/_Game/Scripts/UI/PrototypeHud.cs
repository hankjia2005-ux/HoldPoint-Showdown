using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Player;
using UnityEngine;

namespace HoldPointShowdown.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private PrototypeHitscanWeapon _weapon;
        [SerializeField] private Damageable _playerDamageable;
        [SerializeField] private PrototypeInventory _inventory;

        private GUIStyle _panelStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _phaseStyle;
        private GUIStyle _crosshairStyle;
        private Texture2D _panelTexture;
        private Texture2D _blueTexture;
        private Texture2D _armorTexture;
        private Texture2D _darkTexture;

        public void Configure(
            MatchPhaseController match,
            PrototypeHitscanWeapon weapon,
            Damageable playerDamageable)
        {
            Configure(match, weapon, playerDamageable, null);
        }

        public void Configure(
            MatchPhaseController match,
            PrototypeHitscanWeapon weapon,
            Damageable playerDamageable,
            PrototypeInventory inventory)
        {
            _match = match;
            _weapon = weapon;
            _playerDamageable = playerDamageable;
            _inventory = inventory;
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Clamp(Screen.height / 900f, 0.75f, 1.25f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawPhaseBanner(width);
            DrawPlayerStatus(height);
            DrawAmmo(width, height);
            DrawCrosshair(width, height);

            GUI.Label(
                new Rect((width * 0.5f) - 310f, height - 38f, 620f, 24f),
                "WASD 移动  空格 跳跃  左键 射击  R 换弹  1/2 切枪  G 丢枪  Z/X/C 物资  E 物资箱",
                _smallStyle);

            GUI.matrix = previousMatrix;
        }

        private void DrawPhaseBanner(float width)
        {
            string phase = _match != null ? FormatPhase(_match.CurrentPhase) : "准备中";
            float elapsed = _match != null ? _match.ElapsedSeconds : 0f;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);

            Rect panel = new((width * 0.5f) - 170f, 18f, 340f, 70f);
            GUI.Box(panel, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panel.x, panel.y + 9f, panel.width, 28f), phase, _phaseStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 38f, panel.width, 22f),
                $"{minutes:00}:{seconds:00}   |   淘汰红方全员",
                _smallStyle);
        }

        private void DrawPlayerStatus(float height)
        {
            Rect panel = new(20f, height - 142f, 300f, 104f);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            float health = _playerDamageable != null ? _playerDamageable.CurrentHealth : 0f;
            float maxHealth = _playerDamageable != null ? _playerDamageable.MaxHealth : 100f;
            float armor = _playerDamageable != null ? _playerDamageable.CurrentArmor : 0f;
            float maxArmor = _playerDamageable != null ? Mathf.Max(1f, _playerDamageable.MaxArmor) : 100f;

            GUI.Label(new Rect(panel.x + 16f, panel.y + 12f, 120f, 24f), "蓝方 // 01", _labelStyle);
            GUI.Label(new Rect(panel.x + 220f, panel.y + 12f, 64f, 24f), health.ToString("0"), _valueStyle);
            DrawBar(new Rect(panel.x + 16f, panel.y + 43f, 268f, 16f), health / maxHealth, _blueTexture);

            GUI.Label(new Rect(panel.x + 16f, panel.y + 69f, 80f, 20f), "护甲", _smallStyle);
            DrawBar(new Rect(panel.x + 90f, panel.y + 72f, 194f, 10f), armor / maxArmor, _armorTexture);
        }

        private void DrawAmmo(float width, float height)
        {
            Rect panel = new(width - 260f, height - 142f, 240f, 104f);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            string magazine = _weapon != null ? _weapon.MagazineAmmo.ToString("00") : "--";
            string reserve = _weapon != null ? _weapon.ReserveAmmo.ToString("000") : "---";
            GUI.Label(new Rect(panel.x + 16f, panel.y + 8f, 208f, 35f), $"{magazine}  /  {reserve}", _phaseStyle);
            string weaponName = _weapon != null && _weapon.Config != null
                ? _weapon.Config.DisplayName
                : "未装备武器";
            if (_weapon != null && _weapon.IsReloading)
            {
                weaponName += "  ·  换弹中";
            }
            GUI.Label(new Rect(panel.x + 16f, panel.y + 40f, 208f, 20f), weaponName, _smallStyle);
            string supplies = _inventory != null
                ? $"背包 {_inventory.UsedSlots}/{_inventory.Capacity}   弹{_inventory.AmmoPacks}  医{_inventory.Medkits}  甲{_inventory.ArmorPlates}"
                : "背包 --";
            GUI.Label(new Rect(panel.x + 16f, panel.y + 69f, 208f, 20f), supplies, _smallStyle);
        }

        private void DrawCrosshair(float width, float height)
        {
            GUI.Label(new Rect((width * 0.5f) - 15f, (height * 0.5f) - 20f, 30f, 40f), "+", _crosshairStyle);
        }

        private void DrawBar(Rect rect, float fill, Texture2D texture)
        {
            GUI.DrawTexture(rect, _darkTexture);
            Rect filled = rect;
            filled.width *= Mathf.Clamp01(fill);
            GUI.DrawTexture(filled, texture);
        }

        private static string FormatPhase(MatchPhase phase)
        {
            return phase switch
            {
                MatchPhase.Scavenge => "阶段一 · 物资发育",
                MatchPhase.Raid => "阶段二 · 攻防争夺",
                MatchPhase.FinalBattle => "阶段三 · 终局决战",
                MatchPhase.Complete => "对局结束",
                _ => "准备中"
            };
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelTexture = MakeTexture(new Color(0.035f, 0.055f, 0.09f, 0.9f));
            _blueTexture = MakeTexture(new Color(0.12f, 0.68f, 1f, 1f));
            _armorTexture = MakeTexture(new Color(0.45f, 0.92f, 1f, 1f));
            _darkTexture = MakeTexture(new Color(0.06f, 0.09f, 0.13f, 0.95f));

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = _panelTexture;
            _panelStyle.normal.textColor = Color.white;
            _panelStyle.padding = new RectOffset(12, 12, 12, 12);

            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = new Color(0.72f, 0.82f, 0.9f) },
                alignment = TextAnchor.MiddleCenter
            };
            _labelStyle = new GUIStyle(_smallStyle)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.32f, 0.78f, 1f) }
            };
            _valueStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };
            _phaseStyle = new GUIStyle(_valueStyle)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            _crosshairStyle = new GUIStyle(_phaseStyle)
            {
                fontSize = 28,
                normal = { textColor = new Color(0.9f, 0.98f, 1f, 0.9f) }
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
