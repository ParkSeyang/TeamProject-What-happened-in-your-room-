using UnityEngine;

public interface IPoolAbleObject
{
    // 이 객체가 속한 풀의 ID (EnemyID와 일치)
    int EnemyID { get; }

    // 풀에서 꺼내질 때 (재사용 시작)
    void OnGet();
    
    // 풀로 반환될 때 (비활성화 시작)
    void OnRelease();
}
