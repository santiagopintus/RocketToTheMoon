using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Sits on a part prefab and links it to its data (mass, thrust, fuel...).
    public class RocketPart : MonoBehaviour
    {
        public PartData data;
    }
}
