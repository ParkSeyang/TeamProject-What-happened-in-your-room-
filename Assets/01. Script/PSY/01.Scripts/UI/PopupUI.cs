using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 일반적인 경고/알림 팝업을 관리하는 기본 클래스입니다.
    /// </summary>
    public class PopupUI : BaseUI
    {
        public override UIType UIType => UIType.WarningPopup;
        public override bool IsPopup => true;

        public override void Refresh()
        {
        }
    }
}
