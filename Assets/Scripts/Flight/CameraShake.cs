using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Makes the camera tremble with a random strength while the rocket is flying.
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] float minStrength = 0.001f;
        [SerializeField] float maxStrength = 0.004f;

        Vector3 startPosition;

        // Remembers where the camera starts.
        void Start()
        {
            if (targetCamera != null) startPosition = targetCamera.transform.localPosition;
        }

        // Moves the camera a little each frame while flying, and puts it back when the flight ends.
        void Update()
        {
            if (targetCamera == null) return;

            Vector3 offset = Vector3.zero;
            if (GameState.IsFlying)
                offset = Random.insideUnitSphere * Random.Range(minStrength, maxStrength);

            targetCamera.transform.localPosition = startPosition + offset;
        }
    }
}
