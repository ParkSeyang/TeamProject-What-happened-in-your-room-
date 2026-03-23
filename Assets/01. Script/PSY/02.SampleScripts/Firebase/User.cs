using Firebase.Auth;
using Firebase.Firestore;

namespace Study.Firebase
{
    // User의 DataModel이다 라고 부릅니다.
    public class User
    {
        public string ID { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
       
        public static User Empty = new User();
    }

    // Firestore의 users 콜렉션의 문서의 DTO (Data Transfer Object)
    [FirestoreData] //선언해주고
    public struct UserData
    {
        //FirestoreProperty를 필드 앞에 입력을 해줍니다.
        [FirestoreProperty] public string email { get; set; }
        [FirestoreProperty] public string displayName { get; set; }
        [FirestoreProperty] public Timestamp createdAt { get; set; }
        [FirestoreProperty] public string role { get; set; }
    }
}