using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Makes the camera tremble with a random strength while the rocket is flying.
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] float minStrength = 0.001f;
        [SerializeField] float maxStrength = 0.004f;

        Vector3 lastOffset;

        // Swaps last frame's shake for a new one while flying, and removes it when the flight ends.
        // It only adds an offset, so other scripts (like CameraFollow) can still move the camera.
        void Update()
        {
            if (targetCamera == null) return;

            Vector3 offset = Vector3.zero;
            if (GameState.IsFlying)
                offset = Random.insideUnitSphere * Random.Range(minStrength, maxStrength);

            targetCamera.transform.localPosition += offset - lastOffset;
            lastOffset = offset;
        }
    }
}
