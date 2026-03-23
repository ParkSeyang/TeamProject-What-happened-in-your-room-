using System;
using System.IO;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Study.Firebase
{
    // Firestore란?
    // No-SQL 기반의 DataBase입니다. Document식으로 데이터를 관리하며
    // 비관계형 데이터 베이스라고 생각하시면 됩니다.
    // 관계형 데이터베이스는 엑셀, 스프레드 시트 처럼 표현할 수 있는 데이터이니까
    // 비 관계형 데이터베이스는 엑셀이나 스프레드시트 형태로 표현 할 수 없는
    // 자유롭게 표현 가능한 데이터베이스다 라고 생각하세요.
    
    // Firestore는 Collection과 Document라는 객체로 DB를 관리합니다.
    // Collection은 하위에 다량의 Document들을 갖고 있습니다
    // ex)
    // Collection
    // - Doc1
    // - Doc2
    // - Doc...
    // Collection은 키워드를 기반으로 조회가 가능하며, Document는 id를 기반으로 조회가 가능합니다.
    
    public class Study_FireStore : MonoBehaviour
    {
        public Study_FirebaseAuth auth;
        private FirebaseApp app => auth.App;

        private FirebaseFirestore firestore;
        
        private async void Start()
        {
            // 초기화 대기를 위한 테스트 코드 입니다.
            while (app == null)
            {
                await Task.Delay(1000);
            }
            
            Debug.Log("App 설정 완료됨. Firestore 설정 시작");

            // FirebaseFirestore에서 Firestore 인스턴스를 가져옵니다. 객체를 생성해서 가져옵니다.
            firestore = FirebaseFirestore.GetInstance(app);
            Debug.Log($"<color=green> FireStore App Loaded : {app.Name}</color=green>");
        }
        
        public void CreateUserDocument(string userId, string email, string displayName)
        {
            //문서를 생성합니다 => Document를 DB에 Write합니다

            // userID의 Document Reference를 메모리영역으로 가져옵니다.
            // 이 메모리는 유니티가 작동하는 메인 쓰레드가 사용중인 메모리 입니다.
            DocumentReference userDoc = firestore.Collection("users").Document(userId);
            
            // UserData에 매개변수로 들어온 정보를 매핑해줍니다.
            UserData userData = new UserData();
            userData.email = email;
            userData.displayName = displayName;
            userData.createdAt = Timestamp.GetCurrentTimestamp();

           //이제는 설정된 데잍어를 DB로 보냅니다. Create할 겁니다.
            userDoc.SetAsync(userData).ContinueWithOnMainThread(task =>
            {
                // 보내는 작업의 결과가 나왔을때 실행되는 코드 블록
                
                //예외처리 부터
                if (task.IsFaulted)
                {
                    // task가 실패하면 task.Exception 내용의 에러가 출력됨
                    Debug.LogError($"CreateUserDocument : {task.Exception}");
                    return;
                }
                
                Debug.Log($"CreateUserDocument :: Success");
            });
        }

        public async Task<User> ReadUserDocument(string uid)
        {
            //문서를 조회합니다 => DB에 uid의 Document가 있는지 물어봅니다. 있다면 Read합니다.

            DocumentSnapshot userSnapshot = await firestore.Collection("users")
                .Document(uid).GetSnapshotAsync();

            if (userSnapshot.Exists == false) return null;

            UserData userData = userSnapshot.ConvertTo<UserData>();
            User user = new User();
            user.ID = uid;
            user.DisplayName = userData.displayName;
            user.Email = userData.email;
            
            return user;
        }

        public void UpdateUserDocument(User user)
        {
            //문서를 갱신합니다 => Document를 DB에 Write합니다
            
            // user의 문서를 가져옵니다
            DocumentReference userDoc = firestore.Collection("users").Document(user.ID);

            // 특정한 필드만 업데이트를 하고 싶으면,
            // 필드의 이름과 값을 입력할 수 있는 UpdateAsync를 사용해야 합니다
            // 근데 보통 {DocumentReference}.SetAsync를 많이 사용함
            userDoc.UpdateAsync("displayName", user.DisplayName).ContinueWithOnMainThread(task => 
            {
                if (task.IsFaulted)
                {
                    // task가 실패하면 task.Exception 내용의 에러가 출력됨
                    Debug.LogError($"UpdateUserDocument : {task.Exception}");
                    return;
                }
                
                Debug.Log($"UpdateUserDocument :: Success");
            });

        }
    }
}