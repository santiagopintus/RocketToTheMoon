using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Prototyping only: builds a small test rocket at the start so there is something to launch.
    // Turn off "Spawn On Start" once players can build their own rocket.
    public class PrototypeRocket : MonoBehaviour
    {
        [SerializeField] bool spawnOnStart = true;
        [SerializeField] BodyData body;
        [SerializeField] NoseData nose;
        [SerializeField] FuelTankData tank;
        [SerializeField] ThrusterData thruster;
        [SerializeField] FinData fin;

        // Builds the rocket: nose on top, tank and thruster below, fins on the sides.
        void Start()
        {
            if (!spawnOnStart) return;

            GameObject bodyPart = Instantiate(body.prefab, transform);
            Attach(nose, bodyPart, SnapSide.Top);
            GameObject tankPart = Attach(tank, bodyPart, SnapSide.Bottom);
            Attach(thruster, tankPart, SnapSide.Bottom);
            Attach(fin, bodyPart, SnapSide.Left);
            Attach(fin, bodyPart, SnapSide.Right);

            SitOnPad(bodyPart);
        }

        // Spawns a part at the given snap point of another part, with the snap point's position and rotation.
        GameObject Attach(PartData data, GameObject parentPart, SnapSide side)
        {
            foreach (SnapPoint snap in parentPart.GetComponentsInChildren<SnapPoint>())
            {
                if (snap.side == side && snap.transform.parent == parentPart.transform)
                    return Instantiate(data.prefab, snap.transform.position, snap.transform.rotation, parentPart.transform);
            }

            Debug.LogWarning(parentPart.name + " has no " + side + " snap point");
            return null;
        }

        // Lifts the rocket so its lowest point touches the top of the pad.
        void SitOnPad(GameObject bodyPart)
        {
            float lowest = float.MaxValue;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                lowest = Mathf.Min(lowest, r.bounds.min.y);

            bodyPart.transform.position += Vector3.up * (transform.position.y - lowest);
        }
    }
}
