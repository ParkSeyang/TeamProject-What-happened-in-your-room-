using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StageSnap : MonoBehaviour, IEndDragHandler
{
    public ScrollRect scrollRect;
    public int stageCount = 3; // 쉬움, 보통, 어려움
    private float[] points;
    private float targetPos;
    private bool isSnapping;

    void Start() {
        // 각 스테이지의 정규화된 위치 설정 (0, 0.5, 1)
        points = new float[stageCount];
        for (int i = 0; i < stageCount; i++) {
            points[i] = i / (float)(stageCount - 1);
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        // 드래그가 끝났을 때 가장 가까운 포인트를 찾음
        float currentPos = scrollRect.horizontalNormalizedPosition;
        float closest = points[0];
        float minDistance = Mathf.Abs(currentPos - points[0]);

        for (int i = 1; i < points.Length; i++) {
            float distance = Mathf.Abs(currentPos - points[i]);
            if (distance < minDistance) {
                minDistance = distance;
                closest = points[i];
            }
        }
        targetPos = closest;
        isSnapping = true;
    }

    void Update() {
        if (isSnapping) {
            // 부드럽게 목표 지점으로 이동
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(scrollRect.horizontalNormalizedPosition, targetPos, Time.deltaTime * 10f);
            if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - targetPos) < 0.001f) {
                scrollRect.horizontalNormalizedPosition = targetPos;
                isSnapping = false;
            }
        }
    }
}
