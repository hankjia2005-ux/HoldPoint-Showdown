using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HoldPointShowdown.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _trainingButton;
        [SerializeField] private Button _quitButton;

        public Button PlayButton => _playButton;

        public void Configure(Button playButton, Button trainingButton, Button quitButton)
        {
            _playButton = playButton;
            _trainingButton = trainingButton;
            _quitButton = quitButton;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _playButton?.onClick.AddListener(() => LoadScene("ArenaTown"));
            _trainingButton?.onClick.AddListener(() => LoadScene("TestGym"));
            _quitButton?.onClick.AddListener(Quit);
        }

        private void Start()
        {
            if (_playButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_playButton.gameObject);
            }
        }

        private static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private static void Quit()
        {
            Application.Quit();
        }
    }
}
