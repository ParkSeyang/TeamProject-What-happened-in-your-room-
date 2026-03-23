using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParkSeyang
{
    /// <summary>
    /// 프로젝트의 모든 씬 전환을 관리하는 중앙 매니저입니다.
    /// 문자열 오타 방지를 위해 Enum 기반의 전환 시스템을 제공합니다.
    /// </summary>
    public enum SceneType
    {
        Login = 0,    // 00_TestLoginScene
        Loading = 1,  // 01_TestLoadingScene
        Lobby = 2,    // 02_TestLobbyScene
        Game = 3      // 03_TestGameScene
    }

    public class GameSceneManager : SingletonBase<GameSceneManager>
    {
        public bool IsLevelLoading { get; private set; }

        // 씬 이름 정의 (빌드 세팅과 일치해야 함)
        private const string SCENE_LOGIN = "00_TestLoginScene";
        private const string SCENE_LOADING = "01_TestLoadingScene";
        private const string SCENE_LOBBY = "02_TestLobbyScene";
        private const string SCENE_GAME = "03_TestGameScene";

        /// <summary>
        /// 특정 씬 타입으로 이동합니다.
        /// </summary>
        public void LoadScene(SceneType type)
        {
            string targetName = GetSceneName(type);
            if (string.IsNullOrEmpty(targetName) == true || IsLevelLoading == true) return;

            StartCoroutine(RoutineLoadScene(targetName));
        }

        private string GetSceneName(SceneType type) => type switch
        {
            SceneType.Login => SCENE_LOGIN,
            SceneType.Loading => SCENE_LOADING,
            SceneType.Lobby => SCENE_LOBBY,
            SceneType.Game => SCENE_GAME,
            _ => string.Empty
        };

        private IEnumerator RoutineLoadScene(string sceneName)
        {
            if (IsLevelLoading == true) yield break;
            
            IsLevelLoading = true;
            Debug.Log($"[GameSceneManager] Starting to load scene: {sceneName}");

            // 1. 로드 시작 시 UI 상태 정리 (UIManager 연동)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetAllInGameUIActive(false);
            }

            // 2. 비동기 씬 로드 시작
            AsyncOperation asyncOp = null;
            try
            {
                asyncOp = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameSceneManager] Scene load exception: {e.Message}");
            }

            if (asyncOp != null)
            {
                // 로드가 완료될 때까지 대기
                while (asyncOp.isDone == false)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogError($"[GameSceneManager] Failed to start loading scene: {sceneName}. Is it in Build Settings?");
            }

            IsLevelLoading = false;

            // 3. 로드 완료 후 UI 상태 복구
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetAllInGameUIActive(true);
            }

            Debug.Log($"[GameSceneManager] Successfully finished loading sequence: {sceneName}");
        }

        // 편의를 위한 래퍼 메서드들
        public void ToLogin() => LoadScene(SceneType.Login);
        public void ToLoading() => LoadScene(SceneType.Loading);
        
        /// <summary>
        /// 로비 씬으로 이동하면서 모든 인게임 데이터를 초기화합니다.
        /// </summary>
        public void ToLobby()
        {
            // [추가] 로비로 돌아갈 때 모든 게임 진행 데이터(스캔, 점수 등)를 깨끗하게 비웁니다.
            if (GameStatusController.IsInitialized == true)
            {
                GameStatusController.Instance.ResetStatus();
            }

            LoadScene(SceneType.Lobby);
        }

        public void ToGame() => LoadScene(SceneType.Game);
    }
}
