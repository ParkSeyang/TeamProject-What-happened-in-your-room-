using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 잘 지워지지 않는 묵은 먼지입니다.
    /// </summary>
    public class StubbornDust : DustBase
    {
        public override ContaminationType Type => contaminationType;
    }
}
