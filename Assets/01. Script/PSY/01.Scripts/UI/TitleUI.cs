using UnityEngine;
using UnityEngine.UI;

namespace ParkSeyang
{
    /// <summary>
    /// 게임 시작 시 가장 먼저 보여지는 타이틀 화면입니다.
    /// 화면 터치(버튼 클릭) 시 로그인 화면으로 전환합니다.
    /// </summary>
    public class TitleUI : BaseUI
    {
        public override UIType UIType => UIType.Title;

        [Header("UI Components")]
        [SerializeField] private Button startButton;

        protected override void Start()
        {
            base.Start();

            // [기획 반영] 버튼 클릭 시 로그인 화면 활성화 로직 바인딩
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
        }

        /// <summary>
        /// 시작 버튼(또는 전체 화면 터치 버튼) 클릭 시 호출됩니다.
        /// </summary>
        private void OnStartButtonClicked()
        {
            // [리팩토링] 중앙 매니저를 통해 상태를 전환하여 일관성을 유지합니다.
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetUIActive(UIType.Title, false);
                UIManager.Instance.SetUIActive(UIType.Login, true);
            }
            
            Debug.Log("[TitleUI] Start Button Clicked. Moving to Login via UIManager.");
        }
    }
}
