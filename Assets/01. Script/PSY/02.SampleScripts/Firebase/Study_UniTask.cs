using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UniTask = Cysharp.Threading.Tasks.UniTask;

namespace Study
{
    public class Study_UniTask : MonoBehaviour
    {
        // UniTask를 사용하는 방법은 대부분 Task => UniTask로 바꿔버리면 됩니다.
        // 내부동작이 달라질 뿐이라서 그렇습니다. 기반은 C# Task 기반입니다
        // 대신에 +@로 유니티 관련한 기능들이 추가되어 있습니다.
        
        // UniTask는 (.Net의 비동기 처리) + (유니티와 연관된 추가 기능)
        // PS : Task를 상속받아서 유니티의 특징을 추가했다!로 이해하시면 좋습니다
        // GC가 없다. 제로 얼로케이션이다. 라는것을 스터디하시면 됩니다.
        
        private const string IMAGE_URL = "https://picsum.photos/500";
        public RawImage rawImage;
    
        public async void DownloadImage()
        {
            Debug.Log($"다운로드를 시작합니다 {DateTime.Now:HH:mm:ss}");
        
            //await Task.Delay(2000); //오래걸리라고 추가해놓음
            await UniTask.Delay(2000);

            try
            {
                Texture2D texture = await GetTextureAsync(IMAGE_URL);
                rawImage.texture = texture;
                Debug.Log($"이미지를 적용했습니다. {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception e) // 
            {
                Debug.LogError(e);
            }
        }

        private async UniTask<Texture2D> GetTextureAsync(string url)
        {
            using (UnityWebRequest rq = UnityWebRequestTexture.GetTexture(url))
            {
                //await rq.SendWebRequest(); // UniTask를 사용하지 않는 일반 방법
                await rq.SendWebRequest().ToUniTask(); // UniTask를 사용하는 방법

                if (rq.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception(rq.error);
                }

                return DownloadHandlerTexture.GetContent(rq);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DownloadImage();
            }
        }

        private void Start()
        {
            MouseClickLoop();
        }

        private async void MouseClickLoop()
        {
            CancellationToken token = this.destroyCancellationToken;

            // try-catch가 호출비용이 조금 있는편
            try
            {
                while (token.IsCancellationRequested == false)// 토큰이 살아있으면
                {
                    Func<bool> onMouseClick = () => Input.GetMouseButtonDown(0);
                    await UniTask.WaitUntil(onMouseClick, cancellationToken: token);
                    // OnClick?.Invoke({A}) 이런식으로 이벤트를 발생시켜도 됩니다.
                    Debug.Log("Click!!");
                    await UniTask.Yield(cancellationToken: token);
                    
                    //UniTask.Yield(PlayerLoopTiming.Update) => Update Frame만큼 (다음 Update)
                    //UniTask.Yield(PlayerLoopTiming.FixedUpdate) => 물리 처리 Frame만큼 대기 (다음 FixedUpdate)
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
        
    }
    
    
}