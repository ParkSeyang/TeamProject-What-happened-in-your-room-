using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// AR 환경의 플레이어 정보를 관리하는 핵심 클래스입니다.
    /// 사용자 식별 정보(ID)와 최종 성과(Score) 관리에 집중합니다.
    /// </summary>
    public class ARPlayer : MonoBehaviour, ICleanAgent
    {
        [Header("User Info")]
        [SerializeField] private string userId = "User_ZeroDarkMos"; 

        [Header("Status (Score)")]
        [SerializeField] private float currentScore = 0f; 

        // --- ICleanAgent Interface Implementation ---

        public GameObject GameObject => gameObject;
        public string AgentID => userId;

        /// <summary>
        /// 외부(UI 등)에서 참조할 현재 점수 프로퍼티입니다.
        /// </summary>
        public float Score => currentScore;

        /// <summary>
        /// 세척(타격) 중일 때 호출됩니다.
        /// </summary>
        public void OnCleanupDetected(CleanupEvent cleanupEvent)
        {
            currentScore += cleanupEvent.Power * 0.1f; 
        }

        /// <summary>
        /// 오염물이 완전히 제거되었을 때 호출됩니다.
        /// </summary>
        public void OnCleanupRemoved(CleanupEvent cleanupEvent)
        {
            float bonus = cleanupEvent.Receiver.Type switch
            {
                ContaminationType.Dust => 10f,
                ContaminationType.StubbornDust => 50f,
                ContaminationType.Stain => 150f,
                _ => 10f
            };

            currentScore += bonus;
            Debug.Log($"<color=cyan>[Score Update]</color> {cleanupEvent.Receiver.Type} 제거 성공! +{bonus}점 획득 (현재 총점: {currentScore:F0})");
        }

        public void ResetScore() => currentScore = 0f;
        public void SetUserID(string newID) => userId = newID;
    }
}
