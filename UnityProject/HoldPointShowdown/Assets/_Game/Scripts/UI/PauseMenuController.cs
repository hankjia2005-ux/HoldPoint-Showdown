using HoldPointShowdown.GameFlow;
using HoldPointShowdown.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HoldPointShowdown.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private MatchPhaseController _match;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _backdrop;

        public static bool IsPaused { get; private set; }
        public static bool IsMatchEnded { get; private set; }
        public static bool ShouldCaptureGameplayCursor => !IsPaused && !IsMatchEnded;

        public static void TogglePaused()
        {
            if (IsMatchEnded)
            {
                ReleaseCursorForUi();
                return;
            }

            SetPaused(!IsPaused);
        }

        public static void ReleaseCursorForUi()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Configure(MatchPhaseController match)
        {
            _match = match;
        }

        public void ProcessEscape()
        {
            if (_match != null && _match.Result != MatchResult.None)
            {
                ReleaseCursorForUi();
                return;
            }

            TogglePaused();
        }

        private void Awake()
        {
            IsPaused = false;
            IsMatchEnded = _match != null && _match.Result != MatchResult.None;
            Time.timeScale = 1f;
            if (IsMatchEnded)
            {
                ReleaseCursorForUi();
            }
        }

        private void OnEnable()
        {
            if (_match != null) _match.MatchEnded += HandleMatchEnded;
        }

        private void OnDisable()
        {
            if (_match != null) _match.MatchEnded -= HandleMatchEnded;
            IsPaused = false;
            IsMatchEnded = false;
            Time.timeScale = 1f;
            ReleaseCursorForUi();
        }

        private void Update()
        {
            if (_match != null && _match.Result != MatchResult.None && !IsMatchEnded)
            {
                HandleMatchEnded(_match.Result);
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ProcessEscape();
            }
        }

        private void HandleMatchEnded(MatchResult _)
        {
            IsPaused = false;
            IsMatchEnded = true;
            Time.timeScale = 1f;
            ReleaseCursorForUi();
        }

        private static void SetPaused(bool paused)
        {
            if (IsMatchEnded)
            {
                IsPaused = false;
                Time.timeScale = 1f;
                ReleaseCursorForUi();
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        private void OnGUI()
        {
            if (!IsPaused) return;
            EnsureStyles();
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _backdrop);
            float width = Mathf.Min(460f, Screen.width * 0.8f);
            Rect panel = new((Screen.width - width) * 0.5f, (Screen.height - 430f) * 0.5f, width, 430f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 38f, panel.width, 58f), "游戏已暂停", _titleStyle);
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 125f, panel.width - 140f, 52f), "继续游戏", _buttonStyle)) SetPaused(false);
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 195f, panel.width - 140f, 52f), "重新开始", _buttonStyle)) ReloadCurrent();
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 265f, panel.width - 140f, 52f), "返回主菜单", _buttonStyle)) LoadMainMenu();
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 335f, panel.width - 140f, 52f), "退出游戏", _buttonStyle)) Quit();
        }

        private static void ReloadCurrent()
        {
            SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static void LoadMainMenu()
        {
            SetPaused(false);
            SceneManager.LoadScene("MainMenu");
        }

        private static void Quit()
        {
            SetPaused(false);
            Application.Quit();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _backdrop = MakeTexture(new Color(0.01f, 0.025f, 0.05f, 0.82f));
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
