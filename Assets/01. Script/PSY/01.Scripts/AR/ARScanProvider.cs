using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ParkSeyang
{
    public class ARScanProvider : MonoBehaviour
    {
        [Header("지형 스캔 설정")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARSession arSession;

        [Header("시각적 보정")]
        [Tooltip("평면 프리팹이 바닥과 밀착되어 보이도록 아래로 내리는 오프셋 값입니다.")]
        [SerializeField] private float planeVisualOffset = -0.01f;

        [Header("스캔 목표 영역 (평방 미터)")]
        [SerializeField] private float targetHorizontalArea = 3.0f;

        [Header("AABB 그룹화 설정")]
        [Tooltip("두 평면이 이 거리 이내에 있으면 같은 그룹으로 묶습니다.")]
        [SerializeField] private float groupingDistance = 0.1f;

        // 진행률 업데이트 이벤트: (수평 진행률 0~1)
        public event Action<float> OnScanProgressUpdated;

        private float currentHorizontalArea = 0.0f;
        private bool isAlreadyCompleted = false;

        // [추가] 각 평면의 원본 메테리얼 배열을 기억하기 위한 캐시 (상태 교체 시 활용)
        private Dictionary<TrackableId, Material[]> planeMaterialCache = new Dictionary<TrackableId, Material[]>();

        private void Awake()
        {
            if (planeManager == null)
            {
                Debug.LogError("[ARScanProvider] ARPlaneManager가 할당되지 않았습니다. 인스펙터에서 XR Origin의 PlaneManager를 연결해주세요.");
            }

            if (arSession == null)
            {
                arSession = UnityEngine.Object.FindAnyObjectByType<ARSession>();
            }
        }

        private void OnEnable()
        {
            if (planeManager != null)
            {
                // [최종 해결] Unity 6.0 (AR Foundation 6.0)에서는 UnityEvent 방식인 AddListener를 사용해야 합니다.
                planeManager.trackablesChanged.AddListener(HandleTrackablesChanged);
            }
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(HandleTrackablesChanged);
            }
        }

        private void HandleTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            // [추가] 새로 감지되거나 업데이트된 평면들에 시각적 설정(오프셋, 메테리얼) 적용
            foreach (var plane in args.added) ApplyPlaneSettings(plane);
            foreach (var plane in args.updated) ApplyPlaneSettings(plane);

            // 면적 계산 업데이트
            UpdateTotalAreaAABB();
            NotifyProgress();

            // 계산된 면적에 따라 스캔 모드를 제어합니다 (바닥 전용).
            UpdateDetectionMode();
        }

        /// <summary>
        /// 평면의 자식 오브젝트(Visual)에 오프셋과 현재 상태에 맞는 메테리얼을 적용합니다.
        /// </summary>
        private void ApplyPlaneSettings(ARPlane plane)
        {
            if (plane == null) return;

            // 1. 오프셋 적용 (자식들만)
            foreach (Transform child in plane.transform)
            {
                child.localPosition = new Vector3(0, planeVisualOffset, 0);
            }

            // 2. 메테리얼 적용 (본체 혹은 자식에 붙은 MeshRenderer 전체 탐색)
            var renderers = plane.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var meshRenderer in renderers)
            {
                // 최초 감지 시 메테리얼 배열 캐싱
                if (planeMaterialCache.ContainsKey(plane.trackableId) == false)
                {
                    planeMaterialCache[plane.trackableId] = meshRenderer.sharedMaterials;
                    Debug.Log($"[ARScanProvider] 평면({plane.trackableId}) 메테리얼 캐싱 완료. 개수: {meshRenderer.sharedMaterials.Length}");
                }

                // 캐시된 메테리얼 중 현재 상태에 맞는 것 선택 (0: 스캔 중, 1: 완료)
                var cachedMats = planeMaterialCache[plane.trackableId];
                if (cachedMats != null && cachedMats.Length >= 2)
                {
                    int targetIndex = isAlreadyCompleted ? 1 : 0;
                    meshRenderer.material = cachedMats[targetIndex];
                    
                    // 디버깅용 로그 (너무 자주 찍히지 않도록 완료 시점에만 명시)
                    if (isAlreadyCompleted == true)
                    {
                        Debug.Log($"[ARScanProvider] 평면({plane.trackableId}) 메테리얼을 1번(완료)으로 교체했습니다.");
                    }
                }
            }
        }

        private void UpdateDetectionMode()
        {
            if (planeManager == null) return;

            bool isHorizontalComplete = currentHorizontalArea >= targetHorizontalArea;

            // [로그] 현재 스캔 진행 상황 출력 (필요시 주석 처리 가능)
            // Debug.Log($"[ARScanProvider] 현재 면적: {currentHorizontalArea:F2} / 목표: {targetHorizontalArea:F2}");

            // [핵심] 상태 변화 감지: 완료되는 순간에 한 번만 실행
            if (isHorizontalComplete == true && isAlreadyCompleted == false)
            {
                Debug.Log("[ARScanProvider] ★★★ 스캔 목표 달성! 메테리얼 일괄 교체 시작 ★★★");
                isAlreadyCompleted = true;
                UpdateAllPlanesMaterial(); // 모든 평면 메테리얼 1번으로 일괄 교체
                SetScanActive(false);
            }
            else if (isHorizontalComplete == false && isAlreadyCompleted == true)
            {
                // 면적이 다시 줄어들 경우 (AABB 특성상 드묾) 상태 복구
                isAlreadyCompleted = false;
                UpdateAllPlanesMaterial(); 
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            }
        }

        /// <summary>
        /// 현재 트래킹 중인 모든 평면의 메테리얼을 최신 상태로 갱신합니다.
        /// </summary>
        private void UpdateAllPlanesMaterial()
        {
            if (planeManager == null) return;

            foreach (var plane in planeManager.trackables)
            {
                ApplyPlaneSettings(plane);
            }
        }

        public void SetScanActive(bool isActive)
        {
            if (planeManager == null)
            {
                Debug.LogWarning("[ARScanProvider] planeManager가 없어 스캔 상태를 변경할 수 없습니다.");
                return;
            }

            // [보완] 스캔 활성화 시 매니저 자체를 확실히 켭니다.
            planeManager.enabled = isActive;

            if (isActive == true)
            {
                // 스캔을 다시 시작할 때는 감지 모드를 초기화합니다.
                UpdateDetectionMode();
            }
        }

        /// <summary>
        /// [재시작용] 감지된 모든 평면을 제거하고 AR 세션을 리셋합니다.
        /// </summary>
        public void ClearAllPlanes()
        {
            // 1. AR 세션 전체 리셋 (가장 확실하고 안전한 방식)
            if (arSession != null)
            {
                arSession.Reset();
                Debug.Log("[ARScanProvider] ARSession has been reset.");
            }

            // 2. 현재 트래킹 중인 평면들 비활성화 (시각적 즉시 제거)
            if (planeManager != null)
            {
                foreach (var plane in planeManager.trackables)
                {
                    if (plane != null && plane.gameObject != null)
                    {
                        plane.gameObject.SetActive(false);
                    }
                }
            }

            // 3. 데이터 초기화
            ResetScanData();
        }

        /// <summary>
        /// 스캔 면적 데이터를 0으로 초기화하고 이벤트를 발송합니다.
        /// </summary>
        public void ResetScanData()
        {
            currentHorizontalArea = 0.0f;
            NotifyProgress();
        }

        private void UpdateTotalAreaAABB()
        {
            if (planeManager == null)
            {
                return;
            }

            List<ARPlane> activePlanes = new List<ARPlane>();
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null && plane.trackingState != TrackingState.None && IsHorizontal(plane) == true)
                {
                    activePlanes.Add(plane);
                }
            }

            if (activePlanes.Count == 0)
            {
                currentHorizontalArea = 0f;
                return;
            }

            var planeBoundsMap = new Dictionary<TrackableId, Bounds>();
            foreach (var plane in activePlanes)
            {
                planeBoundsMap[plane.trackableId] = CalculateSinglePlaneBounds(plane);
            }

            List<List<ARPlane>> groups = GroupNearbyPlanes(activePlanes, planeBoundsMap);

            float horizontalValue = 0.0f;

            foreach (var group in groups)
            {
                if (group == null || group.Count == 0) continue;

                Bounds combinedBounds = CalculateCombinedBounds(group, planeBoundsMap);
                
                // 그룹 내 수평 평면이 있는지 확인 (이미 activePlanes에서 필터링됨)
                horizontalValue += combinedBounds.size.x * combinedBounds.size.z;
            }

            currentHorizontalArea = horizontalValue;
        }

        private List<List<ARPlane>> GroupNearbyPlanes(List<ARPlane> planeList, Dictionary<TrackableId, Bounds> boundsDataMap)
        {
            List<List<ARPlane>> planeGroups = new List<List<ARPlane>>();
            HashSet<TrackableId> visitedPlaneIds = new HashSet<TrackableId>();

            foreach (var startPlane in planeList)
            {
                if (visitedPlaneIds.Contains(startPlane.trackableId)) continue;

                List<ARPlane> currentPlaneGroup = new List<ARPlane>();
                Queue<ARPlane> planeQueue = new Queue<ARPlane>();

                planeQueue.Enqueue(startPlane);
                visitedPlaneIds.Add(startPlane.trackableId);

                while (planeQueue.Count > 0)
                {
                    ARPlane targetPlane = planeQueue.Dequeue();
                    currentPlaneGroup.Add(targetPlane);

                    Bounds expandedBounds = boundsDataMap[targetPlane.trackableId];
                    expandedBounds.Expand(groupingDistance);

                    foreach (var otherPlane in planeList)
                    {
                        if (visitedPlaneIds.Contains(otherPlane.trackableId)) continue;
                        
                        // 바닥 평면끼리만 그룹화
                        if (IsHorizontal(otherPlane) == false) continue;
                        
                        // 노멀 방향 확인
                        float dot = Vector3.Dot(targetPlane.transform.up, otherPlane.transform.up);
                        if (dot < 0.9f) continue;

                        if (expandedBounds.Intersects(boundsDataMap[otherPlane.trackableId]))
                        {
                            visitedPlaneIds.Add(otherPlane.trackableId);
                            planeQueue.Enqueue(otherPlane);
                        }
                    }
                }
                planeGroups.Add(currentPlaneGroup);
            }
            return planeGroups;
        }

        private Bounds CalculateSinglePlaneBounds(ARPlane targetPlane)
        {
            Bounds planeBounds = new Bounds();
            bool isInitialized = false;

            foreach (var localPoint in targetPlane.boundary)
            {
                Vector3 worldPoint = targetPlane.transform.TransformPoint(new Vector3(localPoint.x, 0, localPoint.y));
                if (isInitialized == false) { planeBounds = new Bounds(worldPoint, Vector3.zero); isInitialized = true; }
                else planeBounds.Encapsulate(worldPoint);
            }
            return planeBounds;
        }

        private Bounds CalculateCombinedBounds(List<ARPlane> planeGroup, Dictionary<TrackableId, Bounds> boundsDataMap)
        {
            Bounds combinedBounds = boundsDataMap[planeGroup[0].trackableId];
            for (int i = 1; i < planeGroup.Count; i++) combinedBounds.Encapsulate(boundsDataMap[planeGroup[i].trackableId]);
            return combinedBounds;
        }

        private void NotifyProgress()
        {
            float horizontalProgress = Mathf.Clamp01(currentHorizontalArea / targetHorizontalArea);
            OnScanProgressUpdated?.Invoke(horizontalProgress);
        }

        public bool IsScanRequirementMet()
        {
            return currentHorizontalArea >= targetHorizontalArea;
        }

        /// <summary>
        /// 현재 감지된 모든 수평 평면들 중 가장 낮은 Y값을 바닥 높이로 간주하여 반환합니다.
        /// </summary>
        public float GetScannedFloorHeight()
        {
            if (planeManager == null || planeManager.trackables.count == 0) return 0f;

            float minHeight = float.MaxValue;
            bool foundPlane = false;

            foreach (var plane in planeManager.trackables)
            {
                if (plane != null && plane.trackingState != TrackingState.None && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    // 평면의 중심점 Y좌표 확인
                    float planeHeight = plane.center.y;
                    if (planeHeight < minHeight)
                    {
                        minHeight = planeHeight;
                        foundPlane = true;
                    }
                }
            }

            return foundPlane ? minHeight : 0f;
        }

        private bool IsHorizontal(ARPlane targetPlane) => targetPlane.alignment == PlaneAlignment.HorizontalUp || targetPlane.alignment == PlaneAlignment.HorizontalDown;
        private bool IsVertical(ARPlane targetPlane) => targetPlane.alignment == PlaneAlignment.Vertical;
    }
}
