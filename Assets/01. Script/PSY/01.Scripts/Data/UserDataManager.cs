/*
using System;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Firebase.Firestore;

namespace ParkSeyang
{
    /// <summary>
    /// 유저의 진행도, 자원, 프로필 등 실질적인 데이터를 관리하는 매니저입니다.
    /// 로컬 JSON 백업과 Firebase Firestore 연동을 지원합니다.
    /// </summary>
    public class UserDataManager : SingletonBase<UserDataManager>
    {
        public UserData CurrentUserData { get; private set; }
        public bool IsDataLoaded { get; private set; }

        /// <summary>
        /// 로그인 성공 시 입력했던 ID를 로딩 프로세스에 전달하기 위해 임시 보관합니다.
        /// </summary>
        public string LastLoginId { get; set; }

        /// <summary>
        /// 유저 데이터가 변경되거나 새로 로드되었을 때 발생하는 이벤트입니다.
        /// </summary>
        public Action OnUserDataChanged;

        private const string COLLECTION_NAME = "users";
        private string GetLocalPath(string uid) => Path.Combine(Application.persistentDataPath, $"UserData_{uid}.json");

        protected override void OnInitialize()
        {
            base.OnInitialize();
            IsDataLoaded = false;
        }

        /// <summary>
        /// 서버 또는 로컬 저장소로부터 유저 데이터를 비동기적으로 불러옵니다 (하이브리드 로직).
        /// </summary>
        public async UniTask LoadUserDataAsync(string loginId)
        {
            // [추가] 이미 데이터가 로드된 상태라면 중복 로딩을 피합니다.
            if (IsDataLoaded == true && CurrentUserData != null)
            {
                Debug.Log("[UserDataManager] Data is already loaded. Skipping redundant fetch.");
                return;
            }

            IsDataLoaded = false;
            
            // 1. UID 확인 (JSH 네임스페이스 활용)
            string uid = JSH.FirebaseAuthManager.Instance?.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogError("[UserDataManager] User is not signed in!");
                IsDataLoaded = true; // 무한 로딩 방지
                return;
            }

            try
            {
                // 2. 서버와 로컬 데이터 병렬 로드 시도 (최대 5초 타임아웃)
                Debug.Log("[UserDataManager] Fetching data from Cloud...");
                var docRef = FirebaseFirestore.DefaultInstance.Collection(COLLECTION_NAME).Document(uid);
                
                var snapshot = await docRef.GetSnapshotAsync().AsUniTask().Timeout(TimeSpan.FromSeconds(5));
                UserData localData = LoadLocal(uid);

                UserData serverData = null;
                if (snapshot.Exists)
                {
                    string json = JsonConvert.SerializeObject(snapshot.ToDictionary());
                    serverData = JsonConvert.DeserializeObject<UserData>(json);
                    Debug.Log("[UserDataManager] Server data received.");
                }

                // 3. 충돌 해결 및 최종 데이터 결정
                if (serverData != null && localData != null)
                {
                    CurrentUserData = (localData.lastUpdated > serverData.lastUpdated) ? localData : serverData;
                    // 이름(ID)이 누락된 경우 보정
                    if (string.IsNullOrEmpty(CurrentUserData.userId)) CurrentUserData.userId = loginId;
                    
                    if (localData.lastUpdated > serverData.lastUpdated) await SaveUserDataAsync(); 
                }
                else
                {
                    // [핵심 수정] 데이터가 전혀 없는 신규 유저일 경우, 입력받은 loginId와 발급받은 uid로 생성
                    CurrentUserData = serverData ?? localData ?? new UserData(loginId, uid);
                }

                if (CurrentUserData != null) SaveLocal(uid, CurrentUserData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataManager] Load failed or timed out: {e.Message}. Fallback to local.");
                CurrentUserData = LoadLocal(uid) ?? new UserData(loginId, uid);
            }

            IsDataLoaded = true;
            OnUserDataChanged?.Invoke();
            Debug.Log("[UserDataManager] User data process finished.");
        }

        /// <summary>
        /// 유저 데이터를 서버(Firebase)와 로컬(JSON)에 동시에 저장합니다.
        /// </summary>
        public async UniTask SaveUserDataAsync()
        {
            if (CurrentUserData == null) return;
            string uid = JSH.FirebaseAuthManager.Instance?.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(uid)) return;

            // [추가] 저장 시점에 날짜와 시간 정보를 강제로 갱신합니다.
            CurrentUserData.UpdateTimestamp();

            // 1. 로컬 저장 (백업)
            SaveLocal(uid, CurrentUserData);

            // 2. Firebase Firestore 저장
            try
            {
                var docRef = FirebaseFirestore.DefaultInstance.Collection(COLLECTION_NAME).Document(uid);
                string json = JsonConvert.SerializeObject(CurrentUserData);
                var dataDict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(json);
                await docRef.SetAsync(dataDict);
                
                OnUserDataChanged?.Invoke();
                Debug.Log("[UserDataManager] Data saved to Firebase & Local.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataManager] Firebase save failed: {e.Message}");
            }
        }

        private void SaveLocal(string uid, UserData data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(GetLocalPath(uid), json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserDataManager] Local save failed: {e.Message}");
            }
        }

        private UserData LoadLocal(string uid)
        {
            string path = GetLocalPath(uid);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UserData>(json);
            }
            catch
            {
                return null;
            }
        }

        public void ClearData()
        {
            CurrentUserData = null;
            IsDataLoaded = false;
        }

        /// <summary>
        /// 새로운 점수를 받아 기존 최고 기록보다 높으면 갱신하고 서버와 로컬에 즉시 저장합니다.
        /// </summary>
        public async UniTask<bool> TryUpdateBestScore(int newScore)
        {
            if (CurrentUserData == null)
            {
                Debug.LogWarning("[UserDataManager] User data is not loaded!");
                return false;
            }

            // 1. 기존 기록보다 높은지 확인
            if (newScore > CurrentUserData.bestScore)
            {
                int oldScore = CurrentUserData.bestScore;
                CurrentUserData.bestScore = newScore;
                
                Debug.Log($"[UserDataManager] New Best Score! {oldScore} -> {newScore}. Saving...");

                // 2. 서버와 로컬에 즉시 동기화 (UpdateTimestamp는 Save 내부에서 처리됨)
                await SaveUserDataAsync();
                
                return true;
            }

            Debug.Log($"[UserDataManager] Current score ({newScore}) is not higher than best score ({CurrentUserData.bestScore}).");
            return false;
        }
    }
}
*/
