using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// 유저의 몬스터 인벤토리를 보여주고 페이징 기능을 제공하는 최적화된 로비 뷰
/// </summary>
public class LobbyView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int itemsPerPage = 10;
    [SerializeField] private SignView signView;

    private int currentPage = 0;

    private void OnGUI()
    {
        // 1. 화면 비율에 맞춘 창 크기 설정 (반응형 대응)
        float w = Mathf.Min(Screen.width * 0.8f, 800f);
        float h = Mathf.Min(Screen.height * 0.9f, 950f);
        Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);

        GUI.Box(rect, ""); 
        
        // 내부 영역 (여백을 조금 더 타이트하게 조정)
        GUILayout.BeginArea(new Rect(rect.x + 20, rect.y + 20, w - 40, h - 40));
        GUILayout.BeginVertical();

        // 2. 유저 정보 헤더 (상단 고정)
        DrawUserHeader();

        GUILayout.Space(15);
        GUILayout.Label("<b><size=28>Monster Collection</size></b>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
        GUILayout.Space(20);

        if (FirebaseManager.Instance == null)
        {
            GUILayout.Label("Firebase Manager not found.");
        }
        else
        {
            // 3. 인벤토리 리스트 영역
            DrawInventoryList();
        }

        // --- 유연한 공간 확보 ---
        GUILayout.FlexibleSpace();

        // 4. 하단 게임 시작 버튼 (항상 노출되도록 보정)
        GUI.backgroundColor = Color.green;
        GUIStyle startBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("START AR CATCH", startBtnStyle, GUILayout.Height(80)))
        {
            Debug.Log("[LobbyView] Starting AR Session...");
            SceneManager.LoadScene(1);

        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawUserHeader()
    {
        string email = FirebaseManager.Instance != null ? FirebaseManager.Instance.UserEmail : "Unknown User";
        
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUIStyle userStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        GUILayout.Label("👤 LOGGED IN:", userStyle, GUILayout.Width(120));
        
        GUIStyle emailStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.cyan } };
        GUILayout.Label(email, emailStyle);
        
        GUILayout.FlexibleSpace();
        
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("LOGOUT", GUILayout.Width(100), GUILayout.Height(35)))
        {
            HandleLogout();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void HandleLogout()
    {
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.SignOut();
        if (signView != null)
        {
            signView.gameObject.SetActive(true);
            this.gameObject.SetActive(false);
        }
    }

    private void DrawInventoryList()
    {
        List<string> inventory = FirebaseManager.Instance.MonsterInventory;
        int totalCount = inventory.Count;

        if (totalCount == 0)
        {
            GUIStyle emptyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            GUILayout.Label("\n\nYour inventory is empty.\nGo catch some monsters in AR!", emptyStyle, GUILayout.Height(300));
        }
        else
        {
            int maxPage = (totalCount - 1) / itemsPerPage;
            currentPage = Mathf.Clamp(currentPage, 0, maxPage);

            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, totalCount);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<size=14>Items {startIndex + 1} - {endIndex}</size>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<size=14>Total: {totalCount}</size>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);

            // 리스트 본문 높이 최적화 (45px로 조정)
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(20, 0, 8, 8) };
            for (int i = startIndex; i < endIndex; i++)
            {
                GUILayout.Label($"   [{i + 1:D2}]   Monster ID :  <b>{inventory[i]}</b>", boxStyle, GUILayout.Height(45));
            }

            // 공간 유지용 빈 영역 축소
            int displayedCount = endIndex - startIndex;
            if (displayedCount < itemsPerPage)
            {
                GUILayout.Space((itemsPerPage - displayedCount) * 49); // (45 + spacing)
            }

            GUILayout.Space(15);

            // 페이지 이동 컨트롤 높이 최적화
            GUILayout.BeginHorizontal();
            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("◀ PREV", GUILayout.Height(50))) currentPage--;

            GUI.enabled = (currentPage + 1) * itemsPerPage < totalCount;
            if (GUILayout.Button("NEXT ▶", GUILayout.Height(50))) currentPage++;
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }
}
