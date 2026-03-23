/*
using UnityEngine;
using System.Collections.Generic;

// [주의] 이 파일은 3D RPG 프로젝트의 샘플 코드로, 현재 프로젝트의 인터페이스 및 시스템과 충돌을 방지하기 위해 전체 주석 처리되었습니다.
// 현재 프로젝트의 판정 로직은 CleanupHurtBox 및 WaterGun의 Raycast 로직을 참고해 주세요.

public class HitBox : MonoBehaviour, IHitDetector
{
    [field: SerializeField] public LayerMask DetectionLayer { get; private set; }
    public ICombatAgent Owner { get; private set; }
    
    private Collider hitBoxCollider;
    private HashSet<IHitTargetPart> hitList = new HashSet<IHitTargetPart>();

    private void Awake()
    {
        hitBoxCollider = GetComponent<Collider>();
        if (hitBoxCollider != null)
        {
            hitBoxCollider.isTrigger = true; 
            hitBoxCollider.enabled = false;
        }
    }

    public void Initialize(ICombatAgent owner)
    {
        Owner = owner;
    }

    public void Initialize(ICombatAgent owner, LayerMask detectionLayer)
    {
        Owner = owner;
        DetectionLayer = detectionLayer;
    }

    public void EnableDetection()
    {
        if (hitBoxCollider != null) hitBoxCollider.enabled = true;
        hitList.Clear();
    }

    public void DisableDetection()
    {
        if (hitBoxCollider != null) hitBoxCollider.enabled = false;
        hitList.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
    }
}
*/
