// using UnityEngine;
// using System.Collections.Generic;
// 
// public class MonsterSpawnPoint : MonoBehaviour
// {
//     [Header("Spawn Settings")]
//     [Tooltip("EnemyData.tsv의 ID와 일치해야 함")]
//     public int enemyID; 
//     public float respawnDelay = 10.0f;
//     public float leashRange = 50.0f; // 스폰 지점에서 멀어질 수 있는 최대 거리
//     public int maxSpawnCount = 2;    // [수정] 이 포인트에서 최대 소환 가능한 수
// 
//     private List<GameObject> activeMonsters = new List<GameObject>(); // [수정] 리스트로 관리
//     private float nextSpawnTime; // [추가] 다음 스폰 가능 시간
// 
//     // 매니저에서 풀링된 객체를 가져와 호출합니다.
//     public void OnMonsterSpawned(GameObject monster)
//     {
//         if (monster == null) return;
//         
//         // 1. 위치 우선 설정 (에이전트 활성화 전)
//         monster.transform.SetPositionAndRotation(transform.position, transform.rotation);
//         
//         // 2. 리스트 등록
//         activeMonsters.Add(monster);
//         
//         // 3. 몬스터 타입별 초기화 및 에이전트 복구
//         if (monster.TryGetComponent<>(out var mushroom)) 
//         {
//             mushroom.SetSpawnPoint(this);
//         }
//         else if (monster.TryGetComponent<>(out var slime)) 
//         {
//             slime.SetSpawnPoint(this);
//         }
//         else if (monster.TryGetComponent<>(out var wildBoar)) 
//         {
//             wildBoar.SetSpawnPoint(this);
//         }
// 
//         // 4. 에이전트 강제 워프 (Warp를 써야 NavMesh 에러가 발생하지 않음)
//         var agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
//         if (agent != null)
//         {
//             agent.enabled = true;
//             agent.Warp(transform.position);
//         }
// 
//         nextSpawnTime = Time.time + respawnDelay;
//     }
// 
//    // private void OnDestroy()
//    // {
//    //     // 씬이 언로드될 때 필드에 남은 몬스터들을 매니저 풀로 강제 반환합니다.
//    //     if (MonsterSpawnManager.IsInitialized == true)
//    //     {
//    //         // 리스트의 복사본을 만들어 순회 (반환 중 리스트 수정으로 인한 에러 방지)
//    //         var tempActiveList = new List<GameObject>(activeMonsters);
//    //         foreach (var monster in tempActiveList)
//    //         {
//    //             if (monster != null)
//    //             {
//    //                 MonsterSpawnManager.Instance.ReleaseMonster(enemyID, monster);
//    //             }
//    //         }
//    //     }
//    //     activeMonsters.Clear();
//    // }
// 
//     // [수정] 어떤 몬스터가 죽었는지 인자로 받음
//     public void OnMonsterDead(GameObject monster)
//     {
//         if (activeMonsters.Contains(monster))
//         {
//             activeMonsters.Remove(monster);
//             
//             // 한 마리가 죽으면 다음 스폰 타이머 작동 (이미 지나갔더라도 다시 설정)
//             nextSpawnTime = Mathf.Max(nextSpawnTime, Time.time + respawnDelay);
//         }
//     }
// 
//     public bool ShouldRespawn()
//     {
//         // 1. 최대치보다 적게 소환되어 있고 2. 스폰 대기 시간이 지났을 때만 true
//         return activeMonsters.Count < maxSpawnCount && Time.time >= nextSpawnTime;
//     }
// 
// #if UNITY_EDITOR
//     private void OnDrawGizmos()
//     {
//         Gizmos.color = new Color(1, 0, 0, 0.3f);
//         Gizmos.DrawSphere(transform.position, 0.5f);
//         Gizmos.DrawWireSphere(transform.position, leashRange); // 귀환 거리 시각화
//     }
// #endif
// }