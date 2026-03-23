using UnityEngine;

namespace ParkSeyang
{
    public abstract class BaseUI : MonoBehaviour
    {
        public abstract UIType UIType { get; }

        [Tooltip("이 UI가 열릴 때 게임 시간을 멈추고 커서를 활성화할지 여부")]
        [SerializeField] private bool isPopup = true;
        public virtual bool IsPopup => isPopup;

        [Tooltip("UIManager가 이 UI를 직접 관리(등록/끄기/켜기)할지 여부")]
        [SerializeField] protected bool isManagedByUIManager = true;

        protected virtual void Awake()
        {
            if (isManagedByUIManager == true)
            {
                UIManager.Instance.RegisterUI(this);
            }
        }

        protected virtual void Start()
        {
            // 자식 클래스에서 필요한 초기화 로직 구현
        }

        public virtual void Open()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            gameObject.SetActive(true);
            Refresh();
        }

        public virtual void Close()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// UI의 데이터를 최신 상태로 갱신합니다. (로딩 중 예열 시 호출됨)
        /// </summary>
        public virtual void Refresh() { }
    }
}
