using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Placeholder: darkens the camera background as the rocket climbs.
    public class BackgroundManager : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] float spaceAltitude = 1000f;

        // Starts listening for altitude changes.
        void OnEnable()
        {
            GameState.AltitudeChanged += OnAltitudeChanged;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.AltitudeChanged -= OnAltitudeChanged;
        }

        // Blends from sky to space based on how high the rocket is.
        void OnAltitudeChanged(float altitude)
        {
            if (targetCamera == null) return;

            float t = altitude / spaceAltitude;
            targetCamera.backgroundColor = Color.Lerp(Palette.Teal, Palette.Midnight, t);
        }
    }
}
