using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class UIManager : SingletonBase<UIManager>
{
    private Dictionary<UIType, BaseUI> uiDictionary = new Dictionary<UIType, BaseUI>();

    public bool IsPopupOpen { get; private set; }
    private bool isUIManagementEnabled = true;

  // public MerchantNPC CurrentMerchant { get; set; }

    public void RegisterUI(BaseUI baseUI)
    {
        if (baseUI == null) return;

        // [보강] 이미 DontDestroyOnLoad에 안착한 UI가 있다면 신규 등록 거부
        if (uiDictionary.TryGetValue(baseUI.UIType, out var existingUI))
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 시마다 초기화 로직 실행
        OnInitialize();
    }

  // protected override void OnInitialize()
  // {
  //     string sceneName = SceneManager.GetActiveScene().name;
  //     bool isTitleScene = sceneName.Contains("Start") || sceneName.Contains("Title");
  //     bool isCurrentlyLoading = (GameSceneManager.Instance != null && GameSceneManager.Instance.IsLevelLoading);

  //     // [표준 준수] ! 연산자 대신 false 비교 사용
  //     isUIManagementEnabled = (isTitleScene == false && isCurrentlyLoading == false);

  //     // [핵심 1] EventSystem 중복 방지 및 단일 활성화
  //     var allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
  //     EventSystem primaryES = null;

  //     foreach (var eventSystem in allEventSystems)
  //     {
  //         if (eventSystem.gameObject.scene.name == "DontDestroyOnLoad")
  //         {
  //             if (primaryES == null) primaryES = eventSystem;
  //             else DestroyImmediate(eventSystem.gameObject); 
  //         }
  //     }

  //     foreach (var eventSystem in allEventSystems)
  //     {
  //         if (eventSystem == null || eventSystem.gameObject.scene.name == "DontDestroyOnLoad") continue;

  //         if (primaryES == null)
  //         {
  //             primaryES = eventSystem;
  //             DontDestroyOnLoad(eventSystem.gameObject);
  //         }
  //         else
  //         {
  //             DestroyImmediate(eventSystem.gameObject);
  //         }
  //     }

  //     if (primaryES != null)
  //     {
  //         primaryES.gameObject.SetActive(true);
  //         primaryES.enabled = true;
  //     }

  //     // [핵심 2] UI 원자적 정리 (Canvas 단위)
  //     BaseUI[] allUIs = Object.FindObjectsByType<BaseUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
  //     uiDictionary.Clear();

  //     // 단계 1: 이미 DDOL에 있는 UI들을 장부에 먼저 등록
  //     foreach (var uiBase in allUIs)
  //     {
  //         if (uiBase == null) continue;
  //         Transform rootTransform = uiBase.transform;
  //         while (rootTransform.parent != null) rootTransform = rootTransform.parent;

  //         if (rootTransform.gameObject.scene.name == "DontDestroyOnLoad")
  //         {
  //             uiDictionary[uiBase.UIType] = uiBase;
  //         }
  //     }

  //     // 단계 2: 새로 로드된 루트(Canvas)들을 검사하여 파괴하거나 DDOL로 이동
  //     var newRoots = allUIs
  //         .Where(uiBase => uiBase != null && uiBase.gameObject.scene.name != "DontDestroyOnLoad")
  //         .Select(uiBase => {
  //             Transform currentTransform = uiBase.transform;
  //             while (currentTransform.parent != null) currentTransform = currentTransform.parent;
  //             return currentTransform.gameObject;
  //         })
  //         .Distinct()
  //         .ToList();

  //     foreach (var rootGameObject in newRoots)
  //     {
  //         var componentsInRoot = rootGameObject.GetComponentsInChildren<BaseUI>(true);
  //         // 해당 루트 안에 하나라도 이미 관리 중인 UI 타입이 있다면 중복 세트로 간주
  //         bool isDuplicate = componentsInRoot.Any(uiComponent => uiDictionary.ContainsKey(uiComponent.UIType));

  //         if (isDuplicate == true)
  //         {
  //             DestroyImmediate(rootGameObject);
  //         }
  //         else
  //         {
  //             DontDestroyOnLoad(rootGameObject);
  //             foreach (var uiComponent in componentsInRoot)
  //             {
  //                 uiDictionary[uiComponent.UIType] = uiComponent;
  //             }
  //         }
  //     }

  //     // [단계 3] UI 초기 상태 설정
  //     var uiInstanceList = uiDictionary.Values.ToList();
  //     foreach (var uiInstance in uiInstanceList)
  //     {
  //         if (uiInstance == null) continue;

  //         if (uiInstance.UIType == UIType.Title)
  //         {
  //             if (isTitleScene == true) uiInstance.Open();
  //             else uiInstance.Close();
  //         }
  //         else if (uiInstance.UIType == UIType.Loading)
  //         {
  //             if (isCurrentlyLoading == true) uiInstance.Open();
  //             else uiInstance.Close();
  //         }
  //         else
  //         {
  //             if (isTitleScene == true) uiInstance.Close();
  //             else
  //             {
  //                 if (uiInstance.IsPopup == false) uiInstance.Open();
  //                 else uiInstance.Close();
  //             }
  //         }
  //     }
  //     
  //     // 커서 및 조작 최종 확정
  //     if (isTitleScene == true)
  //     {
  //         Time.timeScale = 1f;
  //         SetControlState(false); 
  //     }
  //     else if (isCurrentlyLoading == false)
  //     {
  //         RefreshUIState();
  //     }
  // }

 //  private void Update()
 //  {
 //      if (isUIManagementEnabled == false) return;

 //      if (Input.GetKeyDown(KeyCode.Escape))
 //      {
 //          if (IsPopupOpen == true) CloseAllPopup();
 //          else ToggleUI(UIType.Menu);
 //      }
 //      
 //      if (Input.GetKeyDown(KeyCode.K)) ToggleUI(UIType.Skill);
 //      if (Input.GetKeyDown(KeyCode.I)) ToggleUI(UIType.Inventory);
 //      
 //      UpdateGlobalState();
 //  }

 //  private void UpdateGlobalState()
 //  {
 //      if (isUIManagementEnabled == false) return;

 //      var uiInstanceList = uiDictionary.Values.ToList();
 //      bool anyActualPopupActive = uiInstanceList.Any(uiInstance => uiInstance != null && uiInstance.IsPopup && uiInstance.gameObject.activeSelf);

 //      IsPopupOpen = anyActualPopupActive;

 //      if (IsPopupOpen == true)
 //      {
 //          Time.timeScale = 0f;
 //          SetControlState(false); 
 //      }
 //      else
 //      {
 //          Time.timeScale = 1f;
 //          SetControlState(true); 
 //      }
 //  }

 //  public void RefreshUIState() => UpdateGlobalState();

 //  public void ToggleUI(UIType uiType)
 //  {
 //      if (uiDictionary.TryGetValue(uiType, out BaseUI targetUI) == false) return;

 //      if (targetUI.gameObject.activeSelf == true) SetUIActive(uiType, false);
 //      else
 //      {
 //          CloseAllPopup(); 
 //          SetUIActive(uiType, true);
 //          
 //          if (uiType == UIType.Inventory)
 //          {
 //              SetUIActive(UIType.Equip, true);
 //              SetUIActive(UIType.Stat, true);
 //          }
 //          if (uiType == UIType.Trade) SetUIActive(UIType.PlayerTrade, true);
 //      }
 //      RefreshUIState();
 //  }

 //  public void CloseAllPopup()
 //  {
 //      var activePopups = uiDictionary.Values.ToList()
 //          .Where(uiInstance => uiInstance != null && uiInstance.IsPopup && uiInstance.gameObject.activeSelf)
 //          .ToList();

 //      foreach (var popupUI in activePopups) SetUIActive(popupUI.UIType, false);
 //      RefreshUIState();
 //  }

  //  public void SetUIActive(UIType type, bool isActive)
  //  {
  //      if (uiDictionary.TryGetValue(type, out var targetUI))
  //      {
  //          if (isActive == true) targetUI.Open();
  //          else if (targetUI.gameObject.activeSelf == true)
  //          {
  //              targetUI.Close();
  //              
  //              if (type == UIType.Trade)
  //              {
  //                  SetUIActive(UIType.PlayerTrade, false);
  //                  if (CurrentMerchant != null) { CurrentMerchant.OnShopClosed(); CurrentMerchant = null; }
  //              }
  //              
  //              if (type == UIType.Inventory)
  //              {
  //                  SetUIActive(UIType.Equip, false);
  //                  SetUIActive(UIType.Stat, false);
  //              }
  //          }
  //      }
  //  }

   // public void SetAllInGameUIActive(bool isActive)
   // {
   //     isUIManagementEnabled = isActive;
   //     
   //     string sceneName = SceneManager.GetActiveScene().name;
   //     bool isTitleScene = sceneName.Contains("Start") || sceneName.Contains("Title");
   //     bool isCurrentlyLoading = (GameSceneManager.Instance != null && GameSceneManager.Instance.IsLevelLoading);
//
   //     var uiInstanceList = uiDictionary.Values.ToList();
//
   //     foreach (var uiInstance in uiInstanceList)
   //     {
   //         if (uiInstance == null) continue;
//
   //         if (isActive == true) 
   //         { 
   //             if (uiInstance.UIType == UIType.Title) uiInstance.Close();
   //             else if (uiInstance.IsPopup == false) uiInstance.Open(); 
   //         }
   //         else
   //         {
   //             bool isLoadingUI = (uiInstance.UIType == UIType.Loading && isCurrentlyLoading == true);
   //             bool shouldShowTitle = (isTitleScene == true && uiInstance.UIType == UIType.Title);
//
   //             if (shouldShowTitle == true || isLoadingUI == true) uiInstance.Open();
   //             else uiInstance.Close();
   //         }
   //     }
   //     
   //     if (isActive == true) RefreshUIState();
   //     else 
   //     { 
   //         Time.timeScale = 1f;
   //         SetControlState(isTitleScene == false); 
   //     }
   // }

    public void ForceRefreshAll()
    {
        var uiInstanceList = uiDictionary.Values.ToList();
        foreach (var uiInstance in uiInstanceList)
        {
            if (uiInstance != null) uiInstance.Refresh();
        }
    }

    private void SetControlState(bool canControl)
    {
        Cursor.visible = (canControl == false);
        Cursor.lockState = canControl ? CursorLockMode.Locked : CursorLockMode.None;
    }

  //  public void ShowWarning(string message) 
  //  { 
  //      if (uiDictionary.TryGetValue(UIType.WarningPopup, out var uiBase) && uiBase is WarningPopupUI warningUI) 
  //      {
  //          warningUI.Show(message); 
  //      }
  //  }

   // public void ShowGameOver() 
   // { 
   //     if (uiDictionary.TryGetValue(UIType.Menu, out var uiBase) && uiBase is GameMenuUI menuUI) 
   //     {
   //         menuUI.SetGameOverMode(); 
   //     }
   // }

    
}
