using UnityEngine;
using System;
using System.Collections.Generic;

namespace ParkSeyang
{
    /// <summary>
    /// 게임의 전반적인 흐름(스캔, 카운트다운, 플레이, 종료)을 관리하는 메인 컨트롤러입니다.
    /// 해당 씬에서만 생존하며, 씬 전환 시 자동으로 파괴 및 재설정됩니다.
    /// </summary>
    public class InGameSystem : MonoBehaviour
    {
        public static InGameSystem Instance { get; private set; }

        #region 상수 및 설정 데이터 (Constants & Settings)
        public const float MAX_SCAN_GAUGE = 100.0f;
        public const float DEFAULT_COUNTDOWN_DURATION = 5.0f;
        public const float DEFAULT_GAME_DURATION = 150.0f; // 150초 
        public const float MAX_WATER_CAPACITY = 100.0f;
        #endregion

        #region 외부 시스템 참조 (External System References)
        [Header("External Systems")]
        [SerializeField] private ARScanProvider arScanProvider;
        [SerializeField] private WaterTankSystem waterTankSystem;
        [SerializeField] private ARObjectSpawner arObjectSpawner;

        [Header("Player References")]
        [Tooltip("플레이어의 WaterGun 컴포넌트를 연결하세요.")]
        [SerializeField] private WaterGun playerWaterGun;
        
        [Tooltip("플레이어의 몸체 콜라이더를 연결하세요.")]
        [SerializeField] private Collider playerCollider;

        public ARScanProvider ScanProvider => arScanProvider;
        public WaterTankSystem TankSystem => waterTankSystem;
        public ARObjectSpawner ObjectSpawner => arObjectSpawner;
        public WaterGun PlayerWaterGun => playerWaterGun;
        public Collider PlayerCollider => playerCollider;
        #endregion

        #region 공유 상태 데이터 (Shared State Data)
        [field: SerializeField] public float ScanProgress { get; set; } = 0f;
        
        public bool IsControlLocked { get; private set; } = true;

        private bool isRefilling = false;
        public bool IsRefilling
        {
            get => isRefilling;
            set
            {
                if (isRefilling != value)
                {
                    isRefilling = value;
                    OnRefillStateChanged?.Invoke(isRefilling);
                }
            }
        }

        public bool IsNearWaterTank { get; set; } = false; 

        // [추가] 충전 상태 변경 이벤트
        public Action<bool> OnRefillStateChanged;
        #endregion

        #region 상태 머신 필드 (State Machine Fields)
        private Dictionary<Type, InGameStateBase> stateDictionary;
        private InGameStateBase currentState;

        public InGameStateBase CurrentState => currentState;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeStateDictionary();
                ChangeState<InitializeState>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update() => currentState?.UpdateState();
        #endregion

        #region 상태 제어 로직 (State Control)
        private void InitializeStateDictionary()
        {
            stateDictionary = new Dictionary<Type, InGameStateBase>
            {
                { typeof(InitializeState), new InitializeState() },
                { typeof(CountdownState), new CountdownState() },
                { typeof(PlayState), new PlayState() },
                { typeof(ResultState), new ResultState() }
            };

            var parameter = new InGameStateBase.InGameParameter
            {
                system = this
            };

            foreach (var state in stateDictionary.Values)
            {
                state.Initialize(parameter);
            }
        }

        public void ChangeState<T>() where T : InGameStateBase
        {
            Type nextType = typeof(T);

            if (stateDictionary.ContainsKey(nextType) == false)
            {
                Debug.LogWarning($"[InGameSystem] {nextType.Name} 상태가 등록되지 않았습니다.");
                return;
            }

            currentState?.ExitState();
            currentState = stateDictionary[nextType];
            currentState.EnterState();

            Debug.Log($"[InGameSystem] State Transition: {nextType.Name}");
        }
        #endregion

        #region 공개 유틸리티 메서드 (Public Utilities)
        public void SetControlLock(bool isLocked) => IsControlLocked = isLocked;

        /// <summary>
        /// [재시작용] 플레이어의 모든 물리적, 논리적 상태를 초기화합니다.
        /// </summary>
        public void ResetPlayerState()
        {
            IsRefilling = false;
            IsNearWaterTank = false;

            if (playerWaterGun != null)
            {
                playerWaterGun.ResetWaterGun();
            }
            
            Debug.Log("[InGameSystem] Player logic states have been reset.");
        }
        #endregion
    }
}
