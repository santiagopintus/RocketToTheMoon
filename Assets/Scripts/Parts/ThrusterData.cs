using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a thruster (engine). Thrust in newtons, fuel use in kg per second.
    [CreateAssetMenu(fileName = "Thruster", menuName = "Rocket/Thruster")]
    public class ThrusterData : PartData
    {
        public float thrust = 12f;
        public float fuelPerSecond = 0.04f;

        public override PartType Type => PartType.Thruster;
    }
}
