using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Moves the camera with the rocket (up, down and sideways), so it stays in view while flying.
    // The faster the rocket goes, the more the camera falls behind, so you can see it speed up.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Transform target;
        [SerializeField] float lagPerSpeed = 0.0008f;
        [SerializeField] float maxLag = 0.15f;

        Vector3 lastCentre;
        Vector3 lastRootPosition;
        Quaternion lastRootRotation;
        float lastLag;

        // Remembers where the rocket starts.
        void Start()
        {
            if (target == null) return;
            lastCentre = Centre();
            lastRootPosition = target.position;
            lastRootRotation = target.rotation;
        }

        // Moves the camera by how much the middle of the rocket moved, minus the extra lag from its speed.
        // Only when the rocket itself moved, so adding parts while building does not move the camera.
        void LateUpdate()
        {
            if (targetCamera == null || target == null) return;

            Vector3 centre = Centre();
            bool rocketMoved = target.position != lastRootPosition || target.rotation != lastRootRotation;
            float lag = Mathf.Min(GameState.Speed * lagPerSpeed, maxLag);

            if (rocketMoved)
            {
                Vector3 moved = centre - lastCentre;
                moved.y -= lag - lastLag;
                moved.z = 0f;
                targetCamera.transform.position += moved;
                lastLag = lag;
            }

            lastCentre = centre;
            lastRootPosition = target.position;
            lastRootRotation = target.rotation;
        }

        // The middle of all the rocket's meshes. Unlike the rocket's root, it does not swing around when the rocket spins.
        Vector3 Centre()
        {
            MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return target.position;

            Bounds bounds = renderers[0].bounds;
            foreach (MeshRenderer r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.center;
        }
    }
}
