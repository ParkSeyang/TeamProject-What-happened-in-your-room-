using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 세척 이벤트 데이터 구조체입니다.
    /// </summary>
    public struct CleanupEvent
    {
        public ICleanAgent Sender;  // 발신자 (플레이어 등)
        public ICleanable Receiver; // 수신자 (오염물)
        public float Power;         // 세척력 (데미지)
        public Vector3 HitPosition; // 타격 위치
    }

    /// <summary>
    /// 세척 가능한 오브젝트가 구현해야 할 인터페이스입니다.
    /// </summary>
    public interface ICleanable
    {
        void OnClean(float power, Vector3 position);
        ContaminationType Type { get; }
        GameObject GameObject { get; }
    }
}
