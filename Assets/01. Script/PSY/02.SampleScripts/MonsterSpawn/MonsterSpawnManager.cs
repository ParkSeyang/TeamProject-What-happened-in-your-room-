// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using UnityEngine.SceneManagement;
// using UnityEngine.Pool;
// 
// public class MonsterSpawnManager : SingletonBase<MonsterSpawnManager>
// {
//     [System.Serializable]
//     public struct MonsterPrefabEntry
//     {
//         public int enemyID;
//         public GameObject prefab;
//     }
// 
//     [Header("Monster Registration")]
//     [Tooltip("EnemyData.tsv의 ID와 일치하는 프리팹을 등록하세요.")]
//     [SerializeField] private List<MonsterPrefabEntry> monsterPrefabs = new List<MonsterPrefabEntry>();
// 
//     private List<MonsterSpawnPoint> spawnPoints = new List<MonsterSpawnPoint>();
//     private Dictionary<int, GameObject> monsterPrefabCache = new Dictionary<int, GameObject>();
//     
//     // [추가] 각 EnemyID별로 별도의 오브젝트 풀을 관리합니다.
//     private Dictionary<int, IObjectPool<GameObject>> monsterPools = new Dictionary<int, IObjectPool<GameObject>>();
// 
//    // protected override void OnInitialize()
//    // {
//    //     InitializePrefabCache();
//    //     RefreshSpawnPoints();
//    // }
// 
//     private void OnEnable()
//     {
//         SceneManager.sceneLoaded += OnSceneLoaded;
//     }
// 
//     private void OnDisable()
//     {
//         SceneManager.sceneLoaded -= OnSceneLoaded;
//     }
// 
//     private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//     {
//         // [추가] 씬 전환 시 아직 필드에 남은(혹은 죽어있는) 모든 몬스터를 풀로 강제 수거합니다.
//         CleanupMonsters();
// 
//         // 씬 전환 시 캐시는 재확인하되, 풀은 유지하여 성능을 확보합니다.
//         InitializePrefabCache();
//         RefreshSpawnPoints();
//     }
// 
//     private void CleanupMonsters()
//     {
//         // 매니저의 자식으로 붙어있는 모든 몬스터 객체를 전수 조사
//         foreach (Transform child in transform)
//         {
//             if (child.gameObject.activeSelf && child.TryGetComponent<IPoolAbleObject>(out var poolAble))
//             {
//                 ReleaseMonster(poolAble.EnemyID, child.gameObject);
//             }
//         }
//     }
// 
//   // private void Update()
//   // {
//   //     if (spawnPoints.Count == 0) return;
// 
//   //     // [추가] 씬 로딩 중에는 몬스터 스폰을 중단합니다.
//   //     if (GameSceneManager.IsInitialized == true && GameSceneManager.Instance.IsLevelLoading == true)
//   //     {
//   //         return;
//   //     }
// 
//   //     foreach (var point in spawnPoints)
//   //     {
//   //         if (point != null && point.ShouldRespawn())
//   //         {
//   //             SpawnMonsterAtPoint(point);
//   //         }
//   //     }
//   // }
// 
//     private void InitializePrefabCache()
//     {
//         monsterPrefabCache.Clear();
//         foreach (var entry in monsterPrefabs)
//         {
//             if (entry.prefab != null && monsterPrefabCache.ContainsKey(entry.enemyID) == false)
//             {
//                 monsterPrefabCache.Add(entry.enemyID, entry.prefab);
//                 
//                 // [추가] 해당 ID의 풀이 없다면 새로 생성 (04.ex 예제의 Dictionary 방식 활용)
//                 if (monsterPools.ContainsKey(entry.enemyID) == false)
//                 {
//                     int poolID = entry.enemyID; // 람다식 내 변수 캡처 시 값이 변하는 것을 방지하기 위한 로컬 복사본
//                     monsterPools.Add(poolID, new ObjectPool<GameObject>(
//                         // createFunc: 풀에 사용 가능한 객체가 없을 때 새로 생성하는 로직
//                         createFunc: () => CreateMonsterInstance(poolID),
//                         
//                         // actionOnGet: 풀에서 객체를 빌려올 때(Get) 실행되는 로직 (주로 활성화 및 초기화)
//                         actionOnGet: OnGetMonster,
//                         
//                         // actionOnRelease: 사용이 끝난 객체를 풀에 반환할 때(Release) 실행되는 로직 (주로 비활성화)
//                         actionOnRelease: OnReleaseMonster,
//                         
//                         // actionOnDestroy: 풀의 최대 크기를 초과하거나 풀이 파괴될 때 객체를 실제로 제거하는 로직
//                         actionOnDestroy: OnDestroyMonster,
//                         
//                         // defaultCapacity: 내부 배열의 초기 크기 (미리 확보해둘 메모리 공간)
//                         defaultCapacity: 10,
//                         
//                         // maxSize: 이 풀이 최대로 보관할 수 있는 객체의 수
//                         maxSize: 50
//                     ));
//                 }
//             }
//         }
//     }
// 
//     #region Pool Actions (UnityEngine.Pool)
// 
//     private GameObject CreateMonsterInstance(int enemyID)
//     {
//         if (monsterPrefabCache.TryGetValue(enemyID, out GameObject prefab))
//         {
//             GameObject instance = Instantiate(prefab, transform);
//             instance.SetActive(false);
//             return instance;
//         }
//         return null;
//     }
// 
//     private void OnGetMonster(GameObject monster)
//     {
//         monster.SetActive(true);
//         // 인터페이스가 있다면 초기화 호출
//         if (monster.TryGetComponent<IPoolAbleObject>(out var poolAble))
//         {
//             poolAble.OnGet();
//         }
//     }
// 
//     private void OnReleaseMonster(GameObject monster)
//     {
//         // 인터페이스가 있다면 반환 처리 호출
//         if (monster.TryGetComponent<IPoolAbleObject>(out var poolAble))
//         {
//             poolAble.OnRelease();
//         }
//         monster.SetActive(false);
//     }
// 
//     private void OnDestroyMonster(GameObject monster)
//     {
//         Destroy(monster);
//     }
// 
//     #endregion
// 
//     public void RefreshSpawnPoints()
//     {
//         spawnPoints.Clear();
//         spawnPoints = FindObjectsByType<MonsterSpawnPoint>(FindObjectsSortMode.None).ToList();
//         
//         if (spawnPoints.Count > 0)
//         {
//             foreach (var point in spawnPoints)
//             {
//                 SpawnMonsterAtPoint(point);
//             }
//         }
//     }
// 
//     private void SpawnMonsterAtPoint(MonsterSpawnPoint point)
//     {
//         if (point == null) return;
// 
//         if (monsterPools.TryGetValue(point.enemyID, out var pool))
//         {
//             GameObject monster = pool.Get();
//             point.OnMonsterSpawned(monster);
//         }
//     }
// 
//     /// <summary>
//     /// 외부(Monster)에서 사망 후 풀로 복귀할 때 호출합니다.
//     /// </summary>
//     public void ReleaseMonster(int enemyID, GameObject monster)
//     {
//         // [안전장치] 이미 비활성화된(풀에 반환된) 객체라면 중복 반환을 방지합니다.
//         if (monster == null || monster.activeSelf == false) return;
// 
//         if (monsterPools.TryGetValue(enemyID, out var pool))
//         {
//             pool.Release(monster);
//         }
//         else
//         {
//             Destroy(monster);
//         }
//     }
// }
