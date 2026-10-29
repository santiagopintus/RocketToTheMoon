using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a wing (fin).
    [CreateAssetMenu(fileName = "Wing", menuName = "Rocket/Wing")]
    public class WingData : PartData
    {
        public override PartType Type => PartType.Wing;
    }
}
