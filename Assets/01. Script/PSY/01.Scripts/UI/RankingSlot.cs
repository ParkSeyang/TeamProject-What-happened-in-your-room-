using UnityEngine;
using TMPro;

namespace ParkSeyang
{
    /// <summary>
    /// 리더보드에서 개별 유저의 랭킹 정보를 표시하는 슬롯 클래스입니다.
    /// </summary>
    public sealed class RankingSlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text rankText;      // 순위 표시
        [SerializeField] private TMP_Text userNameText;  // 닉네임 표시
        [SerializeField] private TMP_Text scoreText;     // 점수 표시

        [Header("Visual Options")]
        [SerializeField] private Color highlightColor = Color.green; // 내 점수 표시용 (기본: 초록색)
        [SerializeField] private Color defaultColor = Color.black;   // 타인 점수 표시용 (기본: 검정색)

        /// <summary>
        /// 슬롯에 랭킹 데이터를 주입합니다.
        /// </summary>
        public void SetData(int rank, UserData data, bool isMe)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            
            if (rankText != null) rankText.text = rank.ToString();
            if (userNameText != null) userNameText.text = data.userName;
            if (scoreText != null) scoreText.text = data.bestScore.ToString("N0");

            // 인스펙터에서 설정한 색상을 적용합니다.
            Color targetColor = isMe ? highlightColor : defaultColor;
            
            if (userNameText != null) userNameText.color = targetColor;
            if (scoreText != null) scoreText.color = targetColor;
            if (rankText != null) rankText.color = targetColor; 
        }

        public void Clear() => gameObject.SetActive(false);
    }
}
