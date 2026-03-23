using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 세척 가능한 오브젝트의 실제 충돌 범위를 담당하는 컴포넌트입니다. (HurtBox 계승)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CleanupHurtBox : MonoBehaviour, ICleanTargetPart
    {
        public ICleanable Owner { get; private set; }
        
        [SerializeField] private Collider hurtCollider;
        public Collider Collider => hurtCollider;

        private void Awake()
        {
            if (hurtCollider == null)
            {
                hurtCollider = GetComponent<Collider>();
            }
        }

        /// <summary>
        /// 주인을 설정하고 CleanupSystem에 등록합니다.
        /// </summary>
        public void Initialize(ICleanable owner)
        {
            Owner = owner;

            if (hurtCollider == null)
            {
                hurtCollider = GetComponent<Collider>();
            }

            // [안정성 강화] 콜라이더를 트리거로 설정하여 물리 간섭 방지 및 판정 일관성 확보
            if (hurtCollider != null)
            {
                hurtCollider.isTrigger = true;
            }

            if (CleanupSystem.IsInitialized == true && hurtCollider != null)
            {
                CleanupSystem.Instance.AddCleanTarget(hurtCollider, this);
            }
        }

        private bool applicationIsQuitting = false;

        private void OnApplicationQuit() => applicationIsQuitting = true;

        private void OnDestroy()
        {
            if (applicationIsQuitting == false && CleanupSystem.IsInitialized == true && hurtCollider != null)
            {
                if (CleanupSystem.Instance != null)
                {
                    CleanupSystem.Instance.RemoveCleanTarget(hurtCollider);
                }
            }
        }
    }
}
