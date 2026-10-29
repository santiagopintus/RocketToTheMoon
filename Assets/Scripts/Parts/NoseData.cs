using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a rocket nose (nose cone).
    [CreateAssetMenu(fileName = "Nose", menuName = "Rocket/Nose")]
    public class NoseData : PartData
    {
        public override PartType Type => PartType.Nose;
    }
}
