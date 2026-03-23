using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParkSeyang
{
    /// <summary>
    /// 플레이어의 물총 기능을 담당하는 클래스입니다.
    /// ICleanDetector 인터페이스를 구현하여 시스템 내에서 정식 세척 도구로 인정받습니다.
    /// </summary>
    public class WaterGun : MonoBehaviour, ICleanDetector
    {
        [Header("Beam Settings")]
        [SerializeField] private Transform muzzlePoint;    
        [SerializeField] private float maxDistance = 25.0f;        
        [SerializeField] private float defaultVisualDistance = 15.0f; 
        [SerializeField] private LayerMask hitLayers;

        [Header("Cleaning Settings")]
        [SerializeField] private float cleaningPower = 1.0f; 
        
        [Header("Debug Info (Read Only)")]
        [Tooltip("런타임에 생성된 WaterBeam에서 자동으로 참조를 가져옵니다.")]
        [SerializeField] private CleanupHitBox hitBox; 

        [Header("Water Settings")]
        [SerializeField] private float currentWaterAmount = 100f; 
        [SerializeField] private float maxWaterAmount = 100f;    
        [SerializeField] private float consumptionRate = 10f;    

        private ICleanAgent owner;         
        private Transform targetPoint;     
        private bool isSpraying = false;
        private bool isInitialized = false;

        #region ICleanDetector 구현 필드 및 프로퍼티
        public ICleanAgent Owner => owner;
        public LayerMask DetectionLayer => hitLayers;
        #endregion

        public float CurrentWater => currentWaterAmount;
        public float MaxWater => maxWaterAmount;

        private void Start()
        {
            StartCoroutine(RoutineInitialize());
        }

        private IEnumerator RoutineInitialize()
        {
            yield return null;
            Initialize(owner); // 인터페이스 방식의 초기화 호출
        }

        public void Initialize(ICleanAgent ownerAgent)
        {
            // [검증] 주인 정보 확보
            owner = ownerAgent ?? GetComponentInParent<ICleanAgent>() ?? GetComponent<ICleanAgent>();

            if (owner == null)
            {
                Debug.LogError($"[WaterGun] CRITICAL ERROR: ICleanAgent not found on {gameObject.name}.");
            }

            if (muzzlePoint == null)
            {
                Debug.LogError($"[WaterGun] CRITICAL ERROR: Muzzle Point is NOT assigned in the inspector!");
            }

            if (targetPoint == null)
            {
                GameObject dummy = new GameObject($"WaterBeam_TargetPoint_{gameObject.name}");
                targetPoint = dummy.transform;
                targetPoint.SetParent(transform);
            }

            isInitialized = true;
            Debug.Log($"[WaterGun] {gameObject.name} initialization completed via ICleanDetector.");
        }

        private void Update()
        {
            if (isInitialized == false) return;
            
            if (isSpraying == true)
            {
                if (currentWaterAmount <= 0)
                {
                    currentWaterAmount = 0;
                    DisableDetection();
                    return;
                }

                currentWaterAmount -= consumptionRate * Time.deltaTime;
                SyncWaterToHub(); 
                UpdateTargetPosition();
            }
        }

        private void SyncWaterToHub()
        {
            if (GameStatusController.IsInitialized == true)
            {
                GameStatusController.Instance.UpdateWater(currentWaterAmount);
            }
        }

        private void UpdateTargetPosition()
        {
            if (targetPoint == null || muzzlePoint == null) return;

            Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);
            
            // [안정성 강화] QueryTriggerInteraction.Collide를 추가하여 트리거인 HurtBox도 감지 가능하게 합니다.
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Collide))
            {
                targetPoint.position = hit.point;

                if (isSpraying == true && hitBox == null)
                {
                    // [중요] WaterBeam 프리팹 내부의 CleanupHitBox를 찾아 초기화합니다.
                    hitBox = GetComponentInChildren<CleanupHitBox>();
                    
                    if (hitBox != null)
                    {
                        // 1. 공격력 및 레이어 정보 주입
                        hitBox.Initialize(owner, hitLayers, cleaningPower);
                        // 2. 세척 활성화 (OnTriggerStay 작동)
                        hitBox.SetActive(true);
                    }
                }

                if (hitBox != null)
                {
                    hitBox.CheckHit(hit.collider, hit.point);
                }
            }
            else
            {
                targetPoint.position = muzzlePoint.position + (muzzlePoint.forward * defaultVisualDistance);
            }
        }

        public void ToggleSpray()
        {
            if (InGameSystem.Instance == null) return;

            if (InGameSystem.Instance.IsRefilling == true)
            {
                InGameSystem.Instance.IsRefilling = false;
                return;
            }

            if (currentWaterAmount <= 0.1f && InGameSystem.Instance.IsNearWaterTank == true)
            {
                InGameSystem.Instance.IsRefilling = true;
                return;
            }

            if (isSpraying == true) DisableDetection();
            else if (currentWaterAmount > 0) EnableDetection();
        }

        #region ICleanDetector 활성화/비활성화 구현
        public void EnableDetection()
        {
            if (CleanupSystem.IsInitialized == false) return;
            if (currentWaterAmount <= 0 || (InGameSystem.Instance != null && InGameSystem.Instance.IsRefilling == true)) return;

            isSpraying = true;
            hitBox = null; 
            
            CleanupSystem.Instance.StartSpraying(owner, muzzlePoint, targetPoint);
        }

        public void DisableDetection()
        {
            isSpraying = false;
            if (hitBox != null)
            {
                hitBox.SetActive(false);
                hitBox = null;
            }

            if (CleanupSystem.IsInitialized == true)
            {
                CleanupSystem.Instance.StopSpraying(owner);
            }
        }
        #endregion

        public void FillWater(float amount)
        {
            currentWaterAmount = Mathf.Min(currentWaterAmount + amount, maxWaterAmount);
            SyncWaterToHub();

            if (currentWaterAmount >= maxWaterAmount && InGameSystem.Instance != null)
            {
                InGameSystem.Instance.IsRefilling = false;
            }
        }

        /// <summary>
        /// [재시작용] 물총의 모든 상태를 초기값으로 리셋합니다.
        /// </summary>
        public void ResetWaterGun()
        {
            DisableDetection();
            currentWaterAmount = maxWaterAmount;
            isSpraying = false;
            SyncWaterToHub();
            
            Debug.Log("[WaterGun] State and Water Amount has been reset.");
        }
    }
}
