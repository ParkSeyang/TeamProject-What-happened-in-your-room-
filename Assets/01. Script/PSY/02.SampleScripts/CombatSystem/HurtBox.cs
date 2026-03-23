/*
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HurtBox : MonoBehaviour, IHitTargetPart
{
    public ICombatAgent Owner { get; private set; }
    
    [SerializeField] private Collider hurtCollider;
    public Collider Collider => hurtCollider;

    private void Awake()
    {
        if (hurtCollider == null)
        {
            hurtCollider = GetComponent<Collider>();
        }
    }

    public void Initialize(ICombatAgent owner)
    {
        Owner = owner;
        
        // 1. 만약 인스펙터 참조가 유실되었다면(변수명 변경 등) 스스로 다시 찾음
        if (hurtCollider == null)
        {
            hurtCollider = GetComponent<Collider>();
        }

        // 2. 그럼에도 없다면 에러 출력, 있다면 시스템에 등록
        if (CombatSystem.Instance != null && hurtCollider != null)
        {
            CombatSystem.Instance.AddHitTarget(hurtCollider, this);
        }
    }

    private void OnDestroy()
    {
        // 씬 전환 시 CombatSystem이 먼저 파괴되었을 수 있으므로 정밀 체크
        if (CombatSystem.IsInitialized == true && CombatSystem.Instance != null && Collider != null)
        {
            CombatSystem.Instance.RemoveHitTarget(Collider, this);
        }
    }
}
*/
