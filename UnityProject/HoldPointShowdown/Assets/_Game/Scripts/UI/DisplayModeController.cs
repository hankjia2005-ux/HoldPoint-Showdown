using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldPointShowdown.UI
{
    [DisallowMultipleComponent]
    public sealed class DisplayModeController : MonoBehaviour
    {
        private const int WindowedWidth = 1600;
        private const int WindowedHeight = 900;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeController()
        {
            if (Object.FindAnyObjectByType<DisplayModeController>() != null)
            {
                return;
            }

            GameObject root = new("显示模式控制器");
            DontDestroyOnLoad(root);
            root.AddComponent<DisplayModeController>();
        }

        private void Awake()
        {
            Application.runInBackground = true;
            if (!Application.isEditor)
            {
                SetFullscreen(true);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool altEnter = keyboard.enterKey.wasPressedThisFrame
                && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            if (keyboard.f11Key.wasPressedThisFrame || altEnter)
            {
                SetFullscreen(Screen.fullScreenMode == FullScreenMode.Windowed);
            }
        }

        private static void SetFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                Screen.SetResolution(
                    Display.main.systemWidth,
                    Display.main.systemHeight,
                    FullScreenMode.FullScreenWindow);
            }
            else
            {
                int width = Mathf.Min(WindowedWidth, Display.main.systemWidth - 80);
                int height = Mathf.Min(WindowedHeight, Display.main.systemHeight - 80);
                Screen.SetResolution(Mathf.Max(960, width), Mathf.Max(540, height), FullScreenMode.Windowed);
            }
        }
    }
}
