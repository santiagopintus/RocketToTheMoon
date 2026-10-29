using UnityEngine;

namespace RocketToTheMoon.Core
{
    // The four sides a snap point can be on.
    public enum SnapSide
    {
        Top,
        Bottom,
        Left,
        Right
    }

    // A point on a part prefab where other parts can attach. What it accepts is decided by the part's data class.
    public class SnapPoint : MonoBehaviour
    {
        public SnapSide side;

        // True once a part is attached here.
        [System.NonSerialized] public bool taken;

        // Draws the point in the Scene view so it is easy to place.
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.01f);
        }
    }
}
