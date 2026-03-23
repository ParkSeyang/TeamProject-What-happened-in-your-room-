using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace ParkSeyang
{
    /// <summary>
    /// ZeroDarkMos 님의 설계에 따른 ID/PW 기반 로그인 및 회원가입 UI입니다.
    /// 별도의 회원가입 패널을 통해 닉네임과 계정 정보를 설정할 수 있습니다.
    /// </summary>
    public sealed class LoginUI : BaseUI
    {
        public override UIType UIType => UIType.Login;
        public override bool IsPopup => true;

        [Header("UI Root Elements")]
        [SerializeField] private GameObject backgroundImage;
        [SerializeField] private GameObject loginContent;
        [SerializeField] private GameObject registerPanel; // 회원가입 전용 패널

        [Header("Login Input Components")]
        [SerializeField] private TMP_InputField idInput;        
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Register Input Components")]
        [SerializeField] private TMP_InputField registerIdInput;
        [SerializeField] private TMP_InputField registerNicknameInput;
        [SerializeField] private TMP_InputField registerPasswordInput;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button openRegisterButton;   // 가입 패널 여는 버튼
        [SerializeField] private Button registerConfirmButton; // 실제 가입 완료 버튼
        [SerializeField] private Button registerCancelButton;  // 가입 취소 버튼
        [SerializeField] private Button closeButton;

        [Header("Feedback UI")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text registrationMessage;

        private bool isProcessing = false;

        protected override void Start()
        {
            base.Start();

            // 로그인 관련
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginButtonClicked);
            if (openRegisterButton != null) openRegisterButton.onClick.AddListener(OpenRegisterPanel);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClicked);

            // 회원가입 관련
            if (registerConfirmButton != null) registerConfirmButton.onClick.AddListener(OnRegisterConfirmClicked);
            if (registerCancelButton != null) registerCancelButton.onClick.AddListener(CloseRegisterPanel);

            ResetUI();
        }

        private void ResetUI()
        {
            isProcessing = false;
            if (statusText != null) 
            {
                statusText.text = "아이디와 비밀번호를 입력해주세요.";
                statusText.color = Color.white;
            }
            
            if (registrationMessage != null) 
            {
                registrationMessage.text = "아이디와 비밀번호를 입력해주세요.";
                registrationMessage.color = Color.white;
            }
            
            if (idInput != null) idInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            
            CloseRegisterPanel();
        }

        #region Login Logic

        private async void OnLoginButtonClicked()
        {
            if (isProcessing == true) return;
            
            if (string.IsNullOrWhiteSpace(idInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                SetStatus("아이디와 비밀번호를 모두 입력해주세요.", Color.yellow);
                return;
            }

            isProcessing = true;
            Debug.Log($"[LoginUI] Attempting login for ID: {idInput.text}");

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.UpdateLastAttemptedId(idInput.text);
            }

            try
            {
                var (success, message, uid) = await FirebaseAuthManager.Instance.SignInAsync(idInput.text, passwordInput.text);
                SetStatus(message, success ? Color.green : Color.red);

                if (success == true)
                {
                    await ProcessLoginSuccess(uid);
                }
                else
                {
                    isProcessing = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LoginUI] Login Error: {ex.Message}");
                isProcessing = false;
            }
        }

        private async UniTask ProcessLoginSuccess(string uid)
        {
            Debug.Log($"[LoginUI] Login SUCCESS. UID: {uid}. Syncing data...");

            try
            {
                await FirebaseFirestoreManager.Instance.LoadUserDataAsync(uid).Timeout(System.TimeSpan.FromSeconds(5));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoginUI] Data sync issue: {e.Message}");
            }

            await UniTask.Delay(System.TimeSpan.FromMilliseconds(300), delayType: DelayType.Realtime);
            
            if (GameSceneManager.Instance != null) GameSceneManager.Instance.ToLoading();
            else UnityEngine.SceneManagement.SceneManager.LoadScene("01_TestLoadingScene");
        }

        #endregion

        #region Register Logic

        private void OpenRegisterPanel()
        {
            if (registerPanel != null) registerPanel.SetActive(true);
            if (loginContent != null) loginContent.SetActive(false);
            
            // 가입 필드 초기화
            if (registerIdInput != null) registerIdInput.text = "";
            if (registerNicknameInput != null) registerNicknameInput.text = "";
            if (registerPasswordInput != null) registerPasswordInput.text = "";
            
            SetStatus("새로운 계정 정보를 입력하세요.", Color.white);
        }

        private void CloseRegisterPanel()
        {
            if (registerPanel != null) registerPanel.SetActive(false);
            if (loginContent != null) loginContent.SetActive(true);
        }

        private async void OnRegisterConfirmClicked()
        {
            if (isProcessing == true) return;

            string id = registerIdInput.text;
            string nickname = registerNicknameInput.text;
            string pw = registerPasswordInput.text;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(pw))
            {
                SetStatus("모든 정보를 입력해주세요.", Color.yellow);
                return;
            }

            isProcessing = true;
            SetStatus("회원가입 중...", Color.white);

            try
            {
                var (success, message, uid) = await FirebaseAuthManager.Instance.SignUpAsync(id, pw);

                if (success == true)
                {
                    Debug.Log($"[LoginUI] SignUp SUCCESS. Creating profile for {nickname}...");
                    await FirebaseFirestoreManager.Instance.CreateInitialProfile(uid, id, nickname);

                    SetStatus("회원가입 성공! 로그인을 진행하세요.", Color.green);
                    CloseRegisterPanel();
                    
                    // 로그인창에 가입한 ID 자동 입력 및 비밀번호창 정리
                    if (idInput != null) idInput.text = id;
                    if (passwordInput != null)
                    {
                        passwordInput.text = "";
                        passwordInput.ActivateInputField(); // 비번에 포커스 주기
                    }
                }
                else
                {
                    SetStatus(message, Color.red);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoginUI] Register Error: {e.Message}");
                SetStatus("회원가입 중 오류가 발생했습니다.", Color.red);
            }
            finally
            {
                isProcessing = false;
            }
        }

        #endregion

        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            if (registrationMessage != null)
            {
                registrationMessage.text = message;
                registrationMessage.color = color;
            }
        }

        private void OnCloseButtonClicked()
        {
            if (isProcessing == true) return;
            UIManager.Instance.SetUIActive(UIType.Login, false);
            UIManager.Instance.SetUIActive(UIType.Title, true);
        }

        public override void Open()
        {
            base.Open();
            ResetUI();
            if (backgroundImage != null) backgroundImage.SetActive(true);
        }

        public override void Close()
        {
            if (backgroundImage != null) backgroundImage.SetActive(false);
            base.Close();
        }
    }
}
