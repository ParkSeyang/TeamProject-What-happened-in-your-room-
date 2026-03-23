using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace ParkSeyang
{
    /// <summary>
    /// 게임 종료 후 결과(최종 점수)를 보여주고 재시작 또는 로비 이동을 처리하는 UI입니다.
    /// </summary>
    public class ResultUI : BaseUI
    {
        public override UIType UIType => UIType.Result;
        public override bool IsPopup => true;

        [Header("UI Components")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button restartButton;

        protected override void Awake()
        {
            base.Awake();
            InitButtonEvents();
        }

        private void InitButtonEvents()
        {
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClick);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClick);
        }

        public override void Open()
        {
            base.Open();
            
            // [데이터 로드] 최종 점수 반영
            if (GameStatusController.Instance != null)
            {
                finalScoreText.text = $"Score : {GameStatusController.Instance.CurrentScore}";
            }
        }

        /// <summary>
        /// 로비로 돌아가기 버튼 클릭 시 호출
        /// </summary>
        private async void OnMenuClick()
        {
            // 중복 클릭 방지
            if (menuButton != null) menuButton.interactable = false;
            if (restartButton != null) restartButton.interactable = false;

            // 1. [데이터 저장] ResultState의 저장 로직 호출 (Firebase 랭킹 갱신 포함)
            if (InGameSystem.Instance != null && InGameSystem.Instance.CurrentState is ResultState resultState)
            {
                // UI 피드백: 텍스트 변경으로 저장 중임을 알림
                if (finalScoreText != null) finalScoreText.text = "Updating Ranking...";
                
                await resultState.SaveResultAsync();
                
                if (finalScoreText != null) finalScoreText.text = "Sync Complete!";
            }

            // 2. 씬 전환 전 데이터 연동 대기 (TimeSpan 활용으로 에러 해결)
            await UniTask.Delay(System.TimeSpan.FromMilliseconds(500), delayType: DelayType.Realtime);

            if (GameSceneManager.Instance != null)
            {
                // 3. UI 비활성화 및 로비 이동
                UIManager.Instance.SetUIActive(UIType.Result, false);
                GameSceneManager.Instance.ToLobby();
            }
        }

        /// <summary>
        /// 게임 재시작 버튼 클릭 시 호출
        /// </summary>
        private void OnRestartClick()
        {
            // 1. 현재 결과 UI 끄기
            UIManager.Instance.SetUIActive(UIType.Result, false);

            // 2. 인게임 시스템을 초기화(스캔) 상태로 되돌림
            if (InGameSystem.Instance != null)
            {
                InGameSystem.Instance.ChangeState<InitializeState>();
            }

            // 3. 스캔 UI 다시 활성화
            UIManager.Instance.SetUIActive(UIType.ARPlaneScan, true);
            
            Debug.Log("[ResultUI] Game Restarted. Transitioning to InitializeState (Scan Phase).");
        }
    }
}
