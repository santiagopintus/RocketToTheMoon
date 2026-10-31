using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Pays credits for every metre above the old record when a flight ends. No new record, no credits.
    // The result popup shows them.
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField]
        float creditsPerMetre = 6f;

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

        // Saves the best altitude, pays for the metres gained over the old record and adds them to the player's credits.
        // The best altitude is saved first, so the store sees new parts when the credits change.
        void OnFlightEnded()
        {
            PreviousBest = PlayerSession.BestAltitude;
            IsNewBest = GameState.MaxAltitude > PreviousBest;
            PlayerSession.ReportAltitude(GameState.MaxAltitude);
            LastCredits = IsNewBest
                ? Mathf.RoundToInt((GameState.MaxAltitude - PreviousBest) * creditsPerMetre)
                : 0;
            PlayerSession.AddCredits(LastCredits);
        }
    }
}
