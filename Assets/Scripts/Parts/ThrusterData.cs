using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a thruster (engine).
    [CreateAssetMenu(fileName = "Thruster", menuName = "Rocket/Thruster")]
    public class ThrusterData : PartData
    {
        public float thrust = 4000f;
        public float fuelPerSecond = 2f;

        public override PartType Type => PartType.Thruster;
    }
}
