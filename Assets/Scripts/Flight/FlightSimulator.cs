using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Simulates the vertical flight (a = T / m - g) and reports the altitude to GameState.
    public class FlightSimulator : MonoBehaviour
    {
        [SerializeField] float gravity = 9.81f;
        [SerializeField] float massPerFuel = 1f;

        float dryMass;
        float thrust;
        float fuelPerSecond;
        float fuel;
        float altitude;
        float velocity;

        // Reads the rocket parts and starts the flight. Called by the Launch button.
        public void Launch()
        {
            if (GameState.IsFlying) return;

            ReadParts();
            altitude = 0f;
            velocity = 0f;
            GameState.StartFlight();
        }

        // Adds up mass, thrust, fuel use and fuel from every part under this object.
        void ReadParts()
        {
            dryMass = 0f;
            thrust = 0f;
            fuelPerSecond = 0f;
            fuel = 0f;

            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
            {
                if (part.data == null) continue;

                dryMass += part.data.mass;
                if (part.data is ThrusterData thruster)
                {
                    thrust += thruster.thrust;
                    fuelPerSecond += thruster.fuelPerSecond;
                }
                if (part.data is FuelTankData tank)
                    fuel += tank.fuelCapacity;
            }
        }

        // Moves the flight forward one fixed step, so the same rocket always gives the same result.
        void FixedUpdate()
        {
            if (!GameState.IsFlying) return;

            float dt = Time.fixedDeltaTime;
            bool burning = fuel > 0f && thrust > 0f;
            float mass = dryMass + fuel * massPerFuel;

            float acceleration = -gravity;
            if (burning)
            {
                acceleration += thrust / mass;
                fuel = Mathf.Max(0f, fuel - fuelPerSecond * dt);
            }

            velocity += acceleration * dt;
            altitude += velocity * dt;

            // The pad stops the rocket from going below the ground.
            if (altitude < 0f)
            {
                altitude = 0f;
                velocity = 0f;
            }

            GameState.SetAltitude(altitude);

            // Once the engines stop, the flight ends at the highest point.
            if (!burning && velocity <= 0f)
                GameState.EndFlight();
        }
    }
}
