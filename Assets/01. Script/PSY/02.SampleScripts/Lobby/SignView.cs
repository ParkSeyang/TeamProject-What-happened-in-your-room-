using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 로그인 및 회원가입을 담당하는 초기 진입 뷰
/// </summary>
public class SignView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyView lobbyView;

    private string email = "";
    private string password = "";
    private string statusMessage = "Welcome! Please log in.";
    private bool isProcessing = false;

    private void Start()
    {
        // 시작 시 이미 로그인되어 있는지 체크 (FirebaseManager.Instance가 준비된 후)
        CheckAutoLogin();
    }

    private async void CheckAutoLogin()
    {
        // Firebase 초기화 대기 (간단한 지연)
        await Task.Delay(1000);
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsLoggedIn)
        {
            EnterLobby();
        }
    }

    private void OnGUI()
    {
        // 로비가 활성화되어 있으면 자기 자신은 그리지 않음
        if (lobbyView != null && lobbyView.gameObject.activeSelf) return;

        float w = 350;
        float h = 300;
        Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);

        GUI.Box(rect, "<b>AR Monster : Auth</b>", new GUIStyle(GUI.skin.box) { richText = true });
        GUILayout.BeginArea(new Rect(rect.x + 20, rect.y + 30, w - 40, h - 40));
        GUILayout.BeginVertical();

        GUILayout.Space(10);
        GUILayout.Label("Email Address");
        email = GUILayout.TextField(email);

        GUILayout.Space(5);
        GUILayout.Label("Password");
        password = GUILayout.PasswordField(password, '*', 20);

        GUILayout.Space(20);

        GUI.enabled = !isProcessing;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("LOGIN", GUILayout.Height(45)))
        {
            HandleSignIn();
        }
        if (GUILayout.Button("SIGN UP", GUILayout.Height(45)))
        {
            HandleSignUp();
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;

        GUILayout.Space(15);
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
        GUILayout.Label($"<color=yellow>{statusMessage}</color>", statusStyle);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private async void HandleSignIn()
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusMessage = "Please enter both email and password.";
            return;
        }

        isProcessing = true;
        statusMessage = "Authenticating...";

        bool success = await FirebaseManager.Instance.SignIn(email, password);
        
        if (success)
        {
            statusMessage = "<color=lime>Login Success!</color>";
            EnterLobby();
        }
        else
        {
            statusMessage = "<color=red>Login Failed. Please try again.</color>";
        }
        isProcessing = false;
    }

    private async void HandleSignUp()
    {
        if (string.IsNullOrEmpty(email) || password.Length < 6)
        {
            statusMessage = "Email is required and password must be at least 6 chars.";
            return;
        }

        isProcessing = true;
        statusMessage = "Creating account...";

        bool success = await FirebaseManager.Instance.SignUp(email, password);

        if (success)
        {
            statusMessage = "<color=lime>Account Created & Logged In!</color>";
            EnterLobby();
        }
        else
        {
            statusMessage = "<color=red>Sign Up Failed. Email may be in use.</color>";
        }
        isProcessing = false;
    }

    private void EnterLobby()
    {
        if (lobbyView != null)
        {
            lobbyView.gameObject.SetActive(true);
            // SignView를 완전히 끄거나 그리지 않도록 설정
            this.gameObject.SetActive(false);
        }
    }
}
