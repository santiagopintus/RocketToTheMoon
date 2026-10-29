using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Turns the max altitude into credits when a flight ends and saves them. The result popup shows them.
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] float creditsPerMetre = 0.1f;

        public int LastCredits { get; private set; }

        // Starts listening for the end of the flight.
        void OnEnable()
        {
            GameState.FlightEnded += OnFlightEnded;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.FlightEnded -= OnFlightEnded;
        }

        // Works out the reward and adds it to the player's credits.
        void OnFlightEnded()
        {
            LastCredits = Mathf.RoundToInt(GameState.MaxAltitude * creditsPerMetre);
            PlayerSession.AddCredits(LastCredits);
        }
    }
}
