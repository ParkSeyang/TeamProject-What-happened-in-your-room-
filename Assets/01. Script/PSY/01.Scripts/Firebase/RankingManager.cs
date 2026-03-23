using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace ParkSeyang
{
    public sealed class RankingManager : SingletonBase<RankingManager>
    {
        private FirebaseFirestore db;
        private const string COLLECTION_NAME = "Rankings";

        public static bool IsInitialized { get; private set; }

        protected override void OnInitialize()
        {
            IsInitialized = false;
            InitializeRankingSystem().Forget();
        }

        private async UniTask InitializeRankingSystem()
        {
            if (IsInitialized == true) return;
            await FirebaseAuthManager.Instance.EnsureInitialized();
            try
            {
                if (FirebaseAuthManager.Instance.App != null)
                {
                    db = FirebaseFirestore.GetInstance(FirebaseAuthManager.Instance.App);
                    IsInitialized = true;
                    Debug.Log("[RankingManager] 랭킹 시스템 초기화 완료.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RankingManager] 초기화 오류: {e.Message}");
            }
        }

        public async UniTask EnsureInitialized()
        {
            try
            {
                await UniTask.WaitUntil(() => IsInitialized == true).Timeout(TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[RankingManager] 초기화 대기 시간 초과.");
            }
        }

        public async UniTask<List<UserData>> GetTopRankings(int limit = 15)
        {
            await EnsureInitialized();
            if (db == null) return new List<UserData>();

            List<UserData> rankings = new List<UserData>();

            try
            {
                Query query = db.Collection(COLLECTION_NAME).OrderByDescending("bestScore").Limit(limit);
                QuerySnapshot snapshot = await query.GetSnapshotAsync().AsUniTask();

                Debug.Log($"[RankingManager] Firestore Query 성공: {snapshot.Documents.Count()}개의 문서를 발견했습니다.");

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (document.Exists == false)
                    {
                        Debug.LogWarning($"[RankingManager] 문서({document.Id})가 존재하지 않습니다.");
                        continue;
                    }

                    try
                    {
                        var data = document.ToDictionary();
                        UserData profile = new UserData();

                        // [추적 1] 닉네임 확인
                        if (data.TryGetValue("userName", out object nameObj) && nameObj != null)
                        {
                            profile.userName = nameObj.ToString();
                        }
                        else
                        {
                            profile.userName = "Unknown";
                            Debug.LogWarning($"[RankingManager] 문서({document.Id})에 'userName' 필드가 없습니다.");
                        }

                        // [추적 2] 점수 확인
                        if (data.TryGetValue("bestScore", out object scoreObj) && scoreObj != null)
                        {
                            profile.bestScore = Convert.ToInt32(scoreObj);
                        }
                        else
                        {
                            profile.bestScore = 0;
                            Debug.LogWarning($"[RankingManager] 문서({document.Id})에 'bestScore' 필드가 없습니다.");
                        }

                        // [추적 3] UID 확인
                        if (data.TryGetValue("userUID", out object uidObj) && uidObj != null)
                            profile.userUID = uidObj.ToString();
                        else
                            profile.userUID = document.Id;

                        rankings.Add(profile);
                        Debug.Log($"[RankingManager] 파싱 완료: {profile.userName} - {profile.bestScore}점");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RankingManager] 문서({document.Id}) 데이터 파싱 중 치명적 오류: {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RankingManager] 쿼리 실행 중 오류: {e.Message}");
            }

            Debug.Log($"[RankingManager] 최종 반환 리스트 개수: {rankings.Count}개");
            return rankings.OrderByDescending(u => u.bestScore).ToList();
        }
    }
}
