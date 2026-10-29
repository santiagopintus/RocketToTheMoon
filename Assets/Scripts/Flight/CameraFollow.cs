using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Moves the camera up and down with the rocket, so it stays in view while flying.
    // The faster the rocket goes, the more the camera falls behind, so you can see it speed up.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Transform target;
        [SerializeField] float lagPerSpeed = 0.0008f;
        [SerializeField] float maxLag = 0.15f;

        float lastHeight;
        float lastLag;

        // Remembers where the rocket starts.
        void Start()
        {
            if (target != null) lastHeight = target.position.y;
        }

        // Moves the camera by the height the rocket moved, minus the extra lag from its speed.
        void LateUpdate()
        {
            if (targetCamera == null || target == null) return;

            float lag = Mathf.Min(GameState.Speed * lagPerSpeed, maxLag);
            float moved = (target.position.y - lastHeight) - (lag - lastLag);
            targetCamera.transform.position += Vector3.up * moved;

            lastHeight = target.position.y;
            lastLag = lag;
        }
    }
}
