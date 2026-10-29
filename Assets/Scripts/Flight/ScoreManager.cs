using TMPro;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Turns the max altitude into credits when a flight ends, saves them and shows the result.
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] float creditsPerMetre = 0.1f;
        [SerializeField] TMP_Text resultLabel;

        // Starts listening for the start and end of the flight.
        void OnEnable()
        {
            GameState.FlightStarted += OnFlightStarted;
            GameState.FlightEnded += OnFlightEnded;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.FlightStarted -= OnFlightStarted;
            GameState.FlightEnded -= OnFlightEnded;
        }

        // Hides the last result when a new flight starts.
        void OnFlightStarted()
        {
            if (resultLabel != null) resultLabel.text = "";
        }

        // Works out the reward, adds it to the player's credits and shows the message.
        void OnFlightEnded()
        {
            int credits = Mathf.RoundToInt(GameState.MaxAltitude * creditsPerMetre);
            PlayerSession.AddCredits(credits);

            string message = "Flight over!\nMax altitude " + GameState.MaxAltitude.ToString("0") + " m\nEarned " + credits + " credits";
            Debug.Log(message);
            if (resultLabel != null) resultLabel.text = message;
        }
    }
}
