using UnityEngine;

namespace ParkSeyang
{
    /// <summary>
    /// 물탱크 프리팹에 부착되어, 범위 내 플레이어의 물총(WaterGun)을 충전합니다.
    /// InGameSystem에 등록된 플레이어 정보를 참조합니다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class WaterTank : MonoBehaviour
    {
        [Header("충전 설정")]
        [SerializeField] private float chargeSpeed = 20f;

        [Header("사운드 설정")]
        [SerializeField] private AudioClip refillSound;

        private AudioSource audioSource;
        private bool isPlayerInRange = false;
        private bool hasInstanceErrorLogged = false; // 에러 로그 중복 출력 방지용

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.clip = refillSound;
                audioSource.loop = true; // 충전 중 계속 들려야 하므로 루프 설정
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            if (InGameSystem.Instance == null)
            {
                // [Loud Fail] 인스턴스가 없으면 개발자에게 즉시 알림
                if (hasInstanceErrorLogged == false)
                {
                    Debug.LogError("[WaterTank] InGameSystem.Instance가 존재하지 않습니다! 씬에 InGameSystem 프리팹이 배치되어 있는지 확인하세요.");
                    hasInstanceErrorLogged = true;
                }
                return;
            }

            // 플레이어가 범위 내에 있고, 시스템에서 충전 버튼을 눌렀을 때(IsRefilling)만 충전을 수행합니다.
            bool isActuallyRefilling = (isPlayerInRange == true && InGameSystem.Instance.IsRefilling == true);

            if (isActuallyRefilling == true)
            {
                // 사운드가 재생 중이 아니면 재생 시작
                if (audioSource != null && audioSource.isPlaying == false)
                {
                    audioSource.Play();
                }

                PerformCharging();
            }
            else
            {
                // 충전 조건이 아닐 때 사운드가 재생 중이면 정지
                if (audioSource != null && audioSource.isPlaying == true)
                {
                    audioSource.Stop();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (InGameSystem.Instance == null || InGameSystem.Instance.PlayerCollider == null) return;

            if (other == InGameSystem.Instance.PlayerCollider)
            {
                isPlayerInRange = true;
                InGameSystem.Instance.IsNearWaterTank = true;
                Debug.Log("[WaterTank] 충전 범위에 진입했습니다. 버튼을 눌러 충전을 시작하세요.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (InGameSystem.Instance == null || InGameSystem.Instance.PlayerCollider == null) return;

            if (other == InGameSystem.Instance.PlayerCollider)
            {
                isPlayerInRange = false;
                InGameSystem.Instance.IsNearWaterTank = false;
                InGameSystem.Instance.IsRefilling = false; // 범위를 벗어나면 충전 상태 강제 해제
                Debug.Log("[WaterTank] 충전 범위를 벗어났습니다.");
            }
        }

        private void PerformCharging()
        {
            if (InGameSystem.Instance != null && InGameSystem.Instance.PlayerWaterGun != null)
            {
                InGameSystem.Instance.PlayerWaterGun.FillWater(chargeSpeed * Time.deltaTime);
            }
        }
    }
}
