using HoldPointShowdown.Combat;
using HoldPointShowdown.Core;
using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Gameplay;
using HoldPointShowdown.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldPointShowdown.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        [SerializeField] private PrototypeHitscanWeapon _weapon;
        [SerializeField] private Damageable _playerDamageable;
        [SerializeField] private PrototypeInventory _inventory;
        [SerializeField] private StrategicHoldPoint _holdPoint;

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
        private Texture2D _crosshairTexture;
        private GUIStyle _messageStyle;
        private GUIStyle _resultStyle;
        private GUIStyle _resultButtonStyle;

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
            PrototypeInventory inventory,
            StrategicHoldPoint holdPoint = null)
        {
            _match = match;
            _weapon = weapon;
            _playerDamageable = playerDamageable;
            _inventory = inventory;
            _holdPoint = holdPoint;
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
            DrawHoldPoint(width);
            DrawPlayerStatus(height);
            DrawAmmo(width, height);
            DrawCrosshair(width, height);
            DrawMatchMessage(width);
            DrawResult(width, height);

            GUI.Label(
                new Rect((width * 0.5f) - 390f, height - 38f, 780f, 24f),
                "WASD 移动  按住 Shift 疾跑  双击 S 蹲走  V 切换视角  左键射击  R 换弹  E 交互  Esc 暂停  F11 全屏",
                _smallStyle);

            if (Cursor.lockState != CursorLockMode.Locked
                && (_match == null || _match.Result == MatchResult.None))
            {
                GUI.Label(
                    new Rect((width * 0.5f) - 220f, (height * 0.5f) + 44f, 440f, 36f),
                    "点击画面重新锁定鼠标并控制准星",
                    _messageStyle);
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawPhaseBanner(float width)
        {
            string phase = _match != null ? FormatPhase(_match.CurrentPhase) : "准备中";
            float remaining = _match != null ? _match.RemainingPhaseSeconds : 0f;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.CeilToInt(remaining % 60f);
            int blue = _match != null ? _match.GetRemainingTeamMembers(TeamId.Blue) : 0;
            int red = _match != null ? _match.GetRemainingTeamMembers(TeamId.Red) : 0;
            string roster = _match != null && _match.CurrentPhase == MatchPhase.FinalBattle
                ? $"终局存活  蓝 {blue} : {red} 红"
                : $"阵容  蓝方 {_match?.GetTeamSize(TeamId.Blue) ?? 0} 人 / 红方 {_match?.GetTeamSize(TeamId.Red) ?? 0} 人";

            Rect panel = new((width * 0.5f) - 170f, 18f, 340f, 70f);
            GUI.Box(panel, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panel.x, panel.y + 9f, panel.width, 28f), phase, _phaseStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 38f, panel.width, 22f),
                $"本阶段剩余 {minutes:00}:{seconds:00}   |   {roster}",
                _smallStyle);
        }

        private void DrawPlayerStatus(float height)
        {
            Rect panel = new(20f, height - 160f, 320f, 122f);
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

            CombatantLifecycle player = _match != null ? _match.GetHumanPlayer() : null;
            string state = "状态：战斗中";
            if (player != null && player.IsEliminated)
            {
                state = "状态：永久出局 · 正在观战";
            }
            else if (player != null && player.IsWaitingToRespawn)
            {
                state = $"已返回基地 · {player.RespawnSecondsRemaining:0.0} 秒后复活";
            }
            else if (player != null && player.IsSpawnProtected)
            {
                state = "状态：复活保护 · 离开出生点或开火后失效";
            }
            else if (_inventory != null && _inventory.IsUsingConsumable)
            {
                state = "状态：正在使用补给 · 受到伤害可能中断";
            }
            else if (_match != null && _match.CurrentPhase == MatchPhase.FinalBattle && player != null)
            {
                state = $"终局命数：{player.FinalLivesRemaining}/{_match.Rules.FinalBattleLives}";
            }
            GUI.Label(new Rect(panel.x + 16f, panel.y + 93f, panel.width - 32f, 20f), state, _smallStyle);
        }

        private void DrawAmmo(float width, float height)
        {
            Rect panel = new(width - 260f, height - 160f, 240f, 122f);
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

        private void DrawHoldPoint(float width)
        {
            if (_holdPoint == null || !_holdPoint.IsActive)
            {
                return;
            }

            string owner = _holdPoint.ControllingTeam switch
            {
                TeamId.Blue => "蓝方控制",
                TeamId.Red => "红方控制",
                _ => "争夺中"
            };
            if (_holdPoint.IsContested)
            {
                owner = "双方交火 · 进度暂停";
            }

            float normalized = (_holdPoint.SignedProgress + 1f) * 0.5f;
            Rect panel = new((width * 0.5f) - 170f, 94f, 340f, 48f);
            GUI.Box(panel, GUIContent.none, _panelStyle);
            GUI.DrawTexture(new Rect(panel.x + 16f, panel.y + 28f, 308f, 7f), _darkTexture);
            GUI.DrawTexture(new Rect(panel.x + 16f, panel.y + 28f, 308f * normalized, 7f), _blueTexture);
            GUI.Label(new Rect(panel.x, panel.y + 4f, panel.width, 22f),
                $"中央据点：{owner}  蓝{_holdPoint.BlueOccupants}/红{_holdPoint.RedOccupants}", _smallStyle);
        }

        private void DrawMatchMessage(float width)
        {
            if (_match == null || string.IsNullOrWhiteSpace(_match.LatestMessage)
                || Time.time > _match.MessageVisibleUntil)
            {
                return;
            }

            GUI.Label(new Rect((width * 0.5f) - 300f, 96f, 600f, 42f), _match.LatestMessage, _messageStyle);
        }

        private void DrawResult(float width, float height)
        {
            if (_match == null || _match.Result == MatchResult.None)
            {
                return;
            }

            PauseMenuController.ReleaseCursorForUi();

            Rect panel = new((width * 0.5f) - 330f, (height * 0.5f) - 235f, 660f, 470f);
            GUI.Box(panel, GUIContent.none, _panelStyle);
            string result = _match.Result switch
            {
                MatchResult.BlueVictory => "蓝方胜利！",
                MatchResult.RedVictory => "红方胜利！",
                _ => "本局平局"
            };
            GUI.Label(new Rect(panel.x, panel.y + 24f, panel.width, 58f), result, _resultStyle);

            CombatantLifecycle player = _match.GetHumanPlayer();
            CombatantStats stats = player != null ? player.GetComponent<CombatantStats>() : null;
            CombatantStats mvp = CombatantStats.FindMvp();
            string statLine = stats != null
                ? $"淘汰  {stats.Eliminations}      输出  {stats.DamageDealt:0}      死亡  {stats.Deaths}      存活  {stats.SurvivalSeconds:0}s"
                : "淘汰  --      输出  --      死亡  --      存活  --";
            GUI.Label(new Rect(panel.x + 45f, panel.y + 105f, panel.width - 90f, 48f), statLine, _phaseStyle);
            string mvpName = mvp != null ? mvp.gameObject.name : "暂无";
            GUI.Label(new Rect(panel.x + 45f, panel.y + 170f, panel.width - 90f, 38f), $"本局 MVP：{mvpName}", _messageStyle);
            GUI.Label(new Rect(panel.x + 45f, panel.y + 218f, panel.width - 90f, 32f),
                "物资会重新刷新，双方回到初始装备", _smallStyle);

            if (GUI.Button(new Rect(panel.x + 65f, panel.y + 285f, 245f, 62f), "再来一局", _resultButtonStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (GUI.Button(new Rect(panel.x + 350f, panel.y + 285f, 245f, 62f), "返回主菜单", _resultButtonStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }
            GUI.Label(new Rect(panel.x, panel.y + 380f, panel.width, 30f), "感谢试玩《红蓝据点枪战》", _smallStyle);
        }

        private void DrawCrosshair(float width, float height)
        {
            float spread = _weapon != null && _weapon.Config != null ? _weapon.CurrentSpreadDegrees : 0f;
            float gap = 8f + (spread * 12f);
            const float length = 12f;
            const float thickness = 2.5f;
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            GUI.DrawTexture(new Rect(centerX - (thickness * 0.5f), centerY - gap - length, thickness, length), _crosshairTexture);
            GUI.DrawTexture(new Rect(centerX - (thickness * 0.5f), centerY + gap, thickness, length), _crosshairTexture);
            GUI.DrawTexture(new Rect(centerX - gap - length, centerY - (thickness * 0.5f), length, thickness), _crosshairTexture);
            GUI.DrawTexture(new Rect(centerX + gap, centerY - (thickness * 0.5f), length, thickness), _crosshairTexture);
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
            _crosshairTexture = MakeTexture(new Color(0.9f, 0.98f, 1f, 0.92f));

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
            _messageStyle = new GUIStyle(_phaseStyle)
            {
                fontSize = 18,
                normal = { textColor = new Color(1f, 0.85f, 0.25f) }
            };
            _resultStyle = new GUIStyle(_phaseStyle)
            {
                fontSize = 38,
                normal = { textColor = Color.white }
            };
            _resultButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
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
