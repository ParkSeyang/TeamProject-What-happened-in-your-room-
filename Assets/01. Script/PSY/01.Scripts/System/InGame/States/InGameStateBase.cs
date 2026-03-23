namespace ParkSeyang
{
    /// <summary>
    /// 게임 상태의 기반이 되는 추상 클래스입니다.
    /// Mushroom FSM 샘플의 설계 철학을 100% 계승하여 구조체 기반 초기화를 수행합니다.
    /// </summary>
    public abstract class InGameStateBase
    {
        /// <summary>
        /// 상태 제어 및 데이터 참조에 필요한 파라미터 묶음입니다.
        /// </summary>
        public struct InGameParameter
        {
            public InGameSystem system;
            // 향후 필요에 따라 UIManager, ARManager 등을 여기에 추가하여 확장합니다.
        }

        protected InGameSystem context;

        /// <summary>
        /// 생성자 대신 호출되어 상태에 필요한 의존성을 주입합니다.
        /// </summary>
        public virtual void Initialize(InGameParameter parameter) => context = parameter.system;

        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void ExitState();
    }
}
