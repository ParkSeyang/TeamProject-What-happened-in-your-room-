using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketAR.AR
{
    public class CaptureView : MonoBehaviour
    {
        private string statusMessage = "Swipe to Catch!";
        private bool isSyncing = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Monster"))
            {
                collision.gameObject.GetComponent<Monster>().OnCaught();
                OnMonsterHit().Forget();
            }
        }

        private async UniTaskVoid OnMonsterHit()
        {
            if (isSyncing) return;
            isSyncing = true;

            // Reset ball physics
            GetComponent<BallPhysicsController>().ResetBall();

            statusMessage = "Catching...";
            await UniTask.Delay(1500);

            if (Random.value > 0.3f)
            {
                statusMessage = "Success! Syncing...";
            }
            else
            {
                statusMessage = "Escaped! Try Again.";
                isSyncing = false;
            }
        }

        private void OnGUI()
        {
            float scaleX = Screen.width / 1080f;
            float scaleY = Screen.height / 1920f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1));

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 70,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            // Status Message Display
            GUI.Label(new Rect(0, 100, 1080, 200), statusMessage, labelStyle);
            
            if (GUI.Button(new Rect(50, 1750, 200, 100), "Exit", new GUIStyle(GUI.skin.button) { fontSize = 40 }))
            {
                SceneManager.LoadScene("Lobby_Scene");
            }
        }
    }
}