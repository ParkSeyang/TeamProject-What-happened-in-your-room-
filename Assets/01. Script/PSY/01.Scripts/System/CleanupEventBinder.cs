using System;
using System.Collections.Generic;
using UnityEngine;
using PixPlays.ElementalVFX;

namespace ParkSeyang
{
    /// <summary>
    /// 세척 시스템의 시각적, 청각적 연출 데이터를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class CleanupEffectData
    {
        [Header("Beam Settings")]
        public BeamVfx waterBeamPrefab;         // 물줄기 프리팹 (PixPlays)

        [Header("Removal Effects (Pending)")]
        public GameObject dustRemoveEffect;     // 일반 먼지 제거 이펙트
        public GameObject stubbornRemoveEffect; // 묵은 먼지 제거 이펙트
        public GameObject stainRemoveEffect;    // 때 제거 이펙트

        [Header("Audio Clips (Pending)")]
        public AudioClip washSound;             // 세척 중 소리 (Loop 추천)
        public AudioClip cleanFinishSound;      // 완전히 제거되었을 때 소리
    }

    /// <summary>
    /// CleanupSystem의 이벤트를 구독하여 실제 연출(이펙트, 사운드)을 수행합니다.
    /// 현재는 물줄기(Beam) 발사 기능만 활성화되어 있으며, 사운드와 제거 이펙트는 주석 처리되었습니다.
    /// </summary>
    public class CleanupEventBinder
    {
        private CleanupEffectData effectData;
        
        // 에이전트별 빔 인스턴스 관리
        private Dictionary<ICleanAgent, BeamVfx> beamInstances = new Dictionary<ICleanAgent, BeamVfx>();
        
        // [임시 주석] 사운드 관리를 위한 오디오 소스
        private AudioSource loopAudioSource;

        public void Initialize(CleanupEffectData data)
        {
            effectData = data;

            
            if (CleanupSystem.Instance != null && loopAudioSource == null)
            {
                loopAudioSource = CleanupSystem.Instance.gameObject.AddComponent<AudioSource>();
                loopAudioSource.loop = true;
                loopAudioSource.playOnAwake = false;
                if (effectData != null) loopAudioSource.clip = effectData.washSound;
            }
            
        }

        public void Enable()
        {
            if (CleanupSystem.Instance != null)
            {
                var events = CleanupSystem.Instance.Subscribe;
                events.OnSomeoneCleaned += OnSomeoneCleaned;
                events.OnSomeoneRemoved += OnSomeoneRemoved;
                events.OnStartSpraying += OnStartSpraying;
                events.OnStopSpraying += OnStopSpraying;
            }
        }

        public void Disable()
        {
            if (CleanupSystem.IsInitialized == false) return;

            var system = CleanupSystem.Instance;
            if (system != null && system.Subscribe != null)
            {
                var events = system.Subscribe;
                events.OnSomeoneCleaned -= OnSomeoneCleaned;
                events.OnSomeoneRemoved -= OnSomeoneRemoved;
                events.OnStartSpraying -= OnStartSpraying;
                events.OnStopSpraying -= OnStopSpraying;
            }
        }

        private void OnStartSpraying(ICleanAgent agent, Transform muzzle, Transform target)
        {
            // [보안] 에이전트가 null이면 로직을 수행하지 않습니다.
            if (agent == null || muzzle == null || target == null) return;
            if (effectData == null || effectData.waterBeamPrefab == null) return;

            if (beamInstances.ContainsKey(agent))
            {
                OnStopSpraying(agent);
            }

            // 빔 생성 및 초기화
            BeamVfx beam = UnityEngine.Object.Instantiate(effectData.waterBeamPrefab, muzzle);
            beam.transform.localPosition = Vector3.zero;
            beam.transform.localRotation = Quaternion.identity;
            
            // [핵심!] 부모(물총)의 스케일 영향을 상쇄하고, 모바일 AR 최적화를 위해 0.5배 크기로 설정합니다.
            // 부모가 작아도 물줄기 모델은 월드 기준 0.5:0.5:0.5 비율을 유지하게 됩니다.
            Vector3 parentScale = muzzle.lossyScale;
            const float targetBeamScale = 0.5f;
            beam.transform.localScale = new Vector3(targetBeamScale / parentScale.x, targetBeamScale / parentScale.y, targetBeamScale / parentScale.z);
            
            // [개선] Source를 muzzle(Transform)로 명확히 전달하고, 타겟도 명확히 전달합니다.
            // PixPlays 시스템의 속도(9999f)를 활용해 즉시 발사되도록 설정합니다.
            VfxData data = new VfxData(muzzle, target, 9999f, 0.5f);
            beam.Play(data);
            
            beamInstances.Add(agent, beam);

            // [추가] ZeroDarkMos님이 요청하신 'BeamBody/BeamBody'만 별도로 정밀 조절합니다.
            // 아래의 innerThickness와 innerLength에 원하는 "월드 실제 크기"를 입력하시면 됩니다.
            Transform innerBeamBody = beam.transform.Find("BeamBody/BeamBody");
            if (innerBeamBody != null)
            {
                const float innerThickness = 0.1f; // [수정 가능] 월드에서의 실제 두께
                const float innerLength = 0.3f;    // [수정 가능] 월드에서의 실제 길이

                // 루트 빔이 0.5배(targetBeamScale)이므로, 입력한 값이 그대로 나오도록 보정하여 대입합니다.
                float compensation = 1.0f / targetBeamScale; 
                innerBeamBody.localScale = new Vector3(innerThickness * compensation, innerThickness * compensation, innerLength * compensation);
            }

            // [디테일 수정] Hit 이펙트 비주얼은 줄이고, 물리 판정(HitBox) 범위는 유지합니다.
            Transform hitEffect = beam.transform.Find("Hit");
            if (hitEffect != null)
            {
                // 1. 비주얼을 담당하는 Hit 부모 객체를 빔 대비 40% 크기로 축소
                const float hitVisualScale = 0.4f; 
                hitEffect.localScale = new Vector3(hitVisualScale, hitVisualScale, hitVisualScale);

                // 2. 자식인 HitBox를 찾아 역으로 스케일을 키워 물리 판정 범위는 1.0(빔 기준)으로 보존
                Transform hitBoxTransform = hitEffect.Find("HitBox");
                if (hitBoxTransform != null)
                {
                    float inverseScale = 1.0f / hitVisualScale;
                    hitBoxTransform.localScale = new Vector3(inverseScale, inverseScale, inverseScale);
                }
            }

            // [핵심 추가] 빔 프리팹에 붙은 CleanupHitBox 초기화
            if (beam.TryGetComponent<CleanupHitBox>(out var hitBox))
            {
                // WaterGun 컴포넌트를 찾아 기본 설정값(Power 등)을 가져와 초기화
                var waterGun = (agent as MonoBehaviour)?.GetComponentInChildren<WaterGun>();
                if (waterGun != null)
                {
                    hitBox.Initialize(agent, LayerMask.GetMask("Enemy"), 1.0f); // Power는 기본 1.0
                    hitBox.SetActive(true);
                }
            }

           
            if (loopAudioSource != null && loopAudioSource.isPlaying == false)
            {
                loopAudioSource.Play();
            }
            
        }

        private void OnStopSpraying(ICleanAgent agent)
        {
            // [보안] 에이전트가 null이면 딕셔너리 조회를 시도하지 않습니다.
            if (agent == null) return;

            if (beamInstances.TryGetValue(agent, out BeamVfx beam))
            {
                beam.Stop();
                UnityEngine.Object.Destroy(beam.gameObject, 0.5f);
                beamInstances.Remove(agent);
            }

            
            if (beamInstances.Count == 0 && loopAudioSource != null)
            {
                loopAudioSource.Stop();
            }
            
        }

        private void OnSomeoneCleaned(CleanupEvent cleanupEvent)
        {
            // BeamVfx 내부에서 자체 히트 이펙트를 처리함
        }

        private void OnSomeoneRemoved(CleanupEvent cleanupEvent)
        {
            if (effectData == null) return;

            /* [임시 주석] 제거 이펙트 및 사운드 연출 비활성화
            GameObject prefab = GetRemoveEffect(cleanupEvent.Receiver.Type);
            
            if (prefab != null)
            {
                GameObject removeFx = UnityEngine.Object.Instantiate(prefab, cleanupEvent.HitPosition, Quaternion.identity);
                UnityEngine.Object.Destroy(removeFx, 2.0f);
            }

            if (effectData.cleanFinishSound != null)
            {
                AudioSource.PlayClipAtPoint(effectData.cleanFinishSound, cleanupEvent.HitPosition);
            }
            */
        }

        private GameObject GetRemoveEffect(ContaminationType type)
        {
            return type switch
            {
                ContaminationType.Dust => effectData.dustRemoveEffect,
                ContaminationType.StubbornDust => effectData.stubbornRemoveEffect,
                ContaminationType.Stain => effectData.stainRemoveEffect,
                _ => effectData.dustRemoveEffect
            };
        }
    }
}
