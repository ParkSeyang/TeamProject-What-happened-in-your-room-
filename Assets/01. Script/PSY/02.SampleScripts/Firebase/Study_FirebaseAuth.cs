using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Newtonsoft.Json.Linq;
using Study.Firebase;
using UnityEngine.Networking;

// 원래는 지금 내용부터 FireStore(비 관계형 DB)
// 강의장 네트워크 문제로 해당 부분은 문제가 해결 된 뒤에 진행하겠습니다.

// 네트워크는 절차 및 진행 확인을 위해서
// 기존의 함수 선언 순서(public -> private) 규칙을 무시하고
// 네트워크 절차대로 선언하여 관리합니다.
// 함수의 실행 순서가 위에서부터 아래로 차례대로 호출된다고 생각하시면서 보세요

public class Study_FirebaseAuth : MonoBehaviour
{
    // Firebase SDK(Software Development Kit)가 어떤 형식으로 이루어져있는지
    // 확인을 하는게 중요합니다

    public User User = new User();

    public FirebaseApp App { get; private set; }
    private FirebaseAuth auth;
    
    async void Start()
    {
        // Firebase의 초기화 절차
        // 1. App의 종속성을 확인 합니다
        // 2. 확인이 끝나면 FirebaseApp의 인스턴스를 생성 요청 합니다.
        // (프로그램 하나가 더 뜬다라고 생각하면됨)
        // 3. auth기능을 수행할 인스턴스를 생성 요청 합니다.
        // 4. 초기화 완료가 된겁니다.

        await FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(async task =>
            {
                var result = task.Result;

                // 이용가능한 상태가 아니라면
                if (result != DependencyStatus.Available)
                {
                    // 예외 처리를 해줍니다
                    Debug.LogError($"Firebase Error : {result.ToString()}");
                    return;
                }
                
                // 이 시점에서는 Firebase를 이용가능한 상태가 됩니다.
                try
                {
                    App = await LoadFirebaseAppFromGoogleServicesFileAsync();
                }
                catch (Exception e)
                {
                    throw new Exception($"Firebase Error : {e.Message}");
                }
                
                // app자체가 생성 완료된 상황
                Debug.Log("<color=green> Firebase App Loaded </color=green>");
                
                // Auth 객체를 생성할겁니다. (생성한 app에서 auth객체를 가지고 오는것)
                auth = FirebaseAuth.GetAuth(App);
            });
    }

    // appName은 유저의 디바이스 환경에서는 실제 AppName이 되어도 상관은 없으나,
    // AppName을 사용할 경우 여러분들이 Firebase를 이용해서 데스크톱 환경에서
    // 멀티플레이 게임을 테스트할때 에러가 발생할 수 있습니다.
    private async Task<FirebaseApp> LoadFirebaseAppFromGoogleServicesFileAsync(string appName = "CustomApp")
    {
        const string jsonFileNameForMobile = "google-services.json";
        const string jsonFileNameForDesktop = "google-services-desktop.json";

        string jsonFileNameTarget = jsonFileNameForDesktop;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        // UNITY_ANDROID && !UNITY_EDITOR = Android 디바이스에서 실행이 된다는 말입니다
        jsonFileNameTarget = jsonFileNameForMobile;
#endif
        // 위의 내용이 아니라면 데스크톱 환경이라는 말
        string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileNameTarget);

#if UNITY_ANDROID && !UNITY_EDITOR
        var www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
        await SendRequestAsync(www);

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"{jsonFileNameTarget} 읽기 실패: {www.error}");
            return null;
        }

        string jsonText = www.downloadHandler.text;
#else

        if (File.Exists(filePath) == false) 
        {
            Debug.LogError($"{jsonFileNameTarget} 없음.");
            return null;
        }

        string jsonText = await File.ReadAllTextAsync(filePath);
