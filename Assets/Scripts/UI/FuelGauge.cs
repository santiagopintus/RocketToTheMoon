using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // Shows how much fuel is left, as a percentage and an optional filled bar.
    public class FuelGauge : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Image fillBar;

        // Starts listening for fuel changes and shows the current amount.
        void OnEnable()
        {
            GameState.FuelChanged += OnFuelChanged;
            OnFuelChanged(GameState.FuelLeft);
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.FuelChanged -= OnFuelChanged;
        }

        // Writes the fuel left and fills the bar to match.
        void OnFuelChanged(float fuelLeft)
        {
            label.text = "Fuel " + Mathf.RoundToInt(fuelLeft * 100f) + "%";
            if (fillBar != null) fillBar.fillAmount = fuelLeft;
        }
    }
}
