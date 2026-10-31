using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // Shows the max altitude and the credits earned when the rocket lands.
    // Continue hides it and puts the rocket back on the pad.
    public class ResultPopup : MonoBehaviour
    {
        [SerializeField]
        GameObject panel;

        [SerializeField]
        TMP_Text altitudeLabel;

        [SerializeField]
        TMP_Text creditsLabel;

        [SerializeField]
        TMP_Text bestLabel;

        [SerializeField]
        TMP_Text unlockLabel;

        [SerializeField]
        Button continueButton;

        [SerializeField]
        ScoreManager score;

        [SerializeField]
        ShopPanel shop;

        [SerializeField]
        FlightSimulator simulator;

        // Hides the popup and connects the Continue button.
        void Awake()
        {
            panel.SetActive(false);
            continueButton.onClick.AddListener(Continue);
        }

        // Starts listening for landings and new launches.
        void OnEnable()
        {
            GameState.Landed += Show;
            GameState.FlightStarted += Hide;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.Landed -= Show;
            GameState.FlightStarted -= Hide;
        }

        // Fills in the numbers and shows the popup.
        void Show()
        {
            altitudeLabel.text = GameState.MaxAltitude.ToString("0") + " m";
            creditsLabel.text = "+" + score.LastCredits + " credits";
            bestLabel.text = BestText();
            bestLabel.color = score.IsNewBest ? Palette.Moonlight : Palette.LightTeal;
            unlockLabel.text = UnlockText();
            panel.SetActive(true);
        }

        // The part this flight unlocked, or the next part to unlock and the altitude it needs.
        // The next part above the old record was just unlocked if the new record reached it.
        string UnlockText()
        {
            PartData next = shop.NextUnlock(score.PreviousBest);
            if (next == null)
                return "All parts unlocked";
            if (next.unlockAltitude <= PlayerSession.BestAltitude)
                return "NEW PART: " + next.partName + "!";
            return "Next: " + next.partName + " at " + next.unlockAltitude.ToString("0") + " m";
        }

        // "NEW BEST!" with the old record, or the record still to beat.
        string BestText()
        {
            string previous = score.PreviousBest.ToString("0") + " m";
            if (!score.IsNewBest)
                return "Best: " + previous;
            return score.PreviousBest > 0f ? "NEW BEST!  (was " + previous + ")" : "NEW BEST!";
        }

        // Hides the popup.
        void Hide()
        {
            panel.SetActive(false);
        }

        // Hides the popup and puts the rocket back on the pad.
        void Continue()
        {
            Hide();
            simulator.ResetToPad();
        }
    }
}
