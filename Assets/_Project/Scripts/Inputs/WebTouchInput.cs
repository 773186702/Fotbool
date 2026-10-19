using UnityEngine;
using UnityEngine.EventSystems;

namespace Fotbool.Inputs
{
    /// <summary>
    /// نظام اللمس للمتصفح والموبايل - يدعم النقر والسحب
    /// </summary>
    public sealed class WebTouchInput : MonoBehaviour
    {
        public static WebTouchInput Instance { get; private set; }

        [Header("الإعدادات")]
        public float swipeThreshold = 50f;
        public float tapMaxTime = 0.3f;

        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool isTouching = false;

        public System.Action<Vector2> OnSwipe;
        public System.Action<Vector2> OnTap;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                ProcessTouch(touch.position, touch.phase);
            }
            else if (Input.GetMouseButton(0))
            {
                ProcessTouch(Input.mousePosition, Input.GetMouseButtonDown(0) ? TouchPhase.Began : 
                    Input.GetMouseButtonUp(0) ? TouchPhase.Ended : TouchPhase.Moved);
            }
        }

        private void ProcessTouch(Vector2 position, TouchPhase phase)
        {
            // تجاهل النقر على عناصر واجهة المستخدم
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            switch (phase)
            {
                case TouchPhase.Began:
                    touchStartPos = position;
                    touchStartTime = Time.time;
                    isTouching = true;
                    break;

                case TouchPhase.Ended:
                    if (isTouching)
                    {
                        float time = Time.time - touchStartTime;
                        float distance = Vector2.Distance(position, touchStartPos);

                        if (time < tapMaxTime && distance < swipeThreshold)
                        {
                            OnTap?.Invoke(position);
                        }
                        else if (distance >= swipeThreshold)
                        {
                            Vector2 direction = position - touchStartPos;
                            OnSwipe?.Invoke(direction);
                        }
                    }
                    isTouching = false;
                    break;
            }
        }
    }
}
