using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // The rocket's flight physics in 2D: it moves left/right and up/down and spins facing the camera.
    // Thrust, gravity, drag and side forces act on each part where it really is, so a badly built rocket turns or tumbles.
    // Plain C# with a fixed time step and a seeded random wind, so the same rocket and seed always fly the same. See Docs/FlightPhysics.md.
    public class RocketSim
    {
        public const float Dt = 0.005f;

        public float gravity = 9.81f;
        public float airDensity = 1.225f;
        public float railLength = 1f;
        public float maxGust = 3f;
        public float spinDamping = 0.0002f;
        public float maxTime = 120f;

        // How far the ground is below the pad top. The rocket lands when it falls this far below where it started.
        public float groundDepth = 0f;

        readonly List<FlightPart> parts;
        readonly float startFuel;
        readonly float[] gustSize = new float[3];
        readonly float[] gustSpeed = new float[3];
        readonly float[] gustPhase = new float[3];

        // Centre of mass in the world (metres, the rocket root starts at 0, 0).
        public Vector2 Centre { get; private set; }
        public Vector2 StartCentre { get; private set; }
        public Vector2 Velocity { get; private set; }

        // Centre of mass from the rocket root. Moves as fuel burns.
        public Vector2 CentreLocal { get; private set; }

        // Angle from straight up in radians (positive = leaning left) and how fast it changes.
        public float Angle { get; private set; }
        public float Spin { get; private set; }

        public float Time { get; private set; }
        public float MaxAltitude { get; private set; }
        public bool Burning { get; private set; }
        public bool LeftGround { get; private set; }
        public bool OffRail { get; private set; }
        public bool Landed { get; private set; }

        public float Altitude => Centre.y - StartCentre.y;
        public float FuelLeft => startFuel > 0f ? TotalFuel() / startFuel : 0f;

        // Over when it is back on the ground, out of time, or it never got off the pad and the fuel is gone.
        public bool Finished => Landed || Time >= maxTime || (!LeftGround && !CanBurn());

        // Where the rocket root is in the world, from the centre of mass and the angle.
        public Vector2 RootPosition => Centre - Rotate(CentreLocal, Angle);

        // Sets up the rocket on the pad and picks this flight's wind from the seed.
        // The rail leans railTilt degrees to a random side (from the seed), so the rocket flies an arc like a real one.
        public RocketSim(List<FlightPart> rocketParts, int seed, float railTilt = 0f)
        {
            parts = rocketParts;
            startFuel = TotalFuel();
            CentreLocal = CentreOfMass(TotalMass());

            var random = new System.Random(seed);
            for (int i = 0; i < gustSize.Length; i++)
            {
                gustSize[i] = (float)random.NextDouble() / gustSize.Length;
                gustSpeed[i] = 0.3f + 1.7f * (float)random.NextDouble();
                gustPhase[i] = 2f * Mathf.PI * (float)random.NextDouble();
            }

            float side = random.Next(2) == 0 ? -1f : 1f;
            Angle = side * railTilt * Mathf.Deg2Rad;
            Centre = Rotate(CentreLocal, Angle);
            StartCentre = Centre;
        }

        // Runs the whole flight at once. Used by tests and to skip a flight.
        public void RunToEnd()
        {
            while (!Finished)
                Step();
        }

        // Moves the flight forward by one time step.
        public void Step()
        {
            if (Finished)
                return;
            Time += Dt;

            Burning = BurnFuel();
            float mass = TotalMass();
            CentreLocal = CentreOfMass(mass);
            float inertia = Inertia(CentreLocal);

            Vector2 up = new Vector2(-Mathf.Sin(Angle), Mathf.Cos(Angle));
            Vector2 side = new Vector2(Mathf.Cos(Angle), Mathf.Sin(Angle));
            Vector2 wind = new Vector2(maxGust * Gust(), 0f);

            Vector2 force = new Vector2(0f, -mass * gravity);
            float torque = -spinDamping * Spin;

            foreach (FlightPart part in parts)
            {
                Vector2 r = Rotate(part.position - CentreLocal, Angle);
                Vector2 air = AirForce(part, r, wind, side);
                force += air;
                torque += Cross(r, air);

                // Thrusters push along the rocket's middle line.
                if (Burning && part.thrust > 0f)
                {
                    Vector2 thrust = up * part.thrust;
                    Vector2 thrustPoint = Rotate(
                        new Vector2(0f, part.position.y) - CentreLocal,
                        Angle
                    );
                    force += thrust;
                    torque += Cross(thrustPoint, thrust);
                }
            }

            // On the launch rail the rocket can only slide along the rail.
            if (Altitude >= railLength)
                OffRail = true;
            if (!OffRail)
            {
                force = Vector2.Dot(force, up) * up;
                torque = 0f;
                Spin = 0f;
                Velocity = Vector2.Dot(Velocity, up) * up;
            }

            Velocity += force / mass * Dt;
            Centre += Velocity * Dt;
            Spin += torque / inertia * Dt;
            Angle += Spin * Dt;

            // The pad holds the rocket up until the thrust is strong enough.
            if (!LeftGround && Centre.y <= StartCentre.y)
            {
                Centre = new Vector2(Centre.x, StartCentre.y);
                Velocity = Vector2.zero;
            }

            if (Altitude > 0f)
                LeftGround = true;
            if (LeftGround && Altitude <= -groundDepth)
                Landed = true;

            MaxAltitude = Mathf.Max(MaxAltitude, Altitude);
        }

        // Drag against the air flow and the side push when the part meets the air at an angle.
        Vector2 AirForce(FlightPart part, Vector2 r, Vector2 wind, Vector2 side)
        {
            // The part also moves because the rocket spins, which is what calms the spinning down.
            Vector2 pointVelocity = Velocity + Spin * new Vector2(-r.y, r.x);
            Vector2 air = pointVelocity - wind;
            float speed = air.magnitude;
            float pressure = 0.5f * airDensity * speed;

            Vector2 drag = -pressure * part.dragArea * air;
            Vector2 push = -pressure * part.sideArea * Vector2.Dot(air, side) * side;
            return drag + push;
        }

        // Burns this step's fuel, taken from every tank evenly. Returns true if the thrusters are firing.
        bool BurnFuel()
        {
            if (!CanBurn())
                return false;

            float total = TotalFuel();
            float burn = Mathf.Min(total, FuelPerSecond() * Dt);
            foreach (FlightPart part in parts)
                part.fuel -= burn * part.fuel / total;
            return true;
        }

        // Returns true if there is fuel and a thruster to burn it.
        bool CanBurn()
        {
            return TotalFuel() > 0f && FuelPerSecond() > 0f;
        }

        // The wind right now, from -1 to 1: a few slow waves added together.
        float Gust()
        {
            float gust = 0f;
            for (int i = 0; i < gustSize.Length; i++)
                gust += gustSize[i] * Mathf.Sin(gustSpeed[i] * Time + gustPhase[i]);
            return gust;
        }

        // The fuel all tanks hold together.
        float TotalFuel()
        {
            float fuel = 0f;
            foreach (FlightPart part in parts)
                fuel += part.fuel;
            return fuel;
        }

        // The fuel all thrusters burn per second together.
        float FuelPerSecond()
        {
            float rate = 0f;
            foreach (FlightPart part in parts)
                rate += part.fuelPerSecond;
            return rate;
        }

        // The mass of all parts and their fuel.
        float TotalMass()
        {
            float mass = 0f;
            foreach (FlightPart part in parts)
                mass += part.mass + part.fuel;
            return Mathf.Max(mass, 0.001f);
        }

        // The balance point of the rocket, from the rocket root.
        Vector2 CentreOfMass(float mass)
        {
            Vector2 sum = Vector2.zero;
            foreach (FlightPart part in parts)
                sum += (part.mass + part.fuel) * part.position;
            return sum / mass;
        }

        // How hard the rocket is to spin around its centre of mass.
        float Inertia(Vector2 centre)
        {
            float inertia = 0f;
            foreach (FlightPart part in parts)
            {
                float mass = part.mass + part.fuel;
                inertia +=
                    mass * (part.position - centre).sqrMagnitude
                    + mass * part.length * part.length / 12f;
            }
            return Mathf.Max(inertia, 0.00001f);
        }

        // Turns a point by an angle (positive = anticlockwise).
        public static Vector2 Rotate(Vector2 point, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        // How much a force at point r turns the rocket (positive = anticlockwise).
        static float Cross(Vector2 r, Vector2 force)
        {
            return r.x * force.y - r.y * force.x;
        }
    }
}
