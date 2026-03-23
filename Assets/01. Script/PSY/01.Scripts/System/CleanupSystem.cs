using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 오염물 세척 로직을 중재하는 중앙 시스템입니다. (CombatSystem 계승)
    /// 옵저버 패턴을 활용하여 세척 시 발생하는 이벤트를 외부에서 구독할 수 있도록 합니다.
    /// </summary>
    public class CleanupSystem : SingletonBase<CleanupSystem>
    {
        [SerializeField] private CleanupEffectData effectData;
        private CleanupEventBinder eventBinder;

        public class Events
        {
            public Action<CleanupEvent> OnSomeoneCleaned;
            public Action<CleanupEvent> OnSomeoneRemoved;
            public Action<ICleanAgent, Transform, Transform> OnStartSpraying;
            public Action<ICleanAgent> OnStopSpraying;
        }

        public Events Subscribe { get; private set; } = new Events();

        // [최적화] 콜라이더와 타겟 파트 간의 O(1) 캐싱 딕셔너리
        private Dictionary<Collider, ICleanTargetPart> CleanTargetDic { get; set; }

        protected override void OnInitialize()
        {
            if (CleanTargetDic == null)
            {
                CleanTargetDic = new Dictionary<Collider, ICleanTargetPart>();
            }

            if (eventBinder == null)
            {
                eventBinder = new CleanupEventBinder();
            }

            // [안전 장치] 데이터 유효성 검사
            if (effectData == null)
            {
                Debug.LogError("[CleanupSystem] EffectData가 인스펙터에서 연결되지 않았습니다! 전투 연출이 작동하지 않습니다.");
                return;
            }

            eventBinder.Initialize(effectData);
            eventBinder.Enable();
        }

        protected override void OnDispose()
        {
            eventBinder?.Disable();
        }

        public void StartSpraying(ICleanAgent agent, Transform muzzle, Transform target) => Subscribe.OnStartSpraying?.Invoke(agent, muzzle, target);
        public void StopSpraying(ICleanAgent agent) => Subscribe.OnStopSpraying?.Invoke(agent);

        public void InvokeCleanEvent(CleanupEvent cleanupEvent)
        {
            if (cleanupEvent.Receiver != null)
            {
                cleanupEvent.Receiver.OnClean(cleanupEvent.Power, cleanupEvent.HitPosition);
                Subscribe.OnSomeoneCleaned?.Invoke(cleanupEvent);
                cleanupEvent.Sender?.OnCleanupDetected(cleanupEvent);
            }
        }

        public void InvokeRemoveEvent(CleanupEvent cleanupEvent)
        {
            Subscribe.OnSomeoneRemoved?.Invoke(cleanupEvent);
            cleanupEvent.Sender?.OnCleanupRemoved(cleanupEvent);
        }

        #region ICleanTargetPart Management

        public void AddCleanTarget(Collider col, ICleanTargetPart targetPart)
        {
            if (col == null || targetPart == null) return;
            CleanTargetDic[col] = targetPart;
        }

        public void RemoveCleanTarget(Collider col)
        {
            if (col == null) return;
            CleanTargetDic.Remove(col);
        }

        public bool HasCleanTarget(Collider col) => col != null && CleanTargetDic.ContainsKey(col);

        public ICleanTargetPart GetCleanTarget(Collider col)
        {
            if (col == null) return null;
            return CleanTargetDic.TryGetValue(col, out var target) ? target : null;
        }

        #endregion
    }
}
