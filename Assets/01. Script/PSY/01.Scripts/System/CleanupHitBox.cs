using System.Collections.Generic;
using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 세척 도구(물총 등)의 판정 범위를 관리하는 클래스입니다.
    /// ICleanTargetPart 인터페이스를 활용하여 고속 피격 판정을 수행합니다.
    /// </summary>
    public class CleanupHitBox : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float hitInterval = 0.05f; // 판정 주기 (초)
        
        private ICleanAgent owner;
        private LayerMask targetLayers;
        private float cleaningPower;
        
        // 마지막 판정 시간을 기록하는 딕셔너리 (인터페이스 기반 캐싱)
        private Dictionary<ICleanTargetPart, float> lastHitTimeDic = new Dictionary<ICleanTargetPart, float>();

        private bool isActive = false;

        public void Initialize(ICleanAgent owner, LayerMask layers, float power)
        {
            this.owner = owner;
            this.targetLayers = layers;
            this.cleaningPower = power;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (active == false)
            {
                lastHitTimeDic.Clear();
            }
        }

        /// <summary>
        /// 범위 판정(AoE)을 위한 트리거 감지 로직입니다.
        /// WaterBeam 끝의 Sphere Collider 영역에 들어온 대상을 실시간으로 세척합니다.
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (isActive == false || other == null) return;

            // 1. 레이어 체크 (설정된 타겟 레이어만 판정)
            if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

            // 2. CleanupSystem을 통해 인터페이스 획득 및 판정 진행
            if (CleanupSystem.Instance != null)
            {
                ICleanTargetPart targetPart = CleanupSystem.Instance.GetCleanTarget(other);
                if (targetPart != null && targetPart.gameObject.activeInHierarchy == true)
                {
                    // 트리거 접촉 지점 또는 오브젝트 중심점을 히트 포인트로 전달
                    ProcessCleaning(targetPart, other.ClosestPoint(transform.position));
                }
            }
        }

        /// <summary>
        /// 기존 레이캐스트 방식과의 하위 호환성을 유지합니다.
        /// </summary>
        public void CheckHit(Collider hitCollider, Vector3 hitPoint)
        {
            if (isActive == false || hitCollider == null) return;

            if (CleanupSystem.Instance != null)
            {
                ICleanTargetPart targetPart = CleanupSystem.Instance.GetCleanTarget(hitCollider);
                if (targetPart != null && targetPart.gameObject.activeInHierarchy == true)
                {
                    ProcessCleaning(targetPart, hitPoint);
                }
            }
        }

        private void ProcessCleaning(ICleanTargetPart target, Vector3 hitPoint)
        {
            float currentTime = Time.time;

            // 1. 인터페이스 기반의 판정 주기(Interval) 체크
            if (lastHitTimeDic.TryGetValue(target, out float lastTime) == true)
            {
                if (currentTime - lastTime < hitInterval) return;
            }

            // [참고] 여기서 target.transform.position 등을 활용해 추가 거리 판정 로직 확장 가능

            // 2. 세척 이벤트 발생 (Receiver는 target.Owner를 통해 데이터 본체에 전달)
            CleanupEvent cleanEvent = new CleanupEvent
            {
                Sender = owner,
                Receiver = target.Owner,
                Power = cleaningPower * hitInterval, 
                HitPosition = hitPoint
            };

            CleanupSystem.Instance.InvokeCleanEvent(cleanEvent);

            // [디버그] 판정 성공 시 로그 출력 (타겟 정보, 세척력, 히트 지점)
            Debug.Log($"<color=cyan>[CleanupHitBox]</color> <b>Hit Success!</b> Target: {target.gameObject.name} | Power: {cleanEvent.Power:F2} | Pos: {hitPoint}");
            
            // 3. 인터페이스 참조를 키로 하여 마지막 판정 시간 갱신
            lastHitTimeDic[target] = currentTime;
        }
    }
}
