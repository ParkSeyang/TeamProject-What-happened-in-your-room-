using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ParkSeyang
{
    /// <summary>
    /// AR 게임 시작 시, 물을 충전할 수 있는 물탱크(Water Tank)를 지면에 배치하는 시스템입니다.
    /// </summary>
    public class WaterTankSystem : MonoBehaviour
    {
        [Header("참조 설정")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;

        [Header("물탱크 설정")]
        [SerializeField] private GameObject waterTankPrefab;
        [SerializeField] private float spawnHeightOffset = 0.9f; 

        private GameObject spawnedWaterTank;

        private void Awake()
        {
            if (planeManager == null) Debug.LogError("[WaterTankSystem] planeManager가 할당되지 않았습니다.");
            if (anchorManager == null) Debug.LogError("[WaterTankSystem] anchorManager가 할당되지 않았습니다.");
        }

        /// <summary>
        /// 지형 스캔이 완료된 후 호출되어 물탱크를 배치합니다.
        /// 가장 넓은 평면을 찾아 그 중심에 배치하는 로직을 포함합니다.
        /// </summary>
        public void SpawnWaterTank()
        {
            StopAllCoroutines();
            StartCoroutine(RoutineSpawnWaterTank());
        }

        private System.Collections.IEnumerator RoutineSpawnWaterTank()
        {
            if (waterTankPrefab == null || planeManager == null) yield break;

            // [보강] 가용한 평면이 생길 때까지 대기 (최대 10초)
            float waitTimer = 0f;
            while (FindLargestHorizontalPlane() == null && waitTimer < 10.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            ARPlane largestPlane = FindLargestHorizontalPlane();

            if (largestPlane == null)
            {
                Debug.LogWarning("[WaterTankSystem] 10초 동안 물탱크를 배치할 수 있는 수평 평면을 찾지 못했습니다.");
                yield break;
            }

            // 평면의 중심에서 오프셋만큼 위로 이동
            Vector3 spawnPosition = largestPlane.center;
            spawnPosition.y += spawnHeightOffset;

            spawnedWaterTank = Instantiate(waterTankPrefab, spawnPosition, Quaternion.identity);

            // 현실 좌표 고정을 위해 Anchor 추가
            if (anchorManager != null)
            {
                ARAnchor anchor = anchorManager.AttachAnchor(largestPlane, new Pose(spawnPosition, Quaternion.identity));
                if (anchor != null)
                {
                    spawnedWaterTank.transform.parent = anchor.transform;
                }
            }

            Debug.Log($"[WaterTankSystem] 물탱크가 {largestPlane.trackableId} 평면의 중심에 배치되었습니다.");
        }

        private ARPlane FindLargestHorizontalPlane()
        {
            ARPlane largestPlane = null;
            float maxArea = 0.0f;

            foreach (var plane in planeManager.trackables)
            {
                // [보강] 트래킹 상태가 유효한 수평 평면만 선택합니다.
                if (plane.trackingState == TrackingState.None || plane.alignment != PlaneAlignment.HorizontalUp) continue;

                float area = plane.size.x * plane.size.y;
                if (area > maxArea)
                {
                    maxArea = area;
                    largestPlane = plane;
                }
            }

            return largestPlane;
        }

        public void DestroyWaterTank()
        {
            if (spawnedWaterTank != null)
            {
                Destroy(spawnedWaterTank);
                spawnedWaterTank = null;
            }
        }
    }
}
