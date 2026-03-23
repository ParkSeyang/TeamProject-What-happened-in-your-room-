using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PocketAR.AR
{
    public class BallPhysicsController : MonoBehaviour
    {
        [SerializeField] private float throwForce = 35f;
        
        private Rigidbody rb;
        private Vector3 initialPosition;
        private bool isThrown = false;
        private Vector2 touchStartPosition;
        private float touchStartTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.localPosition;
        }

        private void Update()
        {
            /* Update에서는 Swipe에 대한 판정처리만 합니다.
             * Swipe의 값을 구해서 ThrowBall 매개변수로 전달합니다.
             */
            if (isThrown || Touchscreen.current == null) return;

            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)  // 터치의 시작
            {
                touchStartPosition = touch.position.ReadValue();
                touchStartTime = Time.time;
            }
            else if (touch.press.wasReleasedThisFrame)  // 터치의 종료
            {
                if (Time.time - touchStartTime < 0.5f)
                {
                    Vector2 swipe = touch.position.ReadValue() - touchStartPosition;
                    if (swipe.magnitude > 100f) ThrowBall(swipe);
                }
            }
        }

        private void ThrowBall(Vector2 swipe)
        {
            isThrown = true;
            rb.isKinematic = false;

            // Swipe 세기에 따른 힘 계산 (거리 기반 가중치)
            float swipeMagnitude = swipe.magnitude;
            float forceMultiplier = Mathf.Clamp(swipeMagnitude / 500f, 0.5f, 2.0f); // 500px 기준 0.5~2배
            float finalThrowForce = throwForce * forceMultiplier;

            float horizontalRatio = (swipe.x / Screen.width) * 2.0f; 
            
            Transform camTransform = Camera.main.transform;
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;

            Vector3 throwDirection = (camForward + (camRight * horizontalRatio)).normalized;

            // 최종 힘 적용
            Vector3 force = (throwDirection * finalThrowForce) + (Vector3.up * (finalThrowForce * 0.25f));
            rb.AddForce(force, ForceMode.Impulse);

            ResetAfterTimeout().Forget();
        }

        private async UniTaskVoid ResetAfterTimeout()
        {
            await UniTask.Delay(5000);
            if (isThrown) ResetBall();
        }

        public void ResetBall()
        {
            isThrown = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.localPosition = initialPosition;
        }
    }
}