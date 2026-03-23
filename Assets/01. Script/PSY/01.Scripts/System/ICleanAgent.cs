using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 세척 시스템의 상호작용 주체(플레이어, 로봇 등)가 구현할 인터페이스입니다.
    /// Firebase 연동 및 랭킹 시스템을 위해 사용자의 식별 정보와 세척 성과 감지 로직을 포함합니다.
    /// </summary>
    public interface ICleanAgent
    {
        // 에이전트의 실제 게임 오브젝트
        GameObject GameObject { get; }

        // Firebase 연동 시 사용할 사용자의 고유 ID 또는 닉네임
        string AgentID { get; }

        /// <summary>
        /// 세척이 성공적으로 감지되었을 때 호출됩니다. (점수 계산용)
        /// </summary>
        /// <param name="cleanupEvent">발생한 세척 이벤트 정보</param>
        void OnCleanupDetected(CleanupEvent cleanupEvent);

        /// <summary>
        /// 오염물이 완전히 제거되었을 때 호출됩니다. (추가 보너스 점수 등)
        /// </summary>
        /// <param name="cleanupEvent">발생한 제거 이벤트 정보</param>
        void OnCleanupRemoved(CleanupEvent cleanupEvent);
    }
}
