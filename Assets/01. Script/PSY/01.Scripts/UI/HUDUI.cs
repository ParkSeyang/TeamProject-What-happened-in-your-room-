using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

namespace ParkSeyang
{
    /// <summary>
    /// 하드웨어 터치(Touchscreen)와 버튼 클릭을 모두 지원하며, 
    /// WaterGun의 ToggleSpray를 정확히 호출하는 모바일 최적화 HUD입니다.
    /// </summary>
    public class HUDUI : BaseUI
    {
        public override UIType UIType => UIType.HUD;
        public override bool IsPopup => false; 

        [Header("Aim Settings")]
        [SerializeField] private Image aimImage;

        [Header("Time Bar Settings")]
        [SerializeField] private Image timeBarFill;
        [SerializeField] private TMP_Text timeText;

        [Header("Score Settings")]
        [SerializeField] private TMP_Text scoreText;

        [Header("Water System Settings")]
        [SerializeField] private Button waterButton; 
        [SerializeField] private TMP_Text waterButtonText;
        [SerializeField] private Image waterGaugeFill;
        [SerializeField] private TMP_Text waterGaugeText;

        [Header("Countdown Settings")]
        [SerializeField] private CanvasGroup countdownGroup;
        [SerializeField] private Image Background;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private float countdownDuration = 5f;
        
        private WaterGun cachedWaterGun;
        private RectTransform buttonRect;
        private Canvas rootCanvas;
        private bool isGamePlaying = false;

        protected override void Awake()
        {
            base.Awake();
            if (waterButton != null) buttonRect = waterButton.GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
            
            InitializeHUD();

            if (countdownGroup != null)
            {
                countdownGroup.alpha = 0f;
                countdownGroup.gameObject.SetActive(false);
            }
        }

        private void InitializeHUD()
        {
            FindWaterGun();

            if (waterButton != null)
            {
                waterButton.onClick.RemoveAllListeners();
                waterButton.onClick.AddListener(OnWaterButtonClicked);
            }
        }

        private void FindWaterGun()
        {
            if (InGameSystem.Instance != null && InGameSystem.Instance.PlayerWaterGun != null)
            {
                cachedWaterGun = InGameSystem.Instance.PlayerWaterGun;
            }
            else
            {
                cachedWaterGun = Object.FindAnyObjectByType<WaterGun>();
            }
        }

        private void Update()
        {
            if (InGameSystem.Instance != null && InGameSystem.Instance.IsControlLocked == true) return;
            HandleHardwareTouch();
        }

