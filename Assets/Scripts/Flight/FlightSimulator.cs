using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Simulates the vertical flight (a = T / m - g) and reports the altitude to GameState.
    public class FlightSimulator : MonoBehaviour
    {
        [SerializeField] float gravity = 9.81f;
        [SerializeField] float massPerFuel = 1f;
        [SerializeField] float visualScale = 0.01f;
        [SerializeField] float resetDelay = 3f;
        [SerializeField] float flipSpeed = 90f;

        Vector3 startPosition;
        Quaternion startRotation;
        bool falling;
        float landingAltitude;
        float dryMass;
        float thrust;
        float fuelPerSecond;
        float fuel;
        float maxFuel;
        float altitude;
        float velocity;

        public float Gravity => gravity;
        public float MassPerFuel => massPerFuel;

        // Remembers where the rocket sits on the pad.
        void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        // Reads the rocket parts and starts the flight. Called by the Launch button.
        // It also works while the rocket is falling, so it doubles as a reset.
        public void Launch()
        {
            if (GameState.IsFlying) return;

            CancelInvoke(nameof(ResetToPad));
            falling = false;
            transform.position = startPosition;
            transform.rotation = startRotation;
            ReadParts();
            altitude = 0f;
            velocity = 0f;
            landingAltitude = RocketHeight() / visualScale;
            GameState.StartFlight();
        }

        // Measures how tall the rocket is in the scene, from all its visible parts.
        float RocketHeight()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.size.y;
        }

        // Adds up mass, thrust, fuel use and fuel from every part under this object.
        public RocketStats ReadStats()
        {
            var stats = new RocketStats();
            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
                if (part.data != null) stats.Add(part.data);
            return stats;
        }

        // Copies the rocket's totals into the flight numbers.
        void ReadParts()
        {
            RocketStats stats = ReadStats();
            dryMass = stats.DryMass;
            thrust = stats.Thrust;
            fuelPerSecond = stats.FuelPerSecond;
            fuel = stats.Fuel;
            maxFuel = fuel;
        }

        // Moves the flight forward one fixed step, so the same rocket always gives the same result.
        void FixedUpdate()
        {
            if (falling) Fall();
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

            GameState.SetFuel(maxFuel > 0f ? fuel / maxFuel : 0f);
            GameState.SetSpeed(Mathf.Max(0f, velocity));
            GameState.SetAltitude(altitude);

            // Moves the rocket on screen. Real metres are scaled down so the scene stays small.
            transform.position = startPosition + Vector3.up * altitude * visualScale;

            // Once the engines stop, the flight ends at the highest point and the rocket starts to fall.
            if (!burning && velocity <= 0f)
            {
                GameState.EndFlight();
                falling = altitude > landingAltitude;
                if (!falling) Invoke(nameof(ResetToPad), resetDelay);
            }
        }

        // Lets the rocket drop with gravity while it turns nose down, until the nose hits the pad.
        void Fall()
        {
            float dt = Time.fixedDeltaTime;
            velocity -= gravity * dt;
            altitude = Mathf.Max(landingAltitude, altitude + velocity * dt);

            Quaternion noseDown = startRotation * Quaternion.Euler(0f, 0f, 180f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, noseDown, flipSpeed * dt);
            transform.position = startPosition + Vector3.up * altitude * visualScale;
            GameState.SetAltitude(altitude);

            if (altitude <= landingAltitude)
            {
                falling = false;
                Invoke(nameof(ResetToPad), resetDelay);
            }
        }

        // Puts the rocket back on the pad with full fuel, ready for the next launch.
        void ResetToPad()
        {
            falling = false;
            transform.position = startPosition;
            transform.rotation = startRotation;
            GameState.SetFuel(1f);
            GameState.SetSpeed(0f);
            GameState.SetAltitude(0f);
        }
    }
}
