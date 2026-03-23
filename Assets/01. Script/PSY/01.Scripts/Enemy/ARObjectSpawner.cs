using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace ParkSeyang
{
    /// <summary>
    /// AR 지형 스캔 완료 후, 바닥 평면에 오염물(Contamination)을 무작위로 생성하는 클래스입니다.
    /// </summary>
    public class ARObjectSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct DifficultySpawnSettings
        {
            public DifficultyLevel difficulty;
            public GameObject prefab;
            public int initialSpawnCount;
            public float spawnInterval;
        }

        [Header("스캔 데이터 참조")]
        [SerializeField] private ARScanProvider scanProvider;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;

        [Header("난이도별 스폰 설정")]
        [Tooltip("각 난이도별로 사용할 프리팹, 초기 개수, 생성 간격을 설정하세요.")]
        [SerializeField] private List<DifficultySpawnSettings> difficultySettings = new List<DifficultySpawnSettings>();
        
        [Header("공통 설정")]
        [SerializeField] private float spawnHeightOffset = 0.001f;
        [SerializeField] private LayerMask waterTankLayer; // [추가] 물탱크 레이어

        private Dictionary<DifficultyLevel, DifficultySpawnSettings> settingsCache = new Dictionary<DifficultyLevel, DifficultySpawnSettings>();
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Coroutine spawnCoroutine;

        // 런타임에서 사용할 현재 난이도 설정값
        private DifficultySpawnSettings currentSettings;

        private void Awake()
        {
            InitializeSettingsCache();

            if (scanProvider == null) Debug.LogError("[ARObjectSpawner] scanProvider가 할당되지 않았습니다.");
            if (planeManager == null) Debug.LogError("[ARObjectSpawner] planeManager가 할당되지 않았습니다.");
            if (anchorManager == null) Debug.LogError("[ARObjectSpawner] anchorManager가 할당되지 않았습니다.");
        }

        private void OnDisable()
        {
            // [핵심] 파괴되거나 비활성화될 때 코루틴을 반드시 중지하여 MissingReference를 방지합니다.
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }

        private void InitializeSettingsCache()
        {
            settingsCache.Clear();
            foreach (var setting in difficultySettings)
            {
                if (settingsCache.ContainsKey(setting.difficulty) == false)
                {
                    settingsCache.Add(setting.difficulty, setting);
                }
            }
        }

        /// <summary>
        /// 지형 스캔이 완료된 후 호출되어 오염물 생성을 시작합니다.
        /// </summary>
        public void SpawnContaminations()
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            
            // [개선!] 난이도에 따른 모든 설정을 한 번에 로드합니다.
            if (GameStatusController.IsInitialized == true)
            {
                DifficultyLevel selectedLevel = GameStatusController.Instance.SelectedDifficulty;
                if (settingsCache.TryGetValue(selectedLevel, out currentSettings) == false)
                {
                    Debug.LogError($"[ARObjectSpawner] {selectedLevel} 난이도 설정이 인스펙터에서 누락되었습니다!");
                    return;
                }
            }

            spawnCoroutine = StartCoroutine(RoutineSpawnProcess());
        }

        private IEnumerator RoutineSpawnProcess()
        {
            if (currentSettings.prefab == null) yield break;

            // [보강] 가용한 평면이 생길 때까지 대기 (최대 10초)
            float waitTimer = 0f;
            while (GetAvailablePlanes().Count == 0 && waitTimer < 10.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            if (GetAvailablePlanes().Count == 0)
            {
                Debug.LogWarning("[ARObjectSpawner] 10초 동안 가용한 평면을 찾지 못해 스폰을 시작할 수 없습니다.");
                yield break;
            }

            // [최적화] 인스펙터에서 설정한 난이도별 간격 적용
            var wait = new WaitForSeconds(currentSettings.spawnInterval);

            // 1. 초기 스폰 (난이도별 개수 적용)
            SpawnBatch(currentSettings.prefab, currentSettings.initialSpawnCount);

            // 2. 주기적 추가 스폰
            while (GameStatusController.Instance != null && GameStatusController.Instance.GameTimer > 0)
            {
                yield return wait;

                if (GameStatusController.Instance.GameTimer <= 0) break;

                SpawnBatch(currentSettings.prefab, 1);
            }
        }

        private void SpawnBatch(GameObject targetPrefab, int count)
        {
            if (planeManager == null || targetPrefab == null) return;

            var availablePlanes = GetAvailablePlanes();
            if (availablePlanes.Count == 0) return;

            // [최적화] 프리팹의 DustBase에 등록된 실제 메쉬 콜라이더 크기 파악
            float prefabSize = 0.1f;
            var dustBase = targetPrefab.GetComponent<DustBase>();
            if (dustBase != null && dustBase.BodyCollider != null)
            {
                // 메쉬의 바운드 크기 중 가장 큰 값을 반경으로 사용 (월드 스케일 적용)
                Vector3 extents = dustBase.BodyCollider.bounds.extents;
                prefabSize = Mathf.Max(extents.x, extents.z) * targetPrefab.transform.localScale.x;
            }

            for (int i = 0; i < count; i++)
            {
                bool isSpawned = false;
                int attempts = 0;
                const int MAX_ATTEMPTS = 5; // [제안 반영] 최대 시도 횟수 5회로 제한

                while (isSpawned == false && attempts < MAX_ATTEMPTS)
                {
                    attempts++;
                    ARPlane targetPlane = availablePlanes[Random.Range(0, availablePlanes.Count)];
                    Vector3 spawnPos = GetRandomPositionOnPlane(targetPlane);

                    if (spawnPos != Vector3.zero)
                    {
                        // [개선] 프리팹의 크기를 고려하여 물탱크와 충돌하는지 체크
                        if (IsPositionSafe(spawnPos, prefabSize) == true)
                        {
                            SpawnSingleContamination(targetPrefab, spawnPos, targetPlane);
                            isSpawned = true;
                        }
                    }
                }
            }
        }

        private bool IsPositionSafe(Vector3 pos, float radius)
        {
            // [개선] 단순히 점 하나가 아니라, 먼지의 크기(radius)만큼 공간이 비어있는지 체크합니다.
            // waterTankLayer와만 충돌 검사를 수행합니다.
            return Physics.CheckSphere(pos, radius, waterTankLayer) == false;
        }

        /// <summary>
        /// 게임 종료 시 호출되어 스폰을 중지하고 모든 오브젝트를 제거합니다.
        /// </summary>
        public void StopSpawningAndClear()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            ClearAllSpawnedObjects();
            Debug.Log("[ARObjectSpawner] Spawning stopped and all objects cleared.");
        }

        private List<ARPlane> GetAvailablePlanes()
        {
            List<ARPlane> planes = new List<ARPlane>();
            foreach (var plane in planeManager.trackables)
            {
                // [보강] 트래킹 상태가 유효한 수평(바닥) 평면만 포함합니다.
                if (plane != null && plane.trackingState != TrackingState.None && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    planes.Add(plane);
                }
            }
            return planes;
        }

        private Vector3 GetRandomPositionOnPlane(ARPlane plane)
        {
            // [개선] 스캔된 통합 바닥 높이를 가져옵니다.
            float floorHeight = (scanProvider != null) ? scanProvider.GetScannedFloorHeight() : plane.center.y;

            // 평면의 중심에서 랜덤한 오프셋 계산 (단순화된 바운더리 체크)
            float randomX = Random.Range(-plane.extents.x * 0.8f, plane.extents.x * 0.8f);
            float randomY = Random.Range(-plane.extents.y * 0.8f, plane.extents.y * 0.8f);

            // ARPlane의 로컬 좌표계에서 Z는 0입니다. (extents.x, extents.y가 가로 세로)
            Vector3 localPos = new Vector3(randomX, 0, randomY);
            Vector3 worldPos = plane.transform.TransformPoint(localPos);
            
            // [디테일 수정] Y좌표를 계산된 바닥 높이로 고정하고, 오프셋만큼만 띄웁니다.
            worldPos.y = floorHeight + spawnHeightOffset;

            return worldPos;
        }

        private void SpawnSingleContamination(GameObject prefab, Vector3 position, ARPlane plane)
        {
            // [핵심] 바닥 평면에만 스폰하므로 바닥용 회전값만 설정합니다.
            Vector3 normal = plane.transform.up;
            Quaternion spawnRotation = Quaternion.LookRotation(normal, Vector3.forward);

            GameObject obj = Instantiate(prefab, position, spawnRotation);

            // AR Anchor를 추가하여 현실 공간 좌표에 고정
            if (anchorManager != null)
            {
                ARAnchor anchor = anchorManager.AttachAnchor(plane, new Pose(position, spawnRotation));
                if (anchor != null)
                {
                    obj.transform.parent = anchor.transform;
                }
            }

            spawnedObjects.Add(obj);
        }

        public void ClearAllSpawnedObjects()
        {
            // [최적화] 리스트를 역순으로 순회하며 안전하게 제거합니다.
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Destroy(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }
    }
}
