using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace ParkSeyang
{
    /// <summary>
    /// 로비 화면의 메인 로직 및 스테이지 선택을 관리하는 UI 클래스입니다.
    /// 랭킹 표시는 RankingUI 클래스로 위임하지만, 패널의 활성화 상태는 직접 관리할 수 있습니다.
    /// </summary>
    public sealed class LobbyUI : BaseUI
    {
        public override UIType UIType => UIType.Lobby;
        public override bool IsPopup => false;

        [Header("Main Menu Elements")]
        [SerializeField] private GameObject lobbyMainContent;
        [SerializeField] private TMP_Text userIdText;
        [SerializeField] private Button gameStartButton;
        [SerializeField] private Button rankingButton;
        [SerializeField] private Button helpButton;
        
        [Header("Sub Panels")]
        [SerializeField] private GameObject stageSelectionPanel;
        [SerializeField] private GameObject rankingPanel; // [복구] 인스펙터에서 관리 가능하게 추가
        [SerializeField] private GameObject helpPanel;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Close Buttons")]
        [SerializeField] private Button stageCloseButton;
        [SerializeField] private Button helpCloseButton;
        [SerializeField] private Button rankingCloseButton; // 지원 수정

        protected override void Start()
        {
            base.Start();
            InitButtonEvents();
            CloseAllSubPanels();
        }

        private void InitButtonEvents()
        {
            if (gameStartButton != null)
            {
                gameStartButton.onClick.RemoveAllListeners();
                gameStartButton.onClick.AddListener(OnGameStartClick);
            }

            if (rankingButton != null)
            {
                rankingButton.onClick.RemoveAllListeners();
                rankingButton.onClick.AddListener(OnRankingClick);
            }
            
            if (helpButton != null)
            {
                helpButton.onClick.RemoveAllListeners();
                helpButton.onClick.AddListener(OnHelpClick);
            }


            if (easyButton != null) easyButton.onClick.AddListener(() => OnDifficultySelected(0));
            if (normalButton != null) normalButton.onClick.AddListener(() => OnDifficultySelected(1));
            if (hardButton != null) hardButton.onClick.AddListener(() => OnDifficultySelected(2));

            if (stageCloseButton != null) stageCloseButton.onClick.AddListener(CloseAllSubPanels);
            if (helpCloseButton != null) helpCloseButton.onClick.AddListener(CloseAllSubPanels);
            if (rankingCloseButton != null) rankingCloseButton.onClick.AddListener(CloseAllSubPanels); //지원 수정

        }

        public void CloseAllSubPanels()
        {
            if (stageSelectionPanel != null) stageSelectionPanel.SetActive(false);
            if (rankingPanel != null) rankingPanel.SetActive(false);
            if (helpPanel != null) helpPanel.SetActive(false);

            if (lobbyMainContent != null) lobbyMainContent.SetActive(true);
        }

        #region Interaction Methods

        private void OnGameStartClick()
        {
            if (lobbyMainContent != null) lobbyMainContent.SetActive(false);
            if (stageSelectionPanel != null) stageSelectionPanel.SetActive(true);
        }

        private void OnRankingClick()
        {
            Debug.Log("[LobbyUI] Ranking Button Clicked.");
            
            if (rankingPanel != null)
            {
                rankingPanel.SetActive(true);
                // RankingUI 컴포넌트가 있다면 강제로 Open 호출하여 데이터 로드 유도
                if (rankingPanel.TryGetComponent<RankingUI>(out var rankingUI))
                {
                    rankingUI.Open();
                }
            }
            
            if (lobbyMainContent != null) lobbyMainContent.SetActive(false);
        }
        
        private void OnHelpClick()
        {
            if (lobbyMainContent != null) lobbyMainContent.SetActive(false);
            if (helpPanel != null) helpPanel.SetActive(true);
        }

        private void OnDifficultySelected(int difficultyIndex)
        {
            if (GameStatusController.Instance != null)
                GameStatusController.Instance.SelectedDifficulty = (DifficultyLevel)difficultyIndex;

            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.ToGame();
        }

        #endregion

        private void OnEnable()
        {
            if (FirebaseFirestoreManager.Instance != null)
                FirebaseFirestoreManager.Instance.OnDataUpdated += Refresh;
        }

        private void OnDisable()
        {
            if (FirebaseFirestoreManager.Instance != null)
                FirebaseFirestoreManager.Instance.OnDataUpdated -= Refresh;
        }

        public override void Open()
        {
            // [버그 수정] ZeroDarkMos 님의 제안에 따라 로비 씬이 아닐 경우 활성화를 원천 차단합니다.
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isLobbyScene = sceneName.Contains("Lobby") == true;

            if (isLobbyScene == false)
            {
                Close();
                return;
            }

            base.Open();
            CloseAllSubPanels();
            Refresh();
        }

        public override void Close()
        {
            CloseAllSubPanels();
            gameObject.SetActive(false);
        }

        public override void Refresh()
        {
            if (userIdText != null)
            {
                var data = FirebaseFirestoreManager.Instance.currentData;
                userIdText.text = (data != null && string.IsNullOrEmpty(data.userName) == false) ? data.userName : "Guest User";
            }
        }
    }
}