#endif

        JObject root = JObject.Parse(jsonText);

        string projectId = root["project_info"]?["project_id"]?.ToString();
        string storageBucket = root["project_info"]?["storage_bucket"]?.ToString();
        string projectNumber = root["project_info"]?["project_number"]?.ToString();

        var client = root["client"]?[0];
        string appId = client?["client_info"]?["mobilesdk_app_id"]?.ToString();
        string apiKey = client?["api_key"]?[0]?["current_key"]?.ToString();

        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError($"{jsonFileNameTarget} 파일 확인 필요");
            return null;
        }

        // 모든 정보가 유효하다면
        // 해당 내용을 토대로 AppOptions 파일을 생성합니다.
        AppOptions options = new AppOptions
        {
            ProjectId = projectId,
            StorageBucket = storageBucket,
            AppId = appId,
            ApiKey = apiKey,
            MessageSenderId = projectNumber
        };

        // 만들어낸 Option파일(google-services 파일을 기반으로 FirebaseApp을 생성합니다)
        FirebaseApp app = FirebaseApp.Create(options, appName);
        return app;
        
    }

    public static async Task<UnityWebRequest> SendRequestAsync(UnityWebRequest request)
    {
        var asyncOp = request.SendWebRequest();

        while (asyncOp.isDone == false)
            await Task.Yield();

        return request;
    }
    
    // Firebase를 연동했을때 주 기능들이 되는겁니다 
    #region Public API

    // 익명 로그인 (게스트 로그인이라고 생각하시면 됩니다.)
    public void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            // 익명로그인 task 실패 예외처리
            if (task.IsCanceled || task.IsFaulted)
            {
                //만약에 실패하면 error만 출려하고 return 합니다
                Debug.LogError($"SignInAnonymously : {task.Exception}");
                return;
            }

            var result = task.Result;
            FirebaseUser newUser = result.User; //Firebase의 유저 정보를 갖고 있는 유저 객체
            
            Debug.Log("<color=green> Firebase SignInAnonymously Success </color=green>");
            Debug.Log($"{newUser.UserId}, {newUser.DisplayName}, {newUser.Email}");
        });
    }

    public void SignUp(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                // Task관련된 예외처리
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"SignUp : {task.Exception}");
                    // 중복된 email이라거나
                    // 비밀번호가 최소요건을 만족시키지 못한다거나 하는
                    return;
                }

                Debug.Log($"<color=green> Create Account : {email}, {password} </color>");

                var uid = task.Result.User.UserId;
                SignIn(email, password);
            });
    }

    public void SignIn(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(async task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError($"SignIn : {task.Exception}");
                // Email이 없다거나 비밀번호가 다르다거나 기타 등등
                return;
            }

            var result = task.Result;
            // 회원가입 이미 된 상황일 경우에는 User객체를 새롭게 생성해줘야 하니까.
            FirebaseUser newUser = result.User;
           

            User = await GetComponent<Study_FireStore>().ReadUserDocument(newUser.UserId);
            
            // User 문서가 없다면 => 신규 유저! 문서 기반이 아닌 FirebaseUser기반의 새로운 User를 생성합니다.
            if (User == null)
            {
                User = new User() 
                    // Firebase에서 다루는 User와 App에서 다루는 User를 분리하는게 좋습니다.
                    // 저는 단순복사로 보여드리지만, ref를 가져오셔도 됩니다. User 클래스 안에 FirebaseUser를 넣는
                    // 종속성을 주입하셔도 좋습니다.
                    {
                        DisplayName = newUser.DisplayName,
                        Email = newUser.Email,
                        ID = newUser.UserId,
                    };
            }
            Debug.Log($"<color=green> SignIn : {User.ID}, {User.DisplayName}, {User.Email} </color>");
        });
    }

    public void SignOut()
    {
        auth.SignOut(); // 로그아웃이 실패할리가 없다. 로그아웃이 실패하는 App 본적있으심??
        User = User.Empty;
        Debug.Log($"<color=green> SignOut : {User.ID} </color>");
    }

    #endregion
}
