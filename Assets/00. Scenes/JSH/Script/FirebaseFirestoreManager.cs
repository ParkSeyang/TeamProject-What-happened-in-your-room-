namespace JSH
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Cysharp.Threading.Tasks;
    using Firebase;
    using Firebase.Firestore;
    using Newtonsoft.Json;
    using UnityEngine;

    
    // [가이드 반영] 직렬화를 위한 유저 프로필 데이터 구조
    
    [Serializable]
    public sealed class UserProfile
    {
        public string userId;
        public string userName;
        public int bestScore;
        public string lastUpdated;

        public UserProfile() { }

        public UserProfile(string id, string name, int score)
        {
            userId = id;
            userName = name;
            bestScore = score;
            lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public sealed class FirebaseFirestoreManager : MonoBehaviour
    {
        private static FirebaseFirestoreManager instance;
        public static FirebaseFirestoreManager Instance => instance;

        private FirebaseFirestore db;
        private const string SCORE_COLLECTION = "Rankings";
        private string LocalSavePath => Path.Combine(Application.persistentDataPath, "user_profile.json");
        
        // 초기화 완료 여부를 추적하기 위한 소스
        private readonly UniTaskCompletionSource initCompletionSource = new();
        public UniTask WaitUntilInitialized => initCompletionSource.Task;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeFirestore().Forget();
        }

        private async UniTaskVoid InitializeFirestore()
        {
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
                
                if (status == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;
                    Debug.Log("[FirebaseFirestoreManager] Firestore initialized successfully.");
                    initCompletionSource.TrySetResult();
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreManager] Could not resolve all Firebase dependencies: {status}");
                    initCompletionSource.TrySetException(new Exception($"Firebase dependencies not met: {status}"));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                initCompletionSource.TrySetException(e);
            }
        }

       
        //[가이드 반영] 유저 데이터를 Firestore에 업로드하고 로컬에 캐싱합니다.
        
        public async UniTask UploadScoreWithCacheAsync(string userId, int score)
        {
            await WaitUntilInitialized;

            if (db == null) return;

            // 1. 기존 데이터 확인 (현재 점수가 최고 점수일 때만 업로드)
            UserProfile currentProfile = LoadLocalProfile();
            if (currentProfile != null && score <= currentProfile.bestScore)
            {
                Debug.Log("[FirebaseFirestoreManager] 현재 점수가 최고 점수보다 낮아 업로드를 건너뜁니다.");
                return;
            }

            // 2. 새 프로필 생성 및 로컬 저장
            var newProfile = new UserProfile(userId, "Player_" + userId.Substring(0, 5), score);
            SaveLocalProfile(newProfile);

            // 3. Firestore 업로드 (UID를 문서 ID로 사용)
            var scoreData = new Dictionary<string, object>
            {
                { "userId", newProfile.userId },
                { "userName", newProfile.userName },
                { "bestScore", newProfile.bestScore },
                { "timestamp", FieldValue.ServerTimestamp }
            };

            try
            {
                // Rankings (Collection) / {UID} (Document)
                await db.Collection(SCORE_COLLECTION).Document(userId).SetAsync(scoreData).AsUniTask();
                Debug.Log($"[FirebaseFirestoreManager] Firestore upload success: {score} for {userId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] Firestore upload failed: {e.Message}");
            }
        }

        private void SaveLocalProfile(UserProfile profile)
        {
            try
            {
                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(LocalSavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] Local save failed: {e.Message}");
            }
        }

        private UserProfile LoadLocalProfile()
        {
            if (File.Exists(LocalSavePath) == false) return null;

            try
            {
                string json = File.ReadAllText(LocalSavePath);
                return JsonConvert.DeserializeObject<UserProfile>(json);
            }
            catch
            {
                return null;
            }
        }

        
        // 상위 점수 랭킹을 가져옵니다.
        
        public async UniTask<List<Dictionary<string, object>>> GetTopScoresAsync(int limit = 10)
        {
            await WaitUntilInitialized;

            if (db == null) return null;

            try
            {
                // 가이드 쿼리: OrderByDescending("bestScore").Limit(10)
                var snapshot = await db.Collection(SCORE_COLLECTION)
                    .OrderByDescending("bestScore")
                    .Limit(limit)
                    .GetSnapshotAsync()
                    .AsUniTask();

                var results = new List<Dictionary<string, object>>();
                foreach (var doc in snapshot.Documents)
                {
                    results.Add(doc.ToDictionary());
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] Failed to get rankings: {e.Message}");
                return null;
            }
        }
    }
}
