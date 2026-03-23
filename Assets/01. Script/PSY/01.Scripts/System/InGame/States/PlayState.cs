using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 3단계: 메인 게임 루프
    /// GameStatusController를 통해 시간과 자원을 관리하며 오브젝트를 스폰합니다.
    /// </summary>
    public class PlayState : InGameStateBase
    {
        public override void EnterState()
        {
            context.SetControlLock(false);
            
            // [정보 허브 연동] 게임 상태 초기화 (2분 30초, 물 100%)
            if (GameStatusController.IsInitialized == true)
            {
                GameStatusController.Instance.ResetStatus(InGameSystem.DEFAULT_GAME_DURATION, InGameSystem.MAX_WATER_CAPACITY);
            }

            // [기획 반영] 게임 시작 시 물탱크 스폰 연동
            if (context.TankSystem != null)
            {
                context.TankSystem.SpawnWaterTank();
            }

            // [기획 반영] 게임 시작 시 오염물 오브젝트 랜덤 스폰 연동
            if (context.ObjectSpawner != null)
            {
                context.ObjectSpawner.SpawnContaminations();
            }
        }

        public override void UpdateState()
        {
            if (context.IsRefilling == true) return;

            // [정보 허브 연동] 타이머 업데이트
            if (GameStatusController.IsInitialized == true)
            {
                float newTime = GameStatusController.Instance.GameTimer - Time.deltaTime;
                GameStatusController.Instance.UpdateTimer(newTime);

                if (newTime <= 0)
                {
                    context.ChangeState<ResultState>();
                }
            }
        }

        public override void ExitState()
        {
            context.SetControlLock(true);
            
            if (context.TankSystem != null)
            {
                context.TankSystem.DestroyWaterTank();
            }

            if (context.ObjectSpawner != null)
            {
                context.ObjectSpawner.ClearAllSpawnedObjects();
            }
        }
    }
}
