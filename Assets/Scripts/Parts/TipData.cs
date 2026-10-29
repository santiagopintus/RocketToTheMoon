using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a rocket tip (nose cone).
    [CreateAssetMenu(fileName = "Tip", menuName = "Rocket/Tip")]
    public class TipData : PartData
    {
        public override PartType Type => PartType.Tip;
    }
}
