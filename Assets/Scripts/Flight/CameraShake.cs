using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Makes the camera tremble with a random strength while the rocket is flying, and hard for a moment when it crashes.
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] float minStrength = 0.001f;
        [SerializeField] float maxStrength = 0.004f;
        [SerializeField] float crashStrength = 0.05f;
        [SerializeField] float crashTime = 0.5f;

        Vector3 lastOffset;
        float crashLeft;

        // Starts listening for crashes.
        void OnEnable()
        {
            GameState.Crashed += OnCrashed;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.Crashed -= OnCrashed;
        }

        // Starts the big shake.
        void OnCrashed(Vector3 position)
        {
            crashLeft = crashTime;
        }

        // Swaps last frame's shake for a new one while flying or crashing, and removes it after.
        // It only adds an offset, so other scripts (like CameraFollow) can still move the camera.
        void Update()
        {
            if (targetCamera == null) return;

            Vector3 offset = Vector3.zero;
            if (crashLeft > 0f)
            {
                offset = Random.insideUnitSphere * crashStrength * crashLeft / crashTime;
                crashLeft -= Time.deltaTime;
            }
            else if (GameState.IsFlying)
                offset = Random.insideUnitSphere * Random.Range(minStrength, maxStrength);

            targetCamera.transform.localPosition += offset - lastOffset;
            lastOffset = offset;
        }
    }
}