        private void HandleHardwareTouch()
        {
            Vector2 touchPos = Vector2.zero;
            bool wasTriggered = false;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame == true)
                {
                    touchPos = touch.position.ReadValue();
                    wasTriggered = true;
                }
            }
            else if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame == true)
                {
                    touchPos = Mouse.current.position.ReadValue();
                    wasTriggered = true;
                }
            }

            if (wasTriggered == true && IsPointInsideButton(touchPos) == true)
            {
                ExecuteWaterAction();
            }
        }

        private bool IsPointInsideButton(Vector2 screenPoint)
        {
            if (buttonRect == null) return false;
            Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPoint, cam);
        }

        private void OnWaterButtonClicked()
        {
            ExecuteWaterAction();
        }

        private void ExecuteWaterAction()
        {
            if (InGameSystem.Instance != null && InGameSystem.Instance.IsControlLocked == true) return;

            if (cachedWaterGun == null) FindWaterGun();

            if (cachedWaterGun != null)
            {
                Debug.Log($"[HUDUI] Water Button Triggered -> Calling ToggleSpray on {cachedWaterGun.name}");
                cachedWaterGun.ToggleSpray();
            }
            else
            {
                Debug.LogError("[HUDUI] Action Failed: WaterGun reference not found.");
            }
        }

        #region UI Update & Events (Observer Callbacks)
        private void OnEnable()
        {
            if (GameStatusController.Instance != null)
            {
                var status = GameStatusController.Instance;
                status.OnWaterChanged += UpdateWaterUI;
                status.OnScoreChanged += UpdateScoreUI;
                status.OnTimerChanged += UpdateTimerUI;
                UpdateWaterUI(status.WaterGauge);
                UpdateScoreUI(status.CurrentScore);
                UpdateTimerUI(status.GameTimer);
            }
            if (InGameSystem.Instance != null) InGameSystem.Instance.OnRefillStateChanged += OnRefillStateChanged;
        }

        private void OnDisable()
        {
            if (GameStatusController.Instance != null)
            {
                var status = GameStatusController.Instance;
                status.OnWaterChanged -= UpdateWaterUI;
                status.OnScoreChanged -= UpdateScoreUI;
                status.OnTimerChanged -= UpdateTimerUI;
            }
            if (InGameSystem.Instance != null) InGameSystem.Instance.OnRefillStateChanged -= OnRefillStateChanged;
        }

        private void UpdateWaterUI(float currentWater)
        {
            if (waterGaugeFill != null) waterGaugeFill.fillAmount = currentWater / 100f;
            if (waterGaugeText != null) waterGaugeText.text = $"{Mathf.CeilToInt(currentWater)}%";
            
            if (InGameSystem.Instance != null && InGameSystem.Instance.IsRefilling == false)
            {
                if (waterButtonText != null)
                {
                    waterButtonText.text = (currentWater <= 0.1f) ? "Charge" : "Wash";
                    waterButtonText.color = (currentWater <= 0.1f) ? Color.red : Color.white;
                }
            }
        }

        private void UpdateScoreUI(int score)
        {
            if (scoreText != null) scoreText.text = $"Score : {score}";
        }

        private void UpdateTimerUI(float remainingTime)
        {
            if (timeBarFill != null) timeBarFill.fillAmount = remainingTime / 150f;
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timeText.text = string.Format("{0:D2} : {1:D2}", minutes, seconds);
            }
        }

        private void OnRefillStateChanged(bool isRefilling)
        {
            if (waterButtonText != null)
            {
                if (isRefilling == true)
                {
                    waterButtonText.text = "Stop";
                    waterButtonText.color = Color.yellow;
                }
                else
                {
                    if (GameStatusController.Instance != null)
                        UpdateWaterUI(GameStatusController.Instance.WaterGauge);
                }
            }
        }
        #endregion

        public override void Open()
        {
            base.Open();
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            if (countdownGroup == null) { StartGame(); yield break; }
            countdownGroup.gameObject.SetActive(true);
            countdownGroup.alpha = 1f;
            int currentCount = Mathf.CeilToInt(countdownDuration);
            while (currentCount > 0)
            {
                if (countdownText != null)
                {
                    countdownText.text = currentCount.ToString();
                    float timer = 0f;
                    while (timer < 1.0f)
                    {
                        timer += Time.deltaTime;
                        countdownText.alpha = Mathf.Sin(timer * Mathf.PI); 
                        yield return null;
                    }
                }
                currentCount--;
            }
            if (countdownText != null)
            {
                countdownText.text = "START!";
                float timer = 0f;
                while (timer < 1.0f)
                {
                    timer += Time.deltaTime;
                    countdownText.alpha = 1f - timer;
                    yield return null;
                }
            }
            countdownGroup.gameObject.SetActive(false);
            StartGame();
        }

        private void StartGame()
        {
            isGamePlaying = true;
            if (InGameSystem.Instance != null) InGameSystem.Instance.SetControlLock(false);
            Debug.Log("[HUDUI] Game Started & Control Unlocked.");
        }

        public override void Refresh()
        {
            if (GameStatusController.Instance != null)
            {
                var status = GameStatusController.Instance;
                UpdateWaterUI(status.WaterGauge);
                UpdateScoreUI(status.CurrentScore);
                UpdateTimerUI(status.GameTimer);
            }
        }
    }
}
