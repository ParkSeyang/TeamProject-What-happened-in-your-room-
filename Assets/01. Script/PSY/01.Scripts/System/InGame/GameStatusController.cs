using System;
using UnityEngine;

namespace ParkSeyang
{
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// 게임의 실시간 데이터(물 잔량, 점수, 시간)를 관리하는 중앙 정보 허브입니다.
    /// 데이터가 변경될 때마다 등록된 옵저버(UI 등)에게 이벤트를 전파합니다.
    /// </summary>
    public class GameStatusController : SingletonBase<GameStatusController>
    {
        #region 실시간 데이터 (Model)
        private float waterGauge = 100f;
        private int currentScore = 0;
        private float gameTimer = 150.0f; // 150초
        private float scanProgress = 0f;
        
        public float WaterGauge => waterGauge;
        public int CurrentScore => currentScore;
        public float GameTimer => gameTimer;
        public float ScanProgress => scanProgress;

        // [추가] 로비에서 선택한 난이도를 전역적으로 관리합니다.
        public DifficultyLevel SelectedDifficulty { get; set; } = DifficultyLevel.Easy;
        #endregion

        #region 데이터 변경 이벤트 (Observer)
        public Action<float> OnWaterChanged;
        public Action<int> OnScoreChanged;
        public Action<float> OnTimerChanged;
        public Action<float> OnScanProgressChanged;
        #endregion

        #region 데이터 제어 메서드 (Controller Actions)
        
        /// <summary>
        /// 스캔 진행률을 업데이트하고 변경 사항을 알립니다 (0~100).
        /// </summary>
        public void UpdateScanProgress(float value)
        {
            scanProgress = Mathf.Clamp(value, 0f, 100f);
            OnScanProgressChanged?.Invoke(scanProgress);
        }

        /// <summary>
        /// 물 잔량을 업데이트하고 변경 사항을 알립니다.
        /// </summary>
        public void UpdateWater(float value)
        {
            waterGauge = Mathf.Clamp(value, 0f, 100f);
            OnWaterChanged?.Invoke(waterGauge);
        }

        /// <summary>
        /// 점수를 가산하고 변경 사항을 알립니다.
        /// </summary>
        public void AddScore(int amount)
        {
            currentScore += amount;
            OnScoreChanged?.Invoke(currentScore);
        }

        /// <summary>
        /// 게임 타이머를 업데이트하고 변경 사항을 알립니다.
        /// </summary>
        public void UpdateTimer(float time)
        {
            gameTimer = Mathf.Max(0f, time);
            OnTimerChanged?.Invoke(gameTimer);
        }

        /// <summary>
        /// 새로운 게임 시작 시 또는 로비 복귀 시 모든 데이터를 초기화합니다.
        /// </summary>
        public void ResetStatus(float initialTime = 150f, float initialWater = 100f)
        {
            currentScore = 0;
            gameTimer = initialTime;
            waterGauge = initialWater;
            scanProgress = 0f;
            
            OnScoreChanged?.Invoke(currentScore);
            OnTimerChanged?.Invoke(gameTimer);
            OnWaterChanged?.Invoke(waterGauge);
            OnScanProgressChanged?.Invoke(scanProgress);
            
            Debug.Log("[GameStatusController] Game data has been fully reset.");
        }
        #endregion
    }
}
