using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Newtonsoft.Json;
using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// Cloud Firestore 및 로컬 저장을 관리하는 매니저입니다.
    /// Rankings 컬렉션에 UID를 문서 ID로 사용하여 데이터를 관리합니다.
    /// </summary>
    public sealed class FirebaseFirestoreManager : SingletonBase<FirebaseFirestoreManager>
    {
        private FirebaseFirestore db;
        private const string COLLECTION_NAME = "Rankings";
        
        public UserData currentData { get; private set; }
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 유저 데이터가 로드되거나 변경되었을 때 발생하는 이벤트입니다.
        /// </summary>
        public Action OnDataUpdated;

        protected override void OnInitialize()
        {
            InitializeFirestore().Forget();
        }

        public async UniTask EnsureInitialized()
        {
            try
            {
                // 최대 3초만 기다리고 안 되면 로그를 남기고 진행합니다.
                await UniTask.WaitUntil(() => IsInitialized == true).Timeout(TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[FirebaseFirestoreManager] 초기화 대기 시간 초과. 제한된 기능으로 진행합니다.");
            }
        }

        private async UniTask InitializeFirestore()
        {
            if (IsInitialized == true) return;

            try
            {
                Debug.Log("[FirebaseFirestoreManager] Initializing Firestore...");
                
                // 1. FirebaseAuthManager가 완전히 초기화될 때까지 대기
                await FirebaseAuthManager.Instance.EnsureInitialized();

                // 2. Firestore 인스턴스 생성
                if (FirebaseAuthManager.Instance.App != null)
                {
                    db = FirebaseFirestore.GetInstance(FirebaseAuthManager.Instance.App);
                    IsInitialized = true;
                    Debug.Log("[FirebaseFirestoreManager] Firestore 초기화 완료.");
                }
                else
                {
                    Debug.LogError("[FirebaseFirestoreManager] FirebaseApp is NULL. Firestore cannot start.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] 초기화 중 예외 발생: {e.Message}");
            }
        }

        public async UniTask CreateInitialProfile(string uid, string id, string nickname)
        {
            await EnsureInitialized();

            if (db == null)
            {
                Debug.LogError("[FirebaseFirestoreManager] Cannot create profile: DB is NULL.");
                return;
            }

            // [최적화] 생성자 하나로 모든 초기 데이터 세팅을 완료합니다.
            string emailAddress = $"{id}@2parkshinjo.com";
            currentData = new UserData(uid, emailAddress, nickname);
            
            try
            {
                Debug.Log($"[FirebaseFirestoreManager] Creating profile for {nickname} (UID: {uid})...");
                await db.Collection(COLLECTION_NAME).Document(uid).SetAsync(currentData).AsUniTask();
                UserDataSystem.Instance.SaveUserData(currentData);
                Debug.Log($"[FirebaseFirestoreManager] 초기 프로필 생성 완료: {currentData.userName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] 초기 프로필 생성 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로그인 성공 시 로컬과 서버 데이터를 비교하여 동기화하고 최적의 데이터를 로드합니다.
        /// </summary>
        public async UniTask<UserData> LoadUserDataAsync(string uid)
        {
            Debug.Log($"[FirebaseFirestoreManager] === LoadUserDataAsync START (UID: {uid}) ===");
            await EnsureInitialized();

            if (db == null)
            {
                Debug.LogError("[FirebaseFirestoreManager] Firestore DB is NULL. Fallback to Local.");
                currentData = UserDataSystem.Instance.LoadUserData();
                return currentData;
            }

            try
            {
                // 1. 서버와 로컬 데이터 병렬 로드 시도
                Debug.Log("[FirebaseFirestoreManager] Trace: Step 1 - Data fetching started.");
                var serverTask = db.Collection(COLLECTION_NAME).Document(uid).GetSnapshotAsync().AsUniTask();
                UserData localData = UserDataSystem.Instance.LoadUserData();
                
                Debug.Log("[FirebaseFirestoreManager] Trace: Step 2 - Awaiting server snapshot...");
                DocumentSnapshot serverSnapshot = await serverTask;
                Debug.Log($"[FirebaseFirestoreManager] Trace: Step 2 - Received. (Exists: {serverSnapshot.Exists})");

                UserData serverData = null;
                if (serverSnapshot.Exists == true)
                {
                    serverData = serverSnapshot.ConvertTo<UserData>();
                }

                // 2. 데이터 비교 및 동기화 결정
                Debug.Log("[FirebaseFirestoreManager] Trace: Step 3 - Resolving conflict.");
                currentData = ResolveDataConflict(localData, serverData, uid);

                // 3. 최종 결정된 데이터를 양쪽에 전파
                Debug.Log("[FirebaseFirestoreManager] Trace: Step 4 - Synchronizing...");
                await SynchronizeData(currentData, localData, serverData);

                OnDataUpdated?.Invoke();
                Debug.Log($"[FirebaseFirestoreManager] === LoadUserDataAsync SUCCESS for {currentData?.userName} ===");
                return currentData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] !!! LoadUserDataAsync FATAL EXCEPTION !!! : {e.Message}");
                currentData = UserDataSystem.Instance.LoadUserData();
                return currentData;
            }
        }

        private UserData ResolveDataConflict(UserData local, UserData server, string uid)
        {
            if (local == null && server == null) return null;
            if (local == null) return server;
            if (server == null) return local;

            if (local.bestScore > server.bestScore) return local;
            if (server.bestScore > local.bestScore) return server;

            return local.lastUpdated >= server.lastUpdated ? local : server;
        }

        private async UniTask SynchronizeData(UserData winner, UserData local, UserData server)
        {
            if (winner == null) return;

            bool isServerOutdated = server == null || 
                                    winner.bestScore > server.bestScore || 
                                    winner.lastUpdated > server.lastUpdated;

            if (isServerOutdated == true)
            {
                Debug.Log("[FirebaseFirestoreManager] Sync: Uploading winner to server...");
                await db.Collection(COLLECTION_NAME).Document(winner.userUID).SetAsync(winner).AsUniTask();
            }

            bool isLocalOutdated = local == null || 
                                   winner.bestScore > local.bestScore || 
                                   winner.lastUpdated > local.lastUpdated;

            if (isLocalOutdated == true)
            {
                Debug.Log("[FirebaseFirestoreManager] Sync: Saving winner to local...");
                UserDataSystem.Instance.SaveUserData(winner);
            }
        }

        public async UniTask UpdateBestScore(int currentScore)
        {
            if (currentData == null) currentData = UserDataSystem.Instance.LoadUserData();
            if (currentData == null) return;

            if (currentScore <= currentData.bestScore) return;

            currentData.bestScore = currentScore;
            currentData.UpdateTimestamp();

            try
            {
                UserDataSystem.Instance.SaveUserData(currentData);
                if (db != null)
                {
                    await db.Collection(COLLECTION_NAME).Document(currentData.userUID).SetAsync(currentData).AsUniTask();
                }
                Debug.Log($"[FirebaseFirestoreManager] Best score updated: {currentScore}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreManager] Score update failed: {e.Message}");
            }
        }

        public UserData LoadLocal() => UserDataSystem.Instance.LoadUserData();
    }
}
