using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Turns the built rocket into a RocketSim at launch, runs it, and moves and turns the rocket on screen.
    // The flight ends (credits and result popup) once the rocket falls back to part of its highest altitude, then it keeps falling to the ground.
    public class FlightSimulator : MonoBehaviour
    {
        [SerializeField] float gravity = 9.81f;
        [SerializeField] float visualScale = 0.01f;
        [SerializeField] float railLength = 1f;
        [SerializeField] float maxGust = 3f;
        [SerializeField] float noNoseDrag = 0.004f;
        [SerializeField] float endAtFraction = 0.7f;

        Vector3 startPosition;
        Quaternion startRotation;
        RocketSim sim;

        public float Gravity => gravity;

        // Remembers where the rocket sits on the pad.
        void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        // Reads the rocket parts and starts the flight with a new random wind. Called by the Launch button.
        // It also works while the rocket is falling, so it doubles as a reset.
        public void Launch()
        {
            if (GameState.IsFlying) return;

            ResetToPad();
            sim = new RocketSim(ReadParts(), Random.Range(0, int.MaxValue));
            sim.gravity = gravity;
            sim.railLength = railLength;
            sim.maxGust = maxGust;
            GameState.StartFlight();
        }

        // Runs the rest of the flight at once, so the result shows right away. Called by the Skip button.
        // The highest altitude is passed on first, because the counter never sees it while skipping.
        public void Skip()
        {
            if (sim == null || !GameState.IsFlying) return;

            sim.RunToEnd();
            GameState.SetAltitude(sim.MaxAltitude);
        }

        // Adds up mass, thrust, fuel use and fuel from every part under this object.
        public RocketStats ReadStats()
        {
            var stats = new RocketStats();
            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
                if (part.data != null) stats.Add(part.data);
            return stats;
        }

        // Turns every placed part into a FlightPart at its real position on the rocket.
        // A rocket without a nose gets extra drag at its flat top.
        List<FlightPart> ReadParts()
        {
            var parts = new List<FlightPart>();
            bool hasNose = false;
            float top = 0f;

            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
            {
                if (part.data == null) continue;
                Bounds bounds = part.OwnBounds();
                Vector3 centre = transform.InverseTransformPoint(bounds.center);
                top = Mathf.Max(top, centre.y + bounds.extents.y);
                if (part.data.Type == PartType.Nose) hasNose = true;

                var flightPart = new FlightPart
                {
                    position = new Vector2(centre.x, centre.y),
                    length = bounds.size.y,
                    mass = part.data.mass,
                    dragArea = part.data.dragArea,
                    sideArea = part.data.sideArea
                };
                if (part.data is ThrusterData thruster)
                {
                    flightPart.thrust = thruster.thrust;
                    flightPart.fuelPerSecond = thruster.fuelPerSecond;
                }
                if (part.data is FuelTankData tank)
                    flightPart.fuel = tank.fuelCapacity;
                parts.Add(flightPart);
            }

            if (!hasNose && parts.Count > 0)
                parts.Add(new FlightPart { position = new Vector2(0f, top), dragArea = noNoseDrag });
            return parts;
        }

        // Moves the flight forward in fixed steps, so the same rocket and wind always give the same result.
        void FixedUpdate()
        {
            if (sim == null) return;

            int steps = Mathf.Max(1, Mathf.RoundToInt(Time.fixedDeltaTime / RocketSim.Dt));
            for (int i = 0; i < steps; i++) sim.Step();

            ShowRocket();
            GameState.SetAltitude(Mathf.Max(0f, sim.Altitude));
            if (!GameState.IsFlying) return;

            GameState.SetFuel(sim.FuelLeft);
            GameState.SetSpeed(sim.Velocity.magnitude);

            bool fellBack = sim.MaxAltitude > 0f && sim.Velocity.y < 0f && sim.Altitude <= sim.MaxAltitude * endAtFraction;
            if (sim.Finished || fellBack)
            {                GameState.EndFlight();
                GameState.Land();
            }
        }

        // Puts the rocket where the sim says, turned by its angle. Real metres are scaled down so the scene stays small,
        // but the rocket turns around its real centre of mass.
        void ShowRocket()
        {
            Vector2 moved = (sim.Centre - sim.StartCentre) * visualScale;
            Vector2 root = sim.StartCentre + moved - RocketSim.Rotate(sim.CentreLocal, sim.Angle);
            transform.position = startPosition + startRotation * new Vector3(root.x, root.y, 0f);
            transform.rotation = startRotation * Quaternion.AngleAxis(sim.Angle * Mathf.Rad2Deg, Vector3.forward);
        }

        // Stops the flight and puts the rocket back on the pad with full fuel, ready for the next launch.
        // Called by the result popup's Continue button.
        public void ResetToPad()
        {
            sim = null;
            transform.position = startPosition;
            transform.rotation = startRotation;
            GameState.SetFuel(1f);
            GameState.SetSpeed(0f);
            GameState.SetAltitude(0f);
        }
    }
}
