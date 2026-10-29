using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Data for a fin (wing).
    [CreateAssetMenu(fileName = "Fin", menuName = "Rocket/Fin")]
    public class FinData : PartData
    {
        public override PartType Type => PartType.Fin;
    }
}
