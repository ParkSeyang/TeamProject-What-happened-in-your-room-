/*
using UnityEngine;
using System;

namespace ParkSeyang
{
    /// <summary>
    /// 유저의 핵심 데이터를 담는 클래스입니다.
    /// 1. userId: 로그인 시 입력한 아이디 (닉네임 표시용)
    /// 2. userUID: Firebase 고유 UID (데이터 식별용)
    /// 3. bestScore: 최고 점수 기록
    /// </summary>
    [Serializable]
    public class UserData
    {
        public string userId;      // 로그인 시 사용한 ID (로비 닉네임)
        public string userUID;     // Firebase UID
        public int bestScore;      // 최고 기록 점수
        
        // 데이터 최신성 확인을 위한 타임스탬프 (시스템 판정용 및 가독성용)
        public long lastUpdated;
        public string lastUpdatedDate;

        public UserData()
        {
            // 기본 생성자 (JSON 복원용)
        }

        /// <summary>
        /// 신규 유저 생성 시 호출하는 생성자입니다.
        /// </summary>
        public UserData(string id, string uid)
        {
            userId = id;
            userUID = uid;
            bestScore = 0;
            UpdateTimestamp();
        }

        /// <summary>
        /// 데이터 저장 직전에 호출하여 마지막 수정 시간을 기록합니다.
        /// </summary>
        public void UpdateTimestamp()
        {
            DateTime now = DateTime.Now;
            lastUpdated = now.Ticks;
            lastUpdatedDate = now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
*/
