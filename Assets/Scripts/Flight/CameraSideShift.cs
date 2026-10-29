using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Slides the camera sideways so the rocket sits in the middle of the left two thirds of the screen.
    // Used while the store covers the right third.
    public class CameraSideShift : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Transform target;
        [SerializeField] float slideSpeed = 6f;

        bool shifted;
        float offset;

        // Turns the shift on or off. The camera slides there smoothly.
        public void SetShifted(bool value)
        {
            shifted = value;
        }

        // Moves the camera a bit closer to its goal. It only adds the change, so other camera scripts still work.
        void LateUpdate()
        {
            if (targetCamera == null || target == null) return;

            // Moving the camera right by 1/6 of the view puts the rocket at 1/3 of the screen.
            float goal = shifted ? ViewWidthAtTarget() / 6f : 0f;
            float newOffset = Mathf.Lerp(offset, goal, slideSpeed * Time.deltaTime);
            targetCamera.transform.position += targetCamera.transform.right * (newOffset - offset);
            offset = newOffset;
        }

        // How wide the view is, in metres, at the rocket's distance from the camera.
        float ViewWidthAtTarget()
        {
            float distance = Vector3.Dot(target.position - targetCamera.transform.position, targetCamera.transform.forward);
            float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return height * targetCamera.aspect;
        }
    }
}
