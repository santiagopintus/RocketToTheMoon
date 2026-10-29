using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a fuel tank. Fuel capacity is the mass of fuel in kg.
    [CreateAssetMenu(fileName = "FuelTank", menuName = "Rocket/Fuel Tank")]
    public class FuelTankData : PartData
    {
        public float fuelCapacity = 0.05f;

        public override PartType Type => PartType.FuelTank;

        // Tanks use the shared stacking rule.
        public override bool Accepts(SnapSide side, PartType type)
        {
            return StackAccepts(side, type);
        }
    }
}
