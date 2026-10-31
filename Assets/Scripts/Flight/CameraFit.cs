using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RocketToTheMoon.Core
{
    // While the rocket is on the pad, slides the camera so the whole rocket fits on screen, centred.
    // The scroll wheel zooms in and out on top of that.
    public class CameraFit : MonoBehaviour
    {
        [SerializeField]
        Camera targetCamera;

        [SerializeField]
        RocketBuilder builder;

        [SerializeField]
        float margin = 1.3f;

        [SerializeField]
        float minHeight = 0.9f;

        [SerializeField]
        float zoomStep = 0.1f;

        [SerializeField]
        float minZoom = 0.4f;

        [SerializeField]
        float maxZoom = 2.5f;

        [SerializeField]
        float moveSpeed = 5f;

        Vector3 padPosition;
        Vector3 fitPosition;
        float zoom = 1f;
        bool started;

        // Remembers where the rocket sits on the pad and where the camera starts.
        void Start()
        {
            if (targetCamera == null || builder == null)
                return;
            padPosition = builder.transform.position;
            fitPosition = targetCamera.transform.position;
        }

        // Moves the camera a bit closer to its goal. It only adds the change, so other camera scripts still work.
        // Does nothing while the rocket is away from the pad (flying or falling).
        void LateUpdate()
        {
            if (targetCamera == null || builder == null)
                return;
            if (GameState.IsFlying || builder.transform.position != padPosition)
                return;

            ReadScroll();

            // The first frame jumps straight to the goal, so the game does not start with the camera moving.
            float t = started ? moveSpeed * Time.deltaTime : 1f;
            Vector3 newPosition = Vector3.Lerp(fitPosition, Goal(), t);
            targetCamera.transform.position += newPosition - fitPosition;
            fitPosition = newPosition;
            started = true;
        }

        // Zooms in when scrolling up and out when scrolling down, unless the cursor is over the UI (like the store list).
        void ReadScroll()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll == 0f)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            zoom *= scroll > 0f ? 1f - zoomStep : 1f + zoomStep;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        // Where the camera should be: looking at the middle of the rocket, far enough back to see all of it.
        Vector3 Goal()
        {
            Bounds rocket = builder.PlacedBounds();
            float height = Mathf.Max(rocket.size.y * margin, minHeight) * zoom;
            float distance =
                height * 0.5f / Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return rocket.center - targetCamera.transform.forward * distance;
        }
    }
}
