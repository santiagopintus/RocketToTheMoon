using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Draws an outline around one part (and everything attached to it) by moving its meshes to the "Outline" layer.
    // OutlineRendererFeature draws the outline around that layer.
    public static class PartOutline
    {
        // The part that has the outline now, or null.
        public static GameObject Current { get; private set; }

        // Clears the outline when play mode starts, in case it was left on last time.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Current = null;
        }

        // Moves the outline to this part. Null removes it.
        public static void Show(GameObject part)
        {
            if (Current == part)
                return;

            if (Current != null)
                SetLayer(Current, LayerMask.NameToLayer("Default"));
            Current = part;
            if (part != null)
                SetLayer(part, LayerMask.NameToLayer("Outline"));
        }

        // Puts every mesh of the part on the given layer.
        static void SetLayer(GameObject part, int layer)
        {
            if (layer < 0)
                return;
            foreach (MeshRenderer r in part.GetComponentsInChildren<MeshRenderer>(true))
                r.gameObject.layer = layer;
        }
    }
}
