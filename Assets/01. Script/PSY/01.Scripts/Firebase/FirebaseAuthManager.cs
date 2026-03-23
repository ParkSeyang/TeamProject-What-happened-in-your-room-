using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ParkSeyang
{
    /// <summary>
    /// Firebase Authentication을 관리하는 싱글톤 매니저입니다.
    /// ID/PW 기반 인증과 상황별 피드백 메시지를 제공합니다.
    /// </summary>
    public sealed class FirebaseAuthManager : SingletonBase<FirebaseAuthManager>
    {
        public FirebaseApp App { get; private set; }
        private FirebaseAuth auth;
        private const string DOMAIN = "@2parkshinjo.com";

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 현재 로그인된 유저 정보를 반환합니다.
        /// </summary>
        public FirebaseUser CurrentUser => auth?.CurrentUser;

        /// <summary>
        /// 가장 최근에 로그인을 시도했거나 성공한 유저의 ID(이름)를 기억합니다.
        /// </summary>
        public string LastAttemptedId { get; private set; }

        public void UpdateLastAttemptedId(string id) => LastAttemptedId = id;

        protected override void OnInitialize()
        {
            // OnInitialize에서는 시작만 알리고, 실제 대기는 내부 로직에서 처리합니다.
            InitializeFirebase().Forget();
        }

        public async UniTask EnsureInitialized()
        {
            try
            {
                // 최대 3초만 기다리고 안되면 진행합니다.
                await UniTask.WaitUntil(() => IsInitialized == true).Timeout(TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[FirebaseAuthManager] Firebase 초기화 대기 시간 초과.");
            }
        }

        private async UniTask InitializeFirebase()
        {
            if (IsInitialized == true) return;

            try
            {
                // [안전] 종속성 체크에 타임아웃 추가
                var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
                var dependencyStatus = await dependencyTask.Timeout(TimeSpan.FromSeconds(5));

                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError($"[FirebaseAuthManager] Firebase 종속성 오류: {dependencyStatus}");
                    return;
                }

                App = await LoadFirebaseAppFromGoogleServicesFileAsync();
                if (App == null) App = FirebaseApp.DefaultInstance;

                auth = FirebaseAuth.GetAuth(App);
                
                IsInitialized = true;
                Debug.Log($"[FirebaseAuthManager] Firebase 초기화 완료 (App: {App.Name})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthManager] 초기화 중 예외 발생: {e.Message}");
                // 실패해도 플래그를 세워 무한 대기를 방지할지 고려 (여기서는 에러 로그만)
            }
        }

        private async UniTask<FirebaseApp> LoadFirebaseAppFromGoogleServicesFileAsync(string appName = "MainApp")
        {
            const string jsonFileNameForMobile = "google-services.json";
            const string jsonFileNameForDesktop = "google-services-desktop.json";
            string jsonFileNameTarget = jsonFileNameForDesktop;

#if UNITY_ANDROID && !UNITY_EDITOR
            jsonFileNameTarget = jsonFileNameForMobile;
#endif
            string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileNameTarget);
            string jsonText = string.Empty;

            if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            {
                using (var www = UnityWebRequest.Get(filePath))
                {
                    // UnityWebRequest를 UniTask로 변환하여 대기하는 표준 방식입니다.
                    await www.SendWebRequest().ToUniTask();
                    
                    if (www.result != UnityWebRequest.Result.Success) return null;
                    jsonText = www.downloadHandler.text;
                }
            }
            else
            {
                if (File.Exists(filePath) == false) return null;
                jsonText = await File.ReadAllTextAsync(filePath);
            }

            if (string.IsNullOrEmpty(jsonText) == true) return null;

            JObject root = JObject.Parse(jsonText);
            string projectId = root["project_info"]?["project_id"]?.ToString();
            string storageBucket = root["project_info"]?["storage_bucket"]?.ToString();
            string projectNumber = root["project_info"]?["project_number"]?.ToString();

            var client = root["client"]?[0];
            string appId = client?["client_info"]?["mobilesdk_app_id"]?.ToString();
            string apiKey = client?["api_key"]?[0]?["current_key"]?.ToString();

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(apiKey)) return null;

            AppOptions options = new AppOptions
            {
                ProjectId = projectId,
                StorageBucket = storageBucket,
                AppId = appId,
                ApiKey = apiKey,
                MessageSenderId = projectNumber
            };

            return FirebaseApp.Create(options, appName);
        }

        private string GetEmailFromId(string id) => $"{id}{DOMAIN}";

        /// <summary>
        /// 회원가입을 시도합니다.
        /// </summary>
        public async UniTask<(bool success, string message, string uid)> SignUpAsync(string id, string password)
        {
            if (auth == null) return (false, "시스템 초기화 중입니다.", string.Empty);

            try
            {
                string email = GetEmailFromId(id);
                var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password).AsUniTask();
                return (true, "회원가입 성공", result.User.UserId);
            }
            catch (FirebaseException e)
            {
                AuthError error = (AuthError)e.ErrorCode;
                return (false, $"회원가입 실패: {error}", string.Empty);
            }
        }

        /// <summary>
        /// 로그인을 시도하며 ZeroDarkMos 님의 설계에 따른 피드백을 반환합니다.
        /// </summary>
        public async UniTask<(bool success, string message, string uid)> SignInAsync(string id, string password)
        {
            if (auth == null) return (false, "시스템 초기화 중입니다.", string.Empty);

            try
            {
                string email = GetEmailFromId(id);
                var result = await auth.SignInWithEmailAndPasswordAsync(email, password).AsUniTask();
                return (true, "로그인 성공", result.User.UserId);
            }
            catch (FirebaseException e)
            {
                AuthError error = (AuthError)e.ErrorCode;
                
                // ZeroDarkMos 님 설계 피드백 조건 반영
                var feedback = error switch
                {
                    AuthError.UserNotFound => "없는 계정 입니다.",
                    AuthError.InvalidEmail => "없는 계정 입니다.",
                    AuthError.WrongPassword => "비밀번호를 틀리셨습니다.",
                    _ => $"로그인 오류: {error}"
                };
                return (false, feedback, string.Empty);
            }
        }

        public void SignOut() => auth?.SignOut();
    }
}
