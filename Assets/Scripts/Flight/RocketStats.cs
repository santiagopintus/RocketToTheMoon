namespace RocketToTheMoon.Core
{
    // The totals of a rocket's parts and whether it can take off. Plain C#, so the server can use it too.
    public class RocketStats
    {
        public int PartCount;
        public float DryMass;
        public float Thrust;
        public float FuelPerSecond;
        public float Fuel;

        // Adds one part's numbers to the totals.
        public void Add(PartData part)
        {
            PartCount++;
            DryMass += part.mass;
            if (part is ThrusterData thruster)
            {
                Thrust += thruster.thrust;
                FuelPerSecond += thruster.fuelPerSecond;
            }
            if (part is FuelTankData tank)
                Fuel += tank.fuelCapacity;
        }

        // The mass with full tanks.
        public float FullMass(float massPerFuel)
        {
            return DryMass + Fuel * massPerFuel;
        }

        // Thrust divided by weight for a given mass. Above 1 the rocket goes up.
        public float ThrustToWeight(float mass, float gravity)
        {
            return mass > 0f ? Thrust / (mass * gravity) : 0f;
        }
    }
}
