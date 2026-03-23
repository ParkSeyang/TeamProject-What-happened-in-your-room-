using System.Collections.Generic;

namespace JSH
{
    using UnityEngine;
    using TMPro;
    using Cysharp.Threading.Tasks;
    using System.Text;

    public sealed class ScoreUIController : MonoBehaviour
    {
        public static ScoreUIController Instance { get; private set; }

        [Header("Ranking Display (TMP)")]
        [SerializeField] private TMP_Text rankingBoardText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // 씬이 시작될 때 최신 랭킹을 자동으로 갱신합니다.
            RefreshRankingAsync().Forget();
        }

        /// <summary>
        /// 게임 종료 시 이 메서드를 호출하여 점수를 업로드하고 랭킹을 즉시 갱신합니다.
        /// </summary>
        /// <param name="newScore">게임에서 획득한 최종 점수</param>
        public async UniTask UpdateScoreAndBoardAsync(int newScore)
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsSignedIn)
            {
                Debug.LogWarning("[ScoreUIController] 로그인 정보가 없어 업로드를 건너뜁니다.");
                return;
            }

            string uid = FirebaseAuthManager.Instance.CurrentUser.UserId;
            
            // 1. [가이드 반영] Firestore 업로드 및 로컬 캐싱 통합 메서드 호출
            await FirebaseFirestoreManager.Instance.UploadScoreWithCacheAsync(uid, newScore);
            
            // 2. 업로드 직후 최신 랭킹으로 점수판 갱신
            await RefreshRankingAsync();
        }

        /// <summary>
        /// 서버(Firestore)에서 데이터를 가져와 TMP 텍스트를 갱신합니다.
        /// </summary>
        public async UniTask RefreshRankingAsync()
        {
            if (rankingBoardText == null) return;

            var topScores = await FirebaseFirestoreManager.Instance.GetTopScoresAsync(10);
            if (topScores == null)
            {
                rankingBoardText.text = "Failed to load ranking.";
                return;
            }

            // Zero Alloc 지향: StringBuilder를 사용하여 문자열 할당 최소화
            var sb = new StringBuilder();
            sb.AppendLine("<size=130%><color=#FFCC00>TOP 10 RANKING</color></size>");
            sb.AppendLine("----------------------------");

            int rank = 1;
            foreach (var data in topScores)
            {
                // [가이드 반영] 필드명을 bestScore로 변경하여 조회
                string userName = data.GetValueOrDefault("userName", (object)"Unknown").ToString();
                string score = data.GetValueOrDefault("bestScore", (object)"0").ToString();
                

                // TMP Rich Text: 순위 강조 및 점수 우측 정렬 효과 (<pos> 태그 활용)
                sb.AppendLine($"<color=#FFA500>{rank:D2}.</color> {userName} <pos=80%>{score}</pos>");
                rank++;
            }

            rankingBoardText.text = sb.ToString();
        }
    }
}
