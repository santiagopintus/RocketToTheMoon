using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // Shows the rocket's mass, thrust and fuel, and a hint about whether it can take off. Updates when the rocket changes.
    public class RocketStatsPanel : MonoBehaviour
    {
        [SerializeField] RocketBuilder builder;
        [SerializeField] FlightSimulator simulator;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text hintLabel;
        [SerializeField] Button launchButton;

        // Starts listening for rocket changes and shows the current numbers.
        void OnEnable()
        {
            builder.Changed += Refresh;
            Refresh();
        }

        // Stops listening.
        void OnDisable()
        {
            builder.Changed -= Refresh;
        }

        // Reads the rocket and writes its numbers and the take-off hint. Masses are shown in grams.
        void Refresh()
        {
            RocketStats stats = simulator.ReadStats();
            float ratio = stats.ThrustToWeight(stats.FullMass, simulator.Gravity);
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            statsLabel.text =
                PartsLine(stats.PartCount) + "\n" +
                "Mass   " + Mathf.RoundToInt(stats.FullMass * 1000f) + " g\n" +
                "Thrust " + stats.Thrust.ToString("0.#", culture) + " N\n" +
                "Fuel   " + Mathf.RoundToInt(stats.Fuel * 1000f) + " g (" + stats.BurnTime.ToString("0.0", culture) + " s)\n" +
                "T/W    " + ratio.ToString("0.00", culture);

            bool ready = stats.Fuel > 0f && ratio >= 1.5f;
            hintLabel.text = Hint(stats, ratio);
            hintLabel.color = ready ? Palette.Moonlight : Palette.LightTeal;

            // The hint above already says what is missing.
            bool canLaunch = stats.PartCount > 0 && stats.Thrust > 0f && stats.Fuel > 0f;
            if (launchButton != null) launchButton.interactable = canLaunch;
        }

        // How many parts are used out of the maximum. Turns cream with "FULL" when no more fit.
        string PartsLine(int count)
        {
            string line = "Parts  " + count + " / " + builder.MaxParts;
            if (count < builder.MaxParts) return line;
            return "<color=#" + ColorUtility.ToHtmlStringRGB(Palette.Moonlight) + ">" + line + "  FULL</color>";
        }

        // One line of advice. Thrust/weight above 1 means it goes up; it also goes up later if burning fuel makes it light enough.
        string Hint(RocketStats stats, float ratio)
        {
            if (stats.PartCount == 0) return "Drag a body onto the pad to start.";
            if (stats.Thrust <= 0f) return "No thrust. Add a thruster.";
            if (stats.Fuel <= 0f) return "No fuel. Add a fuel tank.";

            float emptyRatio = stats.ThrustToWeight(stats.DryMass, simulator.Gravity);
            if (emptyRatio < 1f)
            {
                float missing = stats.FullMass * simulator.Gravity - stats.Thrust;
                return "Too heavy to lift off.\nAdd " + missing.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " N of thrust or remove weight.";
            }
            if (ratio < 1f) return "Slow start: lifts off only\nafter burning some fuel.";
            if (ratio < 1.5f) return "Lifts off, but slowly.\nMore thrust helps.";
            return "Ready for lift-off!";
        }
    }
}
