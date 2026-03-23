using System;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Study.Firebase;

public class LoginView : MonoBehaviour
{
    public Study_FirebaseAuth studyFirebaseAuth;
    public Study_FireStore studyFireStore;
    
    private FirebaseAuth auth;
    private User User => studyFirebaseAuth.User; //보통 디바이스에서 사용자는 한명이어서 싱글톤이나 static 객체로 해도 무방합니다.

    private string password = "";
    private string statusText = "";

    private void OnGUI()
    {
        // GUI 레이아웃 설정
        GUILayout.BeginArea(new Rect(20, 20, 400, 600));
        
        GUILayout.Label("<size=30>Firebase Authentication</size>");
        GUILayout.Space(20);

        GUILayout.Label("Email");
        User.Email = GUILayout.TextField(User.Email, GUILayout.Width(300));

        GUILayout.Label("Password");
        password = GUILayout.PasswordField
            (password, '*', GUILayout.Width(300));

        GUILayout.Space(10);

        // 로그인 및 계정 생성 버튼
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Login", GUILayout.Height(40), GUILayout.Width(148)))
        {
            SignIn(User.Email, password);
        }
        if (GUILayout.Button("Create Account", GUILayout.Height(40), GUILayout.Width(148)))
        {
            SignUp(User.Email, password);
        }
        GUILayout.EndHorizontal();

        // 익명 로그인 버튼
        if (GUILayout.Button("Anonymous Login", GUILayout.Height(40), GUILayout.Width(300)))
        {
            SignInAnonymously();
        }

        GUILayout.Space(20);
        
        if (GUILayout.Button("Sign Out", GUILayout.Height(40), GUILayout.Width(300)))
        {
            studyFirebaseAuth.SignOut();
        }
        
        // 상태 메시지 출력
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.wordWrap = true;
        GUILayout.Label($"Status: {statusText}", statusStyle, GUILayout.Width(300));

        GUILayout.Space(20);
        GUILayout.Label("<size=30>Firebase Firestore</size>");
        
        GUILayout.Space(20);
        if (GUILayout.Button("Create UserDoc", GUILayout.Height(40), GUILayout.Width(300)))
        {
            studyFireStore.CreateUserDocument(User.ID, User.Email, User.DisplayName);
        }
        
        GUILayout.Label("Change Your DisplayName");
        User.DisplayName = GUILayout.TextField(User.DisplayName, GUILayout.Width(300));
        
        if (GUILayout.Button("Update UserDoc", GUILayout.Height(40), GUILayout.Width(300)))
        {
            studyFireStore.UpdateUserDocument(User);
        }
        
        GUILayout.EndArea();
    }

    // 계정 생성 (이메일/비밀번호)
    private void SignUp(string email, string password)
    {
        studyFirebaseAuth.SignUp(email, password);
    }

    // 로그인 (이메일/비밀번호)
    private void SignIn(string email, string password)
    {
        studyFirebaseAuth.SignIn(email, password);
    }

    // 익명 로그인
    private void SignInAnonymously()
    {
        studyFirebaseAuth.SignInAnonymously();
    }
}
