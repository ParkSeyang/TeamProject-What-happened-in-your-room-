namespace ParkSeyang
{
    /// <summary>
    /// 프로젝트 내의 모든 UI 창을 식별하기 위한 고유 타입입니다.
    /// UIManager의 딕셔너리 키로 사용됩니다.
    /// </summary>
    public enum UIType
    {
        None,
        Title,          // 시작 화면 (Game Name + Start Button)
        Login,          // 로그인 화면
        Lobby,          // 로비 화면 (난이도 선택 등)
        HUD,            // 인게임 HUD (시간, 점수, 물 게이지)
        ARPlaneScan,    // AR 지형 스캔 진행률 UI
        Menu,           // 게임 일시정지/설정 메뉴
        Result,         // 게임 결과 화면 (최종 점수)
        Loading,        // 씬 전환 로딩 화면
        WarningPopup,   // 경고/알림 팝업
        Ranking,        // 리더보드/랭킹 화면
    }
}
