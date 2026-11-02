using HoldPointShowdown.Combat;
using HoldPointShowdown.GameFlow;
using UnityEngine;

namespace HoldPointShowdown.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
    [SerializeField] private MatchPhaseController _match;
    [SerializeField] private PrototypeHitscanWeapon _weapon;
    [SerializeField] private Damageable _playerDamageable;

    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;

    public void Configure(
        MatchPhaseController match,
        PrototypeHitscanWeapon weapon,
        Damageable playerDamageable)
    {
        _match = match;
        _weapon = weapon;
        _playerDamageable = playerDamageable;
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.Box(new Rect(16f, 16f, 330f, 168f), GUIContent.none, _panelStyle);
        GUI.Label(new Rect(30f, 26f, 300f, 28f), "HOLDPOINT SHOWDOWN · PROTOTYPE", _titleStyle);

        string phase = _match != null ? _match.CurrentPhase.ToString() : "Not started";
        float elapsed = _match != null ? _match.ElapsedSeconds : 0f;
        string ammo = _weapon != null ? $"{_weapon.MagazineAmmo} / {_weapon.ReserveAmmo}" : "--";
        string health = _playerDamageable != null
            ? $"{_playerDamageable.CurrentHealth:0} + {_playerDamageable.CurrentArmor:0} armor"
            : "--";

        GUI.Label(new Rect(30f, 60f, 300f, 22f), $"Phase: {phase}   Time: {elapsed:0.0}s");
        GUI.Label(new Rect(30f, 84f, 300f, 22f), $"Health: {health}");
        GUI.Label(new Rect(30f, 108f, 300f, 22f), $"Ammo: {ammo}");
        GUI.Label(new Rect(30f, 138f, 300f, 32f), "WASD Move · Mouse Aim · LMB Fire · R Reload");

        GUI.Label(
            new Rect((Screen.width * 0.5f) - 8f, (Screen.height * 0.5f) - 14f, 16f, 28f),
            "+",
            _titleStyle);
    }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = Texture2D.grayTexture;
            _panelStyle.normal.textColor = Color.white;
            _panelStyle.padding = new RectOffset(12, 12, 12, 12);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft
            };
        }
    }
}
