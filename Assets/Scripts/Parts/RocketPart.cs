using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Sits on a part prefab and links it to its data (mass, thrust, fuel...).
    public class RocketPart : MonoBehaviour
    {
        public PartData data;

        // The snap point on another part (or the pad) this part is attached to. Set by the builder.
        [System.NonSerialized] public SnapPoint attachedTo;

        // For a fin: which height step of the side line it sits on. -1 for other parts.
        [System.NonSerialized] public int step = -1;

        // The box around this part's own meshes, without the parts attached to it.
        public Bounds OwnBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (MeshRenderer r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponentInParent<RocketPart>() != this) continue;
                if (first) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                first = false;
            }
            return bounds;
        }
    }
}
