using System;
using Firebase.Firestore;

namespace ParkSeyang
{
    /// <summary>
    /// Firestore의 Rankings 컬렉션 문서와 1:1 매칭되는 데이터 모델(DTO)입니다.
    /// </summary>
    [FirestoreData]
    [Serializable]
    public class UserData
    {
        [FirestoreProperty] public string userUID { get; set; }        // Firebase 고유 UID
        [FirestoreProperty] public string email { get; set; }          // 가입된 이메일
        [FirestoreProperty] public string userName { get; set; }       // 유저 닉네임 (게스트1호 등)
        [FirestoreProperty] public int bestScore { get; set; }         // 최고 기록 점수
        
        [FirestoreProperty] public Timestamp lastUpdated { get; set; } // 서버 정합성용 (초기엔 null 가능)
        
        [FirestoreProperty] public string lastUpdatedDate { get; set; } // 시간 기록용 (YY:HH:MM:SS)

        // Firestore 역직렬화를 위한 필수 기본 생성자
        public UserData() { }

        /// <summary>
        /// 회원가입 시 호출되는 초기 프로필 생성자입니다.
        /// </summary>
        public UserData(string uid, string emailAddress, string nickname)
        {
            userUID = uid;
            email = emailAddress;
            userName = nickname; // 사용자가 입력한 실제 닉네임 반영
            bestScore = 0;
            
            // 요청에 따른 시간 기록 (YY:HH:MM:SS)
            lastUpdatedDate = DateTime.Now.ToString("yy:HH:mm:ss");
            
            // 초기 서버 정합성용 타임스탬프 설정
            lastUpdated = Timestamp.GetCurrentTimestamp();
        }

        /// <summary>
        /// 점수 경신 시 호출하여 타임스탬프와 기록용 시간을 갱신합니다.
        /// </summary>
        public void UpdateTimestamp()
        {
            DateTime now = DateTime.Now;
            lastUpdated = Timestamp.FromDateTime(now.ToUniversalTime());
            lastUpdatedDate = now.ToString("yy:HH:mm:ss");
        }
    }
}
