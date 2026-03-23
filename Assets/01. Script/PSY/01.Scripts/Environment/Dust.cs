using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 가장 기본적인 오염물인 일반 먼지입니다.
    /// </summary>
    public class Dust : DustBase
    {
        public override ContaminationType Type => contaminationType;
    }
}
