using System;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Global flight state. The flight simulator writes it, other brains listen to its events.
    public static class GameState
    {
        public static float Altitude { get; private set; }
        public static float MaxAltitude { get; private set; }
        public static bool IsFlying { get; private set; }
        public static float Speed { get; private set; }
        public static float FuelLeft { get; private set; } = 1f;

        public static event Action FlightStarted;
        public static event Action<float> AltitudeChanged;
        public static event Action<float> FuelChanged;
        public static event Action FlightEnded;
        public static event Action Landed;
        public static event Action<Vector3> Crashed;

        // Resets the flight numbers and tells everyone the rocket launched.
        public static void StartFlight()
        {
            Altitude = 0f;
            MaxAltitude = 0f;
            Speed = 0f;
            IsFlying = true;
            FlightStarted?.Invoke();
        }

        // Saves the current speed in metres per second.
        public static void SetSpeed(float speed)
        {
            Speed = speed;
        }

        // Saves how much fuel is left (0 = empty, 1 = full) and tells everyone about it.
        public static void SetFuel(float fuelLeft)
        {
            FuelLeft = fuelLeft;
            FuelChanged?.Invoke(fuelLeft);
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

        // Tells everyone the rocket is back on the ground after falling.
        public static void Land()
        {
            Landed?.Invoke();
        }

        // Tells everyone the rocket hit the ground fast and exploded at this point.
        public static void Crash(Vector3 position)
        {
            Crashed?.Invoke(position);
        }
    }
}
