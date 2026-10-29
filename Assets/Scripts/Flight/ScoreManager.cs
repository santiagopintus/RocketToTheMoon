using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Placeholder: turns the max altitude into credits when a flight ends.
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] float creditsPerMetre = 0.1f;

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

        // Works out the reward. Saving the credits comes later.
        void OnFlightEnded()
        {
            int credits = Mathf.RoundToInt(GameState.MaxAltitude * creditsPerMetre);
            Debug.Log("Flight over. Max altitude " + GameState.MaxAltitude.ToString("0") + " m, earned " + credits + " credits");
        }
    }
}
