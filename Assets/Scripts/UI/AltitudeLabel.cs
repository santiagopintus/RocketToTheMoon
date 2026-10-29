using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;

namespace RocketToTheMoon.UI
{
    // Placeholder: shows the live altitude in a text label.
    public class AltitudeLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

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

        // Writes the altitude in metres.
        void OnAltitudeChanged(float altitude)
        {
            label.text = altitude.ToString("0") + " m";
        }
    }
}
