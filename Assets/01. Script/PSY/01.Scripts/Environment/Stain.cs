using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 끈적거리는 오염물인 때입니다.
    /// </summary>
    public class Stain : DustBase
    {
        public override ContaminationType Type => contaminationType;
    }
}
