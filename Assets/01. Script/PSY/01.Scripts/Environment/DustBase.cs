using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 오염물의 타입을 구분합니다.
    /// </summary>
    public enum ContaminationType
    {
        Dust,           // 일반 먼지
        StubbornDust,   // 묵은 먼지
        Stain           // 때
    }

    /// <summary>
    /// 모든 오염물 오브젝트의 추상 기본 클래스입니다.
    /// 공통적인 시스템 등록, 세척 판정, 파괴 로직을 처리합니다.
    /// </summary>
    public abstract class DustBase : MonoBehaviour, ICleanable
    {
        [Header("Settings")]
        [SerializeField] protected ContaminationType contaminationType;
        [SerializeField] protected float maxDurability = 100f;
        [SerializeField] protected int rewardScore = 100;
        [SerializeField] protected MeshCollider bodyCollider; // [추가] 먼지 본체 메쉬 콜라이더

        [Header("Visuals (Common)")]
        [SerializeField] protected GameObject cleanEffect; 

        public abstract ContaminationType Type { get; }
        public MeshCollider BodyCollider => bodyCollider; // [추가] 외부 참조용 프로퍼티

        protected float currentDurability;
        protected CleanupHurtBox hurtBox;

        public GameObject GameObject => gameObject;

        protected virtual void Start() => InitializeDust();

        protected virtual void InitializeDust()
        {
            currentDurability = maxDurability;
            hurtBox = GetComponentInChildren<CleanupHurtBox>();

            if (hurtBox == null)
            {
                Debug.LogError($"[DustBase Error] {gameObject.name}에 CleanupHurtBox가 없습니다!");
                return;
            }
            
            hurtBox.Initialize(this);
        }

        public virtual void OnClean(float power, Vector3 position)
        {
            if (currentDurability <= 0) return;

            currentDurability -= power;

            if (currentDurability <= 0)
            {
                RemoveDust();
            }
        }

        protected virtual void RemoveDust()
        {
            // 1. 시스템에 제거 이벤트 전파
            if (CleanupSystem.IsInitialized == true)
            {
                CleanupEvent removeEvent = new CleanupEvent
                {
                    Receiver = this,
                    Power = 0,
                    HitPosition = transform.position
                };
                CleanupSystem.Instance.InvokeRemoveEvent(removeEvent);
            }

            // 2. 난이도 및 타입에 따른 점수 보고
            if (GameStatusController.IsInitialized == true)
            {
                GameStatusController.Instance.AddScore(rewardScore);
            }

            // 3. 오브젝트 제거
            Destroy(gameObject);
        }
    }
}
