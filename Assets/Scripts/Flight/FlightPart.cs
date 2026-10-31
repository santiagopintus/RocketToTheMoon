using UnityEngine;

namespace RocketToTheMoon.Core
{
    // One rocket part as the flight sim sees it. Plain C#, so tests and the server can use it.
    public class FlightPart
    {
        public Vector2 position; // centre of the part from the rocket root, in metres (x sideways, y up)
        public float length; // height of the part in metres, for how hard it is to spin
        public float mass; // kg, without fuel
        public float dragArea; // m^2, slows the rocket down
        public float sideArea; // m^2, pushes the part sideways when the rocket flies at an angle
        public float fuel; // kg of fuel left (tanks only)
        public float thrust; // N (thrusters only)
        public float fuelPerSecond; // kg/s (thrusters only)
    }
}
