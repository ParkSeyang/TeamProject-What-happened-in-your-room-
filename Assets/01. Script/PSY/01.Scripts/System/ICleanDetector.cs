using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 세척 충돌을 감지하는 주체(예: 물총)의 인터페이스입니다.
    /// </summary>
    public interface ICleanDetector
    {
        ICleanAgent Owner { get; }
        LayerMask DetectionLayer { get; }
        
        void Initialize(ICleanAgent owner);
        void EnableDetection();
        void DisableDetection();
    }
}
