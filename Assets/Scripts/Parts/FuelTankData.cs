using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a fuel tank.
    [CreateAssetMenu(fileName = "FuelTank", menuName = "Rocket/Fuel Tank")]
    public class FuelTankData : PartData
    {
        public float fuelCapacity = 90f;

        public override PartType Type => PartType.FuelTank;

        // Tanks use the shared stacking rule.
        public override bool Accepts(SnapSide side, PartType type)
        {
            return StackAccepts(side, type);
        }
    }
}
