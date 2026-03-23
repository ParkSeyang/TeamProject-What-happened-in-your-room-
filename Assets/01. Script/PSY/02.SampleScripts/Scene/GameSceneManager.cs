using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketAR.Scene
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        public string SelectedMonsterId { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        public async UniTask LoadARSceneWithRandomMonster()
        {
            string[] monsterIds = { "M001", "M002", "M003" };
            this.SelectedMonsterId = monsterIds[Random.Range(0, monsterIds.Length)];

            await SceneManager.LoadSceneAsync("AR_Catch_Scene").ToUniTask();
        }

        public async UniTask LoadLobbyScene()
        {
            await SceneManager.LoadSceneAsync("Lobby_Scene").ToUniTask();
        }
    }
}