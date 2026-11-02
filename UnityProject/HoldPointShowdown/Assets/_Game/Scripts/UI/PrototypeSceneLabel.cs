using UnityEngine;

namespace HoldPointShowdown.UI
{
    public sealed class PrototypeSceneLabel : MonoBehaviour
    {
    [SerializeField] private string _title = "HoldPoint Showdown";
    [SerializeField, TextArea] private string _message = "Prototype scene";

    public void Configure(string title, string message)
    {
        _title = title;
        _message = message;
    }

        private void OnGUI()
        {
            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle messageStyle = new(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };

            GUI.Label(new Rect(0f, Screen.height * 0.3f, Screen.width, 48f), _title, titleStyle);
            GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.4f, Screen.width * 0.6f, 120f), _message, messageStyle);
        }
    }
}
