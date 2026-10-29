using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Turns the max altitude into credits when a flight ends and saves them. The result popup shows them.
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] float creditsPerMetre = 0.1f;

        public int LastCredits { get; private set; }

        // The best altitude before the last flight, and whether the last flight beat it.
        public float PreviousBest { get; private set; }
        public bool IsNewBest { get; private set; }

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

        // Saves the best altitude, works out the reward and adds it to the player's credits.
        // The best altitude is saved first, so the store sees new parts when the credits change.
        void OnFlightEnded()
        {
            PreviousBest = PlayerSession.BestAltitude;
            IsNewBest = GameState.MaxAltitude > PreviousBest;
            PlayerSession.ReportAltitude(GameState.MaxAltitude);
            LastCredits = Mathf.RoundToInt(GameState.MaxAltitude * creditsPerMetre);
            PlayerSession.AddCredits(LastCredits);
        }
    }
}
