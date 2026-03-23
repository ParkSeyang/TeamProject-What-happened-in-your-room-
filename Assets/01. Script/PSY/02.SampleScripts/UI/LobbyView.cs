using Cysharp.Threading.Tasks;
using PocketAR.Scene;
using UnityEngine;

namespace PocketAR.UI
{
    public class LobbyView : MonoBehaviour
    {
        private string userInfo = "Loading User Info...";
        private bool isBusy = false;

        private void Start()
        {
            
        }

        private void OnGUI()
        {
            // Set GUI scale for 1080x1920 (Portrait)
            float scaleX = Screen.width / 1080f;
            float scaleY = Screen.height / 1920f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1));

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 50, alignment = TextAnchor.MiddleCenter };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 60 };

            // User Info Label
            GUI.Label(new Rect(0, 200, 1080, 100), userInfo, labelStyle);

            // Start Button
            GUI.enabled = !isBusy;
            if (GUI.Button(new Rect(290, 800, 500, 150), "Start AR Catch", buttonStyle))
            {
                OnStartARButtonClicked().Forget();
            }
            GUI.enabled = true;
        }

        private async UniTaskVoid OnStartARButtonClicked()
        {
            isBusy = true;
            await GameSceneManager.Instance.LoadARSceneWithRandomMonster();
            isBusy = false;
        }
    }
}