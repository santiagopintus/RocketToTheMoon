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
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text altitudeLabel;
        [SerializeField] TMP_Text creditsLabel;
        [SerializeField] Button continueButton;
        [SerializeField] ScoreManager score;
        [SerializeField] FlightSimulator simulator;

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
            panel.SetActive(true);
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
