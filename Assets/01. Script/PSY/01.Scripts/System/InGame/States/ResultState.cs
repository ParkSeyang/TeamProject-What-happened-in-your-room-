using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ParkSeyang
{
    /// <summary>
    /// 4단계: 종료 및 결과 단계
    /// 제한 시간 종료 후 점수 집계, 데이터 저장 및 결과 UI 출력을 담당합니다.
    /// </summary>
    public class ResultState : InGameStateBase
    {
        public override void EnterState()
        {
            context.SetControlLock(true);
            
            // 1. [수정] 즉시 저장 대신 결과 UI만 표시합니다.
            if (UIManager.IsInitialized == true)
            {
                UIManager.Instance.SetUIActive(UIType.HUD, false);
                UIManager.Instance.SetUIActive(UIType.Result, true);
            }

            // 2. 로그 출력
            if (GameStatusController.IsInitialized == true)
            {
                Debug.Log($"<color=cyan>[Result]</color> Game Over. Final Score: {GameStatusController.Instance.CurrentScore}");
            }
        }

        /// <summary>
        /// 이번 판의 결과를 유저 데이터(최고 기록)에 저장하고 랭킹 시스템을 동기화합니다.
        /// </summary>
        public async UniTask SaveResultAsync()
        {
            if (GameStatusController.IsInitialized == false) return;
            int finalScore = GameStatusController.Instance.CurrentScore;

            try
            {
                Debug.Log($"[ResultState] === Firebase Ranking Update Start (Score: {finalScore}) ===");
                
                // 1. [데이터 저장] 최고 점수 경신 시 Firebase Firestore에 자동 업로드
                await FirebaseFirestoreManager.Instance.UpdateBestScore(finalScore);

                // 2. 서버 반영을 위한 안정화 대기
                await UniTask.Delay(300, delayType: DelayType.Realtime);

                Debug.Log("[ResultState] Firebase Ranking Update Completed Successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ResultState] 점수 갱신 중 치명적 오류: {e.Message}");
            }
        }

        public override void UpdateState() { }

        public override void ExitState() { }
    }
}
