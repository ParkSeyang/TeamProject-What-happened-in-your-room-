using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ParkSeyang
{
    /// <summary>
    /// 모든 UI의 생명주기와 표시 상태를 중앙에서 관리하는 매니저입니다.
    /// ZeroDarkMos 님의 기획에 따라 단일 캔버스 내의 UI들을 씬 전환 시 제어합니다.
    /// </summary>
    public class UIManager : SingletonBase<UIManager>
    {
        private Dictionary<UIType, BaseUI> uiDictionary = new Dictionary<UIType, BaseUI>();

        public bool IsPopupOpen { get; private set; }
        private bool isUIManagementEnabled = true;

        public void RegisterUI(BaseUI baseUI)
        {
            if (baseUI == null) return;

            // 중복 등록 방지 (DontDestroyOnLoad 씬에 있는 UI 우선)
            if (uiDictionary.TryGetValue(baseUI.UIType, out var existingUI) == true)
            {
                if (existingUI != null && existingUI.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    return;
                }
            }

            uiDictionary[baseUI.UIType] = baseUI;
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => OnInitialize();

        protected override void OnInitialize()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            
            // [기획 반영] 현재 씬 타입 판정
            bool isTitleScene = sceneName.Contains("Login") == true || sceneName.Contains("Title") == true;
            bool isLobbyScene = sceneName.Contains("Lobby") == true;
            bool isGameScene = sceneName.Contains("Game") == true || sceneName.Contains("Main") == true;
            
            // [버그 수정] 현재 씬 자체가 로딩 씬인지 명확히 체크하여 UI가 닫히는 것을 방지
            bool isLoadingScene = sceneName.Contains("Loading") == true;
            bool isCurrentlyLoading = (GameSceneManager.Instance != null && GameSceneManager.Instance.IsLevelLoading == true) || isLoadingScene;

            isUIManagementEnabled = (isCurrentlyLoading == false);

            // 1. 시스템 정리
            CleanUpEventSystems();
            UpdateCanvasHierarchy();
            
            // 2. 씬에 따른 초기 UI 상태 설정
            InitializeUIStates(isTitleScene, isLobbyScene, isGameScene, isCurrentlyLoading);

            if (isCurrentlyLoading == false)
            {
                RefreshUIState();
            }
        }

        private void CleanUpEventSystems()
        {
            var allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem primaryES = null;

            // 1. 이미 DDOL에 있는 것을 우선적으로 선택
            foreach (var es in allEventSystems)
            {
                if (es.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    if (primaryES == null) primaryES = es;
                    else DestroyImmediate(es.gameObject);
                }
            }

            // 2. 없다면 새로 로드된 것 중 하나를 DDOL로 승격
            foreach (var es in allEventSystems)
            {
                if (es == null || es.gameObject.scene.name == "DontDestroyOnLoad") continue;

                if (primaryES == null)
                {
                    primaryES = es;
                    DontDestroyOnLoad(es.gameObject);
                }
                else
                {
                    DestroyImmediate(es.gameObject);
                }
            }

            // [보강] EventSystem 강제 활성화 및 Input Module 갱신 유도
            if (primaryES != null)
            {
                primaryES.gameObject.SetActive(false);
                primaryES.gameObject.SetActive(true); // 껐다 켜서 모듈 재초기화 유도
                primaryES.enabled = true;
                
                // 포커스 강제 설정 (모바일 터치 응답성 향상)
                primaryES.firstSelectedGameObject = null; 
            }
        }

        private void UpdateCanvasHierarchy()
        {
            // [핵심] UI 원자적 정리 (Canvas 단위)
            BaseUI[] allUIs = Object.FindObjectsByType<BaseUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            // 단계 1: 장부 초기화 및 이미 DontDestroyOnLoad에 안착한 UI들을 우선 등록
            uiDictionary.Clear();
            foreach (var ui in allUIs)
            {
                if (ui == null) continue;
                
                // 최상위 루트가 이미 DDOL인지 확인
                Transform root = ui.transform;
                while (root.parent != null) root = root.parent;

                if (root.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    // DDOL에 있는 UI를 우선적으로 장부에 등록
                    if (uiDictionary.ContainsKey(ui.UIType) == false)
                    {
                        uiDictionary[ui.UIType] = ui;
                    }
                }
            }

            // 단계 2: 새로 로드된 루트(Canvas)들을 검사하여 파괴하거나 DDOL로 이동
            var newRoots = allUIs
                .Where(ui => ui != null && ui.gameObject.scene.name != "DontDestroyOnLoad")
                .Select(ui =>
                {
                    Transform root = ui.transform;
                    while (root.parent != null) root = root.parent;
                    return root.gameObject;
                })
                .Distinct()
                .ToList();

            foreach (var rootGameObject in newRoots)
            {
                var componentsInRoot = rootGameObject.GetComponentsInChildren<BaseUI>(true);
                
                // 해당 루트 안에 하나라도 이미 관리 중인(DDOL에 있는) UI 타입이 있다면 중복 세트로 간주하여 파괴
                bool isDuplicate = componentsInRoot.Any(ui => uiDictionary.ContainsKey(ui.UIType));

                if (isDuplicate == true)
                {
                    DestroyImmediate(rootGameObject);
                }
                else
                {
                    // 중복이 아니면 DDOL로 이동시키고 장부에 등록
                    DontDestroyOnLoad(rootGameObject);
                    foreach (var ui in componentsInRoot)
                    {
                        if (uiDictionary.ContainsKey(ui.UIType) == false)
                        {
                            uiDictionary[ui.UIType] = ui;
                        }
                    }
                }
            }
        }

        private void InitializeUIStates(bool isTitleScene, bool isLobbyScene, bool isGameScene, bool isCurrentlyLoading)
        {
            // Dictionary의 값을 복사하여 반복문 도중 변경 방지
            var uiList = uiDictionary.Values.ToList();
            
            foreach (var ui in uiList)
            {
                if (ui == null) continue;

                // [핵심] 로딩 중일 때는 로딩 UI만 표시하고 나머지는 모두 닫음
                if (isCurrentlyLoading == true)
                {
                    if (ui.UIType == UIType.Loading) ui.Open();
                    else ui.Close();
                    continue;
                }

                // 로딩이 아닐 때 각 씬별 UI 설정
                if (isTitleScene == true)
                {
                    // 로그인/타이틀 씬에서는 TitleUI 우선
                    if (ui.UIType == UIType.Title) ui.Open();
                    else ui.Close();
                }
                else if (isLobbyScene == true)
                {
                    // 로비 씬에서는 LobbyUI 표시
                    if (ui.UIType == UIType.Lobby) ui.Open();
                    else ui.Close();
                }
                else if (isGameScene == true)
                {
                    // 인게임 씬 진입 시 ARPlaneScanUI를 우선 표시 (HUD는 스캔 완료 후 활성화됨)
                    if (ui.UIType == UIType.ARPlaneScan) ui.Open();
                    else ui.Close();
                }
                else
                {
                    // 기본값: 팝업이 아닌 것만 오픈
                    if (ui.IsPopup == false) ui.Open();
                    else ui.Close();
                }
            }
        }

        private void Update()
        {
            if (isUIManagementEnabled == false) return;

            // [안정성] 모바일 환경 등 Keyboard.current가 null일 수 있는 상황 대응
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame == true)
            {
                if (IsPopupOpen == true) CloseAllPopup();
                else ToggleUI(UIType.Menu);
            }

            UpdateGlobalState();
        }

        private void UpdateGlobalState()
        {
            if (isUIManagementEnabled == false) return;

            bool anyPopupActive = uiDictionary.Values.Any(ui => ui != null && ui.IsPopup == true && ui.gameObject.activeSelf == true);
            IsPopupOpen = anyPopupActive;

            // [수정] ZeroDarkMos 님의 지침에 따라 일괄적인 Time.timeScale = 0 로직을 제거합니다.
            // 이제 개별 UI가 필요에 따라 시간을 조절하거나, 배경이 멈추지 않은 상태로 유지됩니다.
        }

        public void RefreshUIState() => UpdateGlobalState();

        public void ToggleUI(UIType uiType)
        {
            if (uiDictionary.TryGetValue(uiType, out BaseUI targetUI) == false) return;

            if (targetUI.gameObject.activeSelf == true) SetUIActive(uiType, false);
            else
            {
                CloseAllPopup();
                SetUIActive(uiType, true);
            }
            RefreshUIState();
        }

        public void CloseAllPopup()
        {
            var activePopups = uiDictionary.Values
                .Where(ui => ui != null && ui.IsPopup == true && ui.gameObject.activeSelf == true)
                .ToList();

            foreach (var popup in activePopups) SetUIActive(popup.UIType, false);
            RefreshUIState();
        }

        public void SetUIActive(UIType type, bool isActive)
        {
            if (uiDictionary.TryGetValue(type, out var targetUI) == true)
            {
                if (isActive == true) targetUI.Open();
                else targetUI.Close();
            }
        }

        public void SetAllInGameUIActive(bool isActive)
        {
            isUIManagementEnabled = isActive;
            OnInitialize(); // 상태 재정렬
        }
    }
}
