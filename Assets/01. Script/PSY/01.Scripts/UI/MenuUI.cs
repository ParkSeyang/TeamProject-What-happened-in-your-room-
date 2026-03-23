using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 게임 일시정지 및 설정 메뉴를 관리하는 UI입니다.
    /// </summary>
    public class MenuUI : BaseUI
    {
        public override UIType UIType => UIType.Menu;
        public override bool IsPopup => true;

        public override void Refresh()
        {
            // 메뉴 데이터 초기화 등
        }
    }
}
