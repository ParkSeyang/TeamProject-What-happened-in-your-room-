namespace JSH
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Firebase;
    using Firebase.Auth;
    using UnityEngine;

    public sealed class FirebaseAuthManager : MonoBehaviour
    {
        private static FirebaseAuthManager instance;
        public static FirebaseAuthManager Instance => instance;

        private const string ID_DOMAIN = "@2parkshinjo.com";
        private FirebaseAuth auth;
        private FirebaseUser user;

        private readonly UniTaskCompletionSource initCompletionSource = new();
        private UniTask WaitUntilInitialized => initCompletionSource.Task;

        public FirebaseUser CurrentUser => user;
        public bool IsSignedIn => user != null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeFirebase().Forget();
        }

        private async UniTaskVoid InitializeFirebase()
        {
            Debug.Log("[FirebaseAuthManager] InitializeFirebase started.");
            try
            {
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();

                if (dependencyStatus == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    auth.StateChanged += OnAuthStateChanged;
                    UpdateUserReference();
                    initCompletionSource.TrySetResult();
                    Debug.Log("[FirebaseAuthManager] InitializeFirebase success.");
                }
                else
                {
                    Debug.LogError($"Firebase dependencies not met: {dependencyStatus}");
                    initCompletionSource.TrySetException(new Exception("Firebase Init Failed"));
                }
            }
            catch (Exception e)
            {
                initCompletionSource.TrySetException(e);
            }
        }

        private void OnAuthStateChanged(object sender, EventArgs eventArgs) => UpdateUserReference();

        private void UpdateUserReference()
        {
            if (auth == null) return;
            if (auth.CurrentUser != user)
            {
                user = auth.CurrentUser;
            }
        }

        private string GetEmailFromId(string id) => $"{id}{ID_DOMAIN}";

        public async UniTask<(bool success, string message)> SignUpWithIdAsync(string id, string password,
            CancellationToken ct = default)
        {
            Debug.Log($"[FirebaseAuthManager] SignUpWithIdAsync called. Waiting for Init... ID: {id}");
            await WaitUntilInitialized;
            Debug.Log("[FirebaseAuthManager] Init Wait Completed.");

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

            try
            {
                string email = GetEmailFromId(id);
                Debug.Log($"[FirebaseAuthManager] Attempting CreateUserWithEmailAndPasswordAsync. Email: {email}");
                await auth.CreateUserWithEmailAndPasswordAsync(email, password)
                    .AsUniTask()
                    .AttachExternalCancellation(linkedCts.Token);

                return (true, "회원가입 성공");
            }
            catch (FirebaseException e)
            {
                AuthError error = (AuthError)e.ErrorCode;
                Debug.LogError($"[FirebaseAuthManager] FirebaseException: {error}, {e.Message}");
                return (false, GetErrorMessage(error));
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthManager] Exception: {e.Message}");
                return (false, "알 수 없는 오류가 발생했습니다.");
            }
        }

        public async UniTask<(bool success, string message)> SignInWithIdAsync(string id, string password,
            CancellationToken ct = default)
        {
            Debug.Log($"[FirebaseAuthManager] SignInWithIdAsync called. Waiting for Init... ID: {id}");
            await WaitUntilInitialized;
            Debug.Log("[FirebaseAuthManager] Init Wait Completed.");

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

            try
            {
                string email = GetEmailFromId(id);
                Debug.Log($"[FirebaseAuthManager] Attempting SignInWithEmailAndPasswordAsync. Email: {email}");
                await auth.SignInWithEmailAndPasswordAsync(email, password)
                    .AsUniTask()
                    .AttachExternalCancellation(linkedCts.Token);

                return (true, "로그인 성공");
            }
            catch (FirebaseException e)
            {
                AuthError error = (AuthError)e.ErrorCode;
                Debug.LogError($"[FirebaseAuthManager] FirebaseException: {error}, {e.Message}");
                return (false, GetErrorMessage(error));
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthManager] Exception: {e.Message}");
                return (false, "알 수 없는 오류가 발생했습니다.");
            }
        }

        private string GetErrorMessage(AuthError error)
        {
            return error switch
            {
                AuthError.InvalidEmail => "유효하지 않은 ID 형식입니다.",
                AuthError.WrongPassword => "비밀번호가 틀렸습니다.",
                AuthError.UserNotFound => "존재하지 않는 계정입니다.",
                AuthError.EmailAlreadyInUse => "이미 사용 중인 ID입니다.",
                AuthError.WeakPassword => "비밀번호가 너무 취약합니다.",
                _ => "인증 오류가 발생했습니다: " + error.ToString()
            };
        }

        public void SignOut()
        {
            auth?.SignOut();
            user = null;
        }

        private void OnDestroy()
        {
            if (auth != null)
            {
                auth.StateChanged -= OnAuthStateChanged;
                auth = null;
            }
        }
    }
}