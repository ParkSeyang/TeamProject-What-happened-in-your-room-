using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace ParkSeyang
{
    /// <summary>
    /// 글로벌 랭킹(리더보드)을 시각적으로 보여주는 UI 클래스입니다.
    /// 스스로 데이터를 로드하고 슬롯을 갱신하며, 데이터 부재 시 피드백을 제공합니다.
    /// </summary>
    public sealed class RankingUI : BaseUI
    {
        public override UIType UIType => UIType.Ranking;
        public override bool IsPopup => true;

        [Header("Ranking List (Prefabs/Container)")]
        [SerializeField] private GridLayoutGroup rankingGrid;
        [SerializeField] private List<RankingSlot> rankingSlots = new List<RankingSlot>();
        [SerializeField] private Button closeButton;

        [Header("Status Feedback")]
        [SerializeField] private TMP_Text loadingText;

        private bool isDataLoading = false;

        protected override void Awake()
        {
            base.Awake();
            // [설계 개선] RankingUI는 이제 자체적으로 Close를 호출하지 않고 LobbyUI가 통합 관리합니다.
            // 에디터와 빌드 간의 버튼 이벤트 실행 순서 차이로 인한 버그를 방지합니다.
        }

        public override void Open()
        {
            base.Open();
            RefreshRankingList().Forget();
        }

        // [복구/보완] 랭킹 창이 닫힐 때 로직입니다.
        public override void Close()
        {
            base.Close();
            // [설계 개선] RankingUI는 이제 LobbyUI의 상태를 직접 제어하지 않습니다. 
            // UI 간의 결합도를 낮추고 각자 본연의 기능에 집중합니다.
        }

        private async UniTask RefreshRankingList()
        {
            if (isDataLoading == true)
            {
                Debug.Log("[RankingUI] 이미 랭킹 데이터를 로딩 중입니다. 중복 호출을 무시합니다.");
                return;
            }
            
            isDataLoading = true;

            try
            {
                rankingSlots.Clear();
                if (rankingGrid != null)
                {
                    rankingSlots.AddRange(rankingGrid.GetComponentsInChildren<RankingSlot>(true));
                }

                foreach (var slot in rankingSlots) if (slot != null) slot.Clear();

                if (loadingText != null)
                {
                    loadingText.text = "Loading Leaderboard...";
                    loadingText.gameObject.SetActive(true);
                }

                var rankings = await RankingManager.Instance.GetTopRankings(Mathf.Max(15, rankingSlots.Count));
                string myUid = FirebaseFirestoreManager.Instance.currentData?.userUID ?? string.Empty;

                if (rankings == null || rankings.Count == 0)
                {
                    if (loadingText != null)
                    {
                        loadingText.text = "No Rankings Data Found.";
                        loadingText.gameObject.SetActive(true);
                    }
                    return;
                }

                if (loadingText != null) loadingText.gameObject.SetActive(false);

                int displayCount = 0;
                for (int i = 0; i < rankings.Count; i++)
                {
                    if (i >= rankingSlots.Count) break;
                    if (rankingSlots[i] == null || rankings[i] == null) continue;

                    bool isMe = (rankings[i].userUID == myUid);
                    rankingSlots[i].SetData(i + 1, rankings[i], isMe);
                    displayCount++;
                }

                Debug.Log($"[RankingUI] Leaderboard updated: {displayCount} users displayed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RankingUI] Error: {ex.Message}");
                if (loadingText != null) loadingText.text = "Failed to load rankings.";
            }
            finally
            {
                isDataLoading = false;
            }
        }

        public override void Refresh()
        {
            RefreshRankingList().Forget();
        }
    }
}
