using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // The speed and Skip buttons, shown only during a flight so retries are faster.
    // The speed button goes 1x -> 2x -> 4x by changing Time.timeScale. It goes back to 1x when the flight ends.
    public class FlightControls : MonoBehaviour
    {
        [SerializeField]
        GameObject panel;

        [SerializeField]
        Button speedButton;

        [SerializeField]
        TMP_Text speedLabel;

        [SerializeField]
        Button skipButton;

        [SerializeField]
        FlightSimulator simulator;

        [SerializeField]
        float[] speeds = { 1f, 2f, 4f };

        int speedIndex;

        // Hides the buttons and connects them.
        void Awake()
        {
            panel.SetActive(false);
            speedButton.onClick.AddListener(NextSpeed);
            skipButton.onClick.AddListener(simulator.Skip);
        }

        // Starts listening for launches and flight ends.
        void OnEnable()
        {
            GameState.FlightStarted += Show;
            GameState.FlightEnded += Hide;
        }

        // Stops listening and puts time back to normal, so leaving the scene mid-flight does not keep it fast.
        void OnDisable()
        {
            GameState.FlightStarted -= Show;
            GameState.FlightEnded -= Hide;
            Time.timeScale = 1f;
        }

        // Shows the buttons at normal speed.
        void Show()
        {
            SetSpeed(0);
            panel.SetActive(true);
        }

        // Hides the buttons and goes back to normal speed.
        void Hide()
        {
            SetSpeed(0);
            panel.SetActive(false);
        }

        // Goes to the next speed, back to the first after the last.
        void NextSpeed()
        {
            SetSpeed((speedIndex + 1) % speeds.Length);
        }

        // Changes how fast the game runs and writes it on the button.
        void SetSpeed(int index)
        {
            speedIndex = index;
            Time.timeScale = speeds[index];
            speedLabel.text = speeds[index] + "x";
        }
    }
}
