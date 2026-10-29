using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;

namespace RocketToTheMoon.UI
{
    // Shows the rocket's mass, thrust and fuel, and a hint about whether it can take off. Updates when the rocket changes.
    public class RocketStatsPanel : MonoBehaviour
    {
        [SerializeField] RocketBuilder builder;
        [SerializeField] FlightSimulator simulator;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text hintLabel;

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

        // Reads the rocket and writes its numbers and the take-off hint.
        void Refresh()
        {
            RocketStats stats = simulator.ReadStats();
            float fullMass = stats.FullMass(simulator.MassPerFuel);
            float ratio = stats.ThrustToWeight(fullMass, simulator.Gravity);

            statsLabel.text =
                "Mass   " + Mathf.RoundToInt(fullMass) + " kg\n" +
                "Thrust " + Mathf.RoundToInt(stats.Thrust) + " N\n" +
                "Fuel   " + Mathf.RoundToInt(stats.Fuel) + "\n" +
                "T/W    " + ratio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            bool ready = stats.Fuel > 0f && ratio >= 1.5f;
            hintLabel.text = Hint(stats, fullMass, ratio);
            hintLabel.color = ready ? Palette.Moonlight : Palette.LightTeal;
        }

        // One line of advice. Thrust/weight above 1 means it goes up; it also goes up later if burning fuel makes it light enough.
        string Hint(RocketStats stats, float fullMass, float ratio)
        {
            if (stats.PartCount == 0) return "Drag a body onto the pad to start.";
            if (stats.Thrust <= 0f) return "No thrust. Add a thruster.";
            if (stats.Fuel <= 0f) return "No fuel. Add a fuel tank.";

            float emptyRatio = stats.ThrustToWeight(stats.DryMass, simulator.Gravity);
            if (emptyRatio < 1f)
            {
                int missing = Mathf.CeilToInt(fullMass * simulator.Gravity - stats.Thrust);
                return "Too heavy to lift off.\nAdd " + missing + " N of thrust or remove weight.";
            }
            if (ratio < 1f) return "Slow start: lifts off only\nafter burning some fuel.";
            if (ratio < 1.5f) return "Lifts off, but slowly.\nMore thrust helps.";
            return "Ready for lift-off!";
        }
    }
}
