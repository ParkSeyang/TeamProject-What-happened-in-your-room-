using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParkSeyang
{
    /// <summary>
    /// 로딩 씬에서 유저 정보를 불러오는 시각적 연출을 담당하며, 완료 후 로비로 이동하는 UI 클래스입니다.
    /// Open() 시점에 연출 및 데이터 로딩을 시작합니다.
    /// </summary>
    public sealed class LoadingUI : BaseUI
    {
        [Header("UI References")]
        [SerializeField] private Image loadingIcon;
        [SerializeField] private Animator loadingAnimator;
        [SerializeField] private Animator loadingTextAnimator;

        [Header("Settings")]
        [SerializeField] private float minLoadingDuration = 1.0f; // 최소 연출 대기 시간

        /// <summary>
        /// 비동기 로딩 작업의 생명주기를 관리하는 컨트롤러입니다.
        /// 씬이 바뀌거나 오브젝트가 파괴될 때, 실행 중인 로직(데이터 로드 등)을 안전하게 멈추기 위한 '정지 신호기' 역할을 합니다.
        /// </summary>
        private CancellationTokenSource loadingTaskController;

        public override UIType UIType => UIType.Loading;

        /// <summary>
        /// UIManager에 의해 UI가 열릴 때 호출되는 진입점입니다.
        /// </summary>
        public override void Open()
        {
            gameObject.SetActive(true);
            
            // 1. 새로운 로딩 작업을 시작하기 전에 기존 작업을 초기화합니다.
            ResetLoadingTaskController();
            
            // 2. 실제 로딩 프로세스를 비동기로 실행합니다. (토큰을 넘겨주어 제어권을 부여합니다.)
            LoadingProcessAsync(loadingTaskController.Token).Forget();
        }

        /// <summary>
        /// UI가 닫힐 때 호출되며, 진행 중인 모든 비동기 로직을 즉시 중단시킵니다.
        /// </summary>
        public override void Close()
        {
            StopAllLoadingTasks();
            gameObject.SetActive(false);
        }

        public override void Refresh() { }

        /// <summary>
        /// 데이터 로딩 및 연출 프로세스를 통합하여 처리합니다.
        /// </summary>
        /// <param name="token">작업 중단 신호를 감지하기 위한 수신기입니다.</param>
        private async UniTask LoadingProcessAsync(CancellationToken token)
        {
            // [방어 코드] 현재 씬이 실제로 'Loading' 전용 씬인지 확인합니다.
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName.Contains("Loading") == false)
            {
                return;
            }

            // 1. 최소 연출 시간 대기 설정 (시스템 흐름의 일관성을 위해 리얼타임 딜레이 사용)
            UniTask minWaitTask = UniTask.Delay(TimeSpan.FromSeconds(minLoadingDuration), delayType: DelayType.Realtime, cancellationToken: token);

            // 2. 유저 데이터 동기화 시작 (Firebase Auth에 로그인된 UID 기준)
            // [수정] 3중 방어 UID 복구 로직 가동
            string uid = FirebaseAuthManager.Instance.CurrentUser?.UserId;
            
            if (string.IsNullOrEmpty(uid) == true)
            {
                Debug.LogWarning("[LoadingUI] Firebase Auth session is null. Trying to recover UID...");
                
                // 방법 1: 이미 메모리에 로드된 데이터가 있는지 확인
                uid = FirebaseFirestoreManager.Instance.currentData?.userUID;

                if (string.IsNullOrEmpty(uid) == true)
                {
                    // 방법 2: 마지막으로 로그인을 시도했던 ID를 기억해 로컬 파일에서 UID를 역추적
                    string lastId = FirebaseAuthManager.Instance.LastAttemptedId;
                    Debug.Log($"[LoadingUI] Attempting local recovery for ID: {lastId}");
                    uid = UserDataSystem.Instance.FindUIDById(lastId);
                }
            }

            if (string.IsNullOrEmpty(uid) == true)
            {
                Debug.LogError("[LoadingUI] All UID recovery attempts failed. Returning to login.");
                HandleMissingUserSession();
                return;
            }

            Debug.Log($"[LoadingUI] UID Confirmed for Data Sync: {uid}");

            // [핵심] 고도화된 매니저를 통해 로컬/서버 데이터를 동기화하며 로드 (타임아웃 추가)
            UniTask<UserData> dataSyncTask = FirebaseFirestoreManager.Instance.LoadUserDataAsync(uid);

            try
            {
                // [안정성 강화] 데이터 동기화 대기 시간을 15초로 늘려 네트워크 지연에 대비합니다.
                await UniTask.WhenAll(minWaitTask, dataSyncTask).Timeout(TimeSpan.FromSeconds(15));
            }
            catch (OperationCanceledException)
            {
                // UniTask 타임아웃 시 발생하는 취소 예외는 의도된 것이므로 경고 대신 정보를 남깁니다.
                Debug.Log("[LoadingUI] 데이터 동기화가 지연되어 로컬 데이터를 우선 사용하여 로비로 이동합니다.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoadingUI] 데이터 동기화 실패 (로비 진입 강제): {e.Message}");
            }

            // 4. 모든 데이터 준비가 완료되었거나 타임아웃 되었으므로 로비로 이동합니다.
            NavigateToLobby();

            // 5. 로딩 프로세스가 완전히 종료되었으므로 UI를 정리합니다.
            FinalizeLoadingSequence();
        }

        /// <summary>
        /// 기존에 실행 중이던 로딩 컨트롤러를 파괴하고 새롭게 생성합니다.
        /// </summary>
        private void ResetLoadingTaskController()
        {
            // 이미 작동 중인 컨트롤러가 있다면 정지 신호를 보내고 메모리에서 해제합니다.
            loadingTaskController?.Cancel();
            loadingTaskController?.Dispose();

            // 새로운 작업을 위한 컨트롤러를 생성하되, 오브젝트가 파괴될 때도 자동으로 멈추도록 연결합니다.
            loadingTaskController = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// 현재 작동 중인 모든 로딩 관련 비동기 작업을 안전하게 중단합니다.
        /// </summary>
        private void StopAllLoadingTasks()
        {
            loadingTaskController?.Cancel();
            loadingTaskController?.Dispose();
            loadingTaskController = null;
        }

        /// <summary>
        /// 유저 세션 정보가 없을 경우 에러를 출력하고 로그인 화면으로 돌려보냅니다.
        /// </summary>
        private void HandleMissingUserSession()
        {
            Debug.LogError("[LoadingUI] 로그인 정보(UID)를 찾을 수 없습니다. 타이틀로 돌아갑니다.");
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ToLogin();
            }
        }

        /// <summary>
        /// 데이터 로드가 완료된 후 로비 씬으로 안전하게 전환합니다.
        /// </summary>
        private void NavigateToLobby()
        {
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ToLobby();
            }
            else
            {
                // 씬 매니저가 없을 경우 직접 로드 (방어 코드)
                SceneManager.LoadScene("02_TestLobbyScene");
            }
        }

        /// <summary>
        /// 모든 로딩 시퀀스가 성공적으로 완료된 후 UI 상태를 최종 정리합니다.
        /// </summary>
        private void FinalizeLoadingSequence()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetUIActive(UIType.Loading, false);
            }
        }

        // 오브젝트가 파괴될 때 혹시 남아있을지 모르는 모든 작업을 최종적으로 멈춥니다.
        private void OnDestroy() => StopAllLoadingTasks();
    }
}
