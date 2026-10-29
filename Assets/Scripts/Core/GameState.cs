using System;

namespace RocketToTheMoon.Core
{
    // Global flight state. The flight simulator writes it, other brains listen to its events.
    public static class GameState
    {
        public static float Altitude { get; private set; }
        public static float MaxAltitude { get; private set; }
        public static bool IsFlying { get; private set; }

        public static event Action FlightStarted;
        public static event Action<float> AltitudeChanged;
        public static event Action FlightEnded;

        // Resets the flight numbers and tells everyone the rocket launched.
        public static void StartFlight()
        {
            Altitude = 0f;
            MaxAltitude = 0f;
            IsFlying = true;
            FlightStarted?.Invoke();
        }

        // Saves the new altitude and tells everyone about it.
        public static void SetAltitude(float altitude)
        {
            Altitude = altitude;
            if (altitude > MaxAltitude) MaxAltitude = altitude;
            AltitudeChanged?.Invoke(altitude);
        }

        // Tells everyone the flight is over.
        public static void EndFlight()
        {
            IsFlying = false;
            FlightEnded?.Invoke();
        }
    }
}
