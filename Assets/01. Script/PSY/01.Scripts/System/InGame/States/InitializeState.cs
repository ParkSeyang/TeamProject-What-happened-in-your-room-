using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 1단계: 초기화 및 환경 스캔 단계
    /// AR 스캔 진행률을 실시간으로 확인하며 스캔 완료 시 다음 단계로 전이합니다.
    /// </summary>
    public class InitializeState : InGameStateBase
    {
        public override void EnterState()
        {
            // 1. 시스템 제어 잠금 및 이전 데이터 완전 청소
            context.SetControlLock(true);
            
            // [추가] 재시작 시 플레이어의 물 상태 및 발사 여부를 강제 리셋합니다.
            context.ResetPlayerState();

            // [추가] 재시작 시 기존에 생성된 적(먼지)들과 스폰 코루틴을 강제 종료합니다.
            var spawner = Object.FindAnyObjectByType<ARObjectSpawner>();
            if (spawner != null)
            {
                spawner.StopSpawningAndClear();
            }

            // 2. 게임 데이터 허브 초기화 (점수, 시간, 물, 스캔율 모두 0)
            if (GameStatusController.Instance != null)
            {
                GameStatusController.Instance.ResetStatus(
                    InGameSystem.DEFAULT_GAME_DURATION, 
                    InGameSystem.MAX_WATER_CAPACITY
                );
            }

            // 3. AR 스캔 데이터 리셋 및 활성화
            if (context.ScanProvider != null)
            {
                context.ScanProvider.ClearAllPlanes(); // 기존 평면 제거 및 세션 리셋
                context.ScanProvider.SetScanActive(true);
                context.ScanProvider.OnScanProgressUpdated += OnScanProgressUpdated;
            }
            
            Debug.Log("[InitializeState] Game Data Reset, Spawner Cleared & AR Scan Restarted.");
        }

        public override void UpdateState()
        {
            // [기획 반영] 스캔 조건이 모두 충족되면 카운트다운으로 전환
            if (context.ScanProvider != null && context.ScanProvider.IsScanRequirementMet() == true)
            {
                context.ChangeState<CountdownState>();
            }
        }

        public override void ExitState()
        {
            // 스캔이 완료되었으므로 이벤트 구독 해제 및 스캔 기능 비활성화
            if (context.ScanProvider != null)
            {
                context.ScanProvider.OnScanProgressUpdated -= OnScanProgressUpdated;
                context.ScanProvider.SetScanActive(false);
            }
        }

        /// <summary>
        /// ARScanProvider로부터 받은 바닥 스캔 진행률을 전역 데이터에 동기화합니다.
        /// </summary>
        private void OnScanProgressUpdated(float horizontalProgress)
        {
            // [수정] 이제 중앙 데이터 허브(GameStatusController)를 통해 진행률을 관리합니다.
            if (GameStatusController.Instance != null)
            {
                GameStatusController.Instance.UpdateScanProgress(horizontalProgress * 100.0f);
            }
        }
    }
}
