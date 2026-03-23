/*
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using UnityEngine;
using JSH; 

namespace ParkSeyang
{
    /// <summary>
    /// 사용자 인증(로그인/회원가입)의 핵심 비즈니스 로직을 담당하는 컨트롤러입니다.
    /// LoginUI와 연동하여 Firebase Auth 서비스를 중계합니다.
    /// </summary>
    [Serializable]
    public class AuthUIController
    {
        public async UniTask<(bool success, string message)> LoginAsync(string id, string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "ID와 비밀번호를 모두 입력해주세요.");
            }

            Debug.Log($"[AuthUIController] Login attempt for ID: {id}");
            //return await FirebaseAuthManager.Instance.SignInWithIdAsync(id, password, ct);
            
            (bool, string) reVal = new();
            string message = "";

            try
            {
                //아래 부분을 익명로그인이 아니라 // FirebaseManager.Instance.SignIn(id, password);  
                var result = await FirebaseManager.Instance.SignInAnonymously();
                
                reVal.Item1 = result;
            }
            catch (FirebaseException e)
            {
                reVal.Item1 = false;
                reVal.Item2 = e.Message;

                throw e;
            }
            
            return reVal;
        }

        public async UniTask<(bool success, string message)> SignUpAsync(string id, string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "ID와 비밀번호를 모두 입력해주세요.");
            }

            if (password.Length < 6)
            {
                return (false, "비밀번호는 최소 6자 이상이어야 합니다.");
            }

            Debug.Log($"[AuthUIController] SignUp attempt for ID: {id}");
            //return await FirebaseAuthManager.Instance.SignUpWithIdAsync(id, password, ct);

            (bool, string) reVal = new();
            string message = "";
            
            try
            {
                var result = await FirebaseManager.Instance.SignUp(id, password);
                reVal.Item1 = result;
            }
            catch (FirebaseException e)
            {
                reVal.Item1 = false;
                reVal.Item2 = e.Message;

                throw e;
            }

            return reVal;
        }
    }
}
*/
