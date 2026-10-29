using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a rocket body part.
    [CreateAssetMenu(fileName = "Body", menuName = "Rocket/Body")]
    public class BodyData : PartData
    {
        public override PartType Type => PartType.Body;

        // Bodies use the shared stacking rule.
        public override bool Accepts(SnapSide side, PartType type)
        {
            return StackAccepts(side, type);
        }
    }
}
