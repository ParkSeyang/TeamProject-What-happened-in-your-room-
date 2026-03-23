using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 2단계: 카운트다운 단계
    /// 스캔 완료 후 게임 시작 전까지 5초간 대기합니다.
    /// </summary>
    public class CountdownState : InGameStateBase
    {
        private float timer;
        private const float DURATION = 5.0f; // 매개변수 대신 상수로 관리하거나 context에서 가져옵니다.

        public override void EnterState()
        {
            timer = DURATION;
            
            // [추가] 카운트다운 시작 시 HUD UI 활성화
            if (UIManager.IsInitialized == true)
            {
                UIManager.Instance.SetUIActive(UIType.HUD, true);
            }
        }

        public override void UpdateState()
        {
            timer -= Time.deltaTime;
            
            // TODO: UIManager를 통해 카운트다운 숫자를 UI에 실시간 표시 로직
            
            if (timer <= 0)
            {
                context.ChangeState<PlayState>();
            }
        }

        public override void ExitState() { }
    }
}
