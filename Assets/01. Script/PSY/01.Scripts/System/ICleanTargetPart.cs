using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 세척(피격) 가능한 오브젝트의 구성 파트 인터페이스입니다.
    /// 실제 충돌체(Collider)와 데이터 주인(Owner)을 연결하는 다리 역할을 합니다.
    /// </summary>
    public interface ICleanTargetPart
    {
        /// <summary>
        /// 이 파트를 포함하고 있는 데이터 본체(먼지 등)입니다.
        /// </summary>
        ICleanable Owner { get; }

        /// <summary>
        /// 실제 물리 판정에 사용되는 콜라이더입니다.
        /// </summary>
        Collider Collider { get; }

        /// <summary>
        /// 파트가 속한 게임 오브젝트입니다.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// 파트의 위치 및 회전 정보에 접근하기 위한 트랜스폼입니다.
        /// </summary>
        Transform transform { get; }
        
        /// <summary>
        /// 주인을 설정하고 초기화합니다.
        /// </summary>
        void Initialize(ICleanable owner);
    }
}
