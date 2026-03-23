using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ParkSeyang
{
    public class ARPlaneScanUI : BaseUI
    {
        public override UIType UIType => UIType.ARPlaneScan;
        public override bool IsPopup => false; 

        [Header("스캔 정보 제공")]
        [SerializeField] private ARScanProvider scanProvider;
        
        [Header("UI 요소")]
        [SerializeField] private TMP_Text progressValueText;
        [SerializeField] private TMP_Text guideInstructionText;

        private bool isScanFinished = false;
        private const float PercentageMultiplier = 100.0f;
        private const float CompletionThreshold = 1.0f;

        protected override void Awake()
        {
            base.Awake();

            // [추가] 인스펙터 참조가 비어있을 경우 씬에서 직접 찾습니다.
            if (scanProvider == null)
            {
                scanProvider = Object.FindAnyObjectByType<ARScanProvider>();
            }
        }

        private void OnEnable()
        {
            // [보강] 영속 UI의 경우 씬 전환 시 scanProvider가 null일 수 있으므로 다시 체크합니다.
            if (scanProvider == null)
            {
                scanProvider = Object.FindAnyObjectByType<ARScanProvider>();
            }

            if (scanProvider != null)
            {
                // [UI 리셋] 내부 플래그만 초기화 (실제 AR 데이터 리셋은 InitializeState에서 전담)
                isScanFinished = false;
                
                scanProvider.OnScanProgressUpdated += UpdateScanDisplay;
                scanProvider.SetScanActive(true);
            }
        }

        private void OnDisable()
        {
            if (scanProvider != null)
            {
                scanProvider.OnScanProgressUpdated -= UpdateScanDisplay;
            }
        }

        private void UpdateScanDisplay(float horizonProgress)
        {
            if (isScanFinished == true)
            {
                return;
            }

            int percentage = Mathf.RoundToInt(horizonProgress * PercentageMultiplier);

            if (progressValueText != null)
            {
                progressValueText.text = $"지형 스캔 진행률: {percentage}%";
            }

            UpdateGuideMessage(horizonProgress);

            // 모든 조건 충족 시 즉시 스캔 완료 처리
            if (scanProvider.IsScanRequirementMet() == true)
            {
                OnScanRequirementMet();
            }
        }

        private void UpdateGuideMessage(float horizonProgress)
        {
            if (guideInstructionText == null || scanProvider.IsScanRequirementMet() == true)
            {
                return;
            }

            if (horizonProgress < CompletionThreshold)
            {
                guideInstructionText.text = "주변의 바닥을 골고루 비춰주세요.";
            }
        }

        private void OnScanRequirementMet()
        {
            if (isScanFinished == true) return;
            isScanFinished = true;

            // 평면 감지 중단
            if (scanProvider != null)
            {
                scanProvider.SetScanActive(false);
            }

            // [핵심] 스캔 완료 시 HUDUI(InGameUI) 활성화 및 자신 종료
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetUIActive(UIType.HUD, true);
            }
            
            Close();
        }
    }
}
