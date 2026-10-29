using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Simulates the vertical flight (a = T / m - g) and reports the altitude to GameState.
    public class FlightSimulator : MonoBehaviour
    {
        [SerializeField] float gravity = 9.81f;
        [SerializeField] float massPerFuel = 1f;
        [SerializeField] float visualScale = 0.01f;
        [SerializeField] float pushSpeed = 0.3f;
        [SerializeField] float spinSpeed = 2f;
        [SerializeField] float fallDrag = 0.5f;

        Vector3 startPosition;
        Quaternion startRotation;
        Rigidbody body;
        bool landed;
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

            ResetToPad();
            ReadParts();
            altitude = 0f;
            velocity = 0f;
            GameState.StartFlight();
        }

        // The box around all the visible parts of the rocket.
        Bounds RocketBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
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
            if (body != null) ShowFallAltitude();
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
                if (altitude > 0f) StartFalling();
                else GameState.Land();
            }
        }

        // Hands the rocket to Unity physics with a small random push and spin, so it tumbles down and lands anywhere.
        void StartFalling()
        {
            Bounds bounds = RocketBounds();
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;

            body = gameObject.AddComponent<Rigidbody>();
            body.linearDamping = fallDrag;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Keeps the rocket flat to the camera: it moves left, right, up and down and only spins facing us.
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

            float side = Random.value < 0.5f ? -1f : 1f;
            body.linearVelocity = Vector3.right * Random.Range(-pushSpeed, pushSpeed);
            body.angularVelocity = Vector3.forward * side * Random.Range(0.5f, 1f) * spinSpeed;
        }

        // Shows the falling rocket's height above the pad on the altitude counter.
        void ShowFallAltitude()
        {
            GameState.SetAltitude(Mathf.Max(0f, (transform.position.y - startPosition.y) / visualScale));
        }

        // Tells everyone the rocket landed the first time the falling rocket hits something.
        void OnCollisionEnter(Collision collision)
        {
            if (body == null || landed) return;

            landed = true;
            GameState.Land();
        }

        // Removes the physics and puts the rocket back on the pad with full fuel, ready for the next launch.
        // Called by the result popup's Continue button.
        public void ResetToPad()
        {
            landed = false;
            if (body != null)
            {
                // Removed right away, so physics cannot move the rocket after it is put back.
                DestroyImmediate(body);
                DestroyImmediate(GetComponent<BoxCollider>());
                body = null;
            }

            transform.position = startPosition;
            transform.rotation = startRotation;
            GameState.SetFuel(1f);
            GameState.SetSpeed(0f);
            GameState.SetAltitude(0f);
        }
    }
}
