using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Sits on a part prefab and links it to its data (mass, thrust, fuel...).
    public class RocketPart : MonoBehaviour
    {
        public PartData data;

        // The snap point on another part (or the pad) this part is attached to. Set by the builder.
        [System.NonSerialized] public SnapPoint attachedTo;

        // The other fin of a pair. Set by the builder.
        [System.NonSerialized] public RocketPart pair;
    }
}
