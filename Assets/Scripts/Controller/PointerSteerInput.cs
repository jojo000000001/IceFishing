using IceFishing.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IceFishing.Controller
{
    /// <summary>
    /// 把触屏/鼠标换成钩子目标世界 X。点在 UI 上时不转向。
    /// </summary>
    public static class PointerSteerInput
    {
        public static bool TryGetWorldX(Camera camera, out float worldX)
        {
            worldX = 0f;
            if (camera == null)
            {
                return false;
            }

            Vector3 screen;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    return false;
                }

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return false;
                }

                screen = touch.position;
            }
            else if (Input.GetMouseButton(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return false;
                }

                screen = Input.mousePosition;
            }
            else
            {
                return false;
            }

            var z = Mathf.Abs(camera.transform.position.z);
            worldX = camera.ScreenToWorldPoint(new Vector3(screen.x, Screen.height * 0.5f, z)).x;
            return true;
        }
    }
}
