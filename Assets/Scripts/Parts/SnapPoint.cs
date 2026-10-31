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
    // Left and right points are lines instead: fins can sit at any of 10 heights along the side of the part.
    public class SnapPoint : MonoBehaviour
    {
        public const int LineSteps = 10;

        public SnapSide side;

        // True once a part is attached here (top and bottom points).
        [System.NonSerialized]
        public bool taken;

        // Which heights of the side line already have a fin.
        [System.NonSerialized]
        public bool[] takenSteps = new bool[LineSteps];

        // Returns true for the side points, which are lines with height steps.
        public bool IsLine => side == SnapSide.Left || side == SnapSide.Right;

        // Where a height step is: on this point's side, from the bottom of the part (step 0) to the top (step 9).
        public Vector3 StepPosition(int step)
        {
            Bounds bounds = transform.parent.GetComponent<RocketPart>().OwnBounds();
            float height = bounds.min.y + (step + 0.5f) / LineSteps * bounds.size.y;
            return new Vector3(transform.position.x, height, transform.position.z);
        }

        // Draws the point in the Scene view so it is easy to place.
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.01f);
        }
    }
}
