/*
using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : SingletonBase<CombatSystem>
{
    [SerializeField] private CombatEffectData effectData;
    private CombatEventBinder eventBinder;

    public class Events
    {
        // 데미지를 입었을때
        public Action<CombatEvent> OnSomeoneTakeDamage;
        
        // 누군가가 회복했을때
        public Action<CombatEvent> OnSomeoneHeal;

        // 가드에 성공했을때
        public Action<CombatEvent> OnSomeoneGuard;

        // 스킬을 시전했을때
        public Action<CombatEvent> OnSomeoneCastSkill;
    }

    public Events Subscribe { get; private set; } = new Events();

    private const int EVENT_PROCESS_PER_FRAME = 10;
    
    private Dictionary<Collider, IHitTargetPart> HitTargetDic { get; set; }
    private Queue<CombatEvent> CombatEventQueue { get; set; }

    protected override void OnInitialize()
    {
        // 필드 초기화
        if (HitTargetDic == null) HitTargetDic = new Dictionary<Collider, IHitTargetPart>();
        if (CombatEventQueue == null) CombatEventQueue = new Queue<CombatEvent>();

        // Binder 초기화 로직
        if (eventBinder == null)
        {
            eventBinder = new CombatEventBinder();
        }
        
        eventBinder.Initialize(effectData);
        eventBinder.Enable();
    }

    /// <summary>
    /// 외부에서 힐 이벤트를 발생시킵니다.
    /// </summary>
    public void InvokeHealEvent(CombatEvent healEvent)
    {
        Subscribe.OnSomeoneHeal?.Invoke(healEvent);
    }

    protected override void OnDispose()
    {
        eventBinder?.Disable();
    }

    private void Update()
    {
        for (int i = 0; i < EVENT_PROCESS_PER_FRAME; i++)
        {
            if (CombatEventQueue.Count == 0) break;
            var combatEvent = CombatEventQueue.Dequeue();
            HandleCombatEvent(combatEvent);
        }
    }

    public void AddCombatEvent(CombatEvent combatEvent)
    {
        CombatEventQueue.Enqueue(combatEvent);
    }

    private void HandleCombatEvent(CombatEvent combatEvent)
    {
        if (combatEvent.Receiver != null)
        {
            combatEvent.Receiver.TakeDamage(combatEvent.Damage, combatEvent.HitInfo);
            Subscribe.OnSomeoneTakeDamage?.Invoke(combatEvent);
        }
    }

    #region IHitTargetPart Management Methods

    public void AddHitTarget(Collider col, IHitTargetPart hitTarget)
    {
        if (col == null) return;
        HitTargetDic.TryAdd(col, hitTarget);
    }

    public void RemoveHitTarget(Collider col, IHitTargetPart hitTarget)
    {
        if (HitTargetDic.ContainsKey(col) == false) return;
        HitTargetDic.Remove(col);
    }

    public bool HasHitTarget(Collider collider)
    {
        if (collider == null) return false;
        return HitTargetDic.ContainsKey(collider);
    }

    public IHitTargetPart GetHitTarget(Collider collider)
    {
        if (collider == null) return null;
        return HitTargetDic[collider];
    }
    
    #endregion
}
*/
