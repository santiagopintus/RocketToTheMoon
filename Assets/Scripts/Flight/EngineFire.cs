using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Shows fire coming out of the lowest thruster while the thrusters are firing.
    // The particles stay where they were born, so a fast rocket leaves a long trail behind it.
    public class EngineFire : MonoBehaviour
    {
        [SerializeField] FlightSimulator simulator;
        [SerializeField] ParticleSystem fire;

        Vector3 nozzle;

        // Starts listening for launches.
        void OnEnable()
        {
            GameState.FlightStarted += FindNozzle;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.FlightStarted -= FindNozzle;
        }

        // Remembers the bottom middle of the lowest thruster, measured from the rocket root.
        // Runs at launch, while the rocket still stands straight on the pad.
        void FindNozzle()
        {
            float lowest = float.MaxValue;
            foreach (RocketPart part in simulator.GetComponentsInChildren<RocketPart>())
            {
                if (!(part.data is ThrusterData)) continue;
                Bounds bounds = part.OwnBounds();
                if (bounds.min.y >= lowest) continue;
                lowest = bounds.min.y;
                nozzle = simulator.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            }
        }

        // Keeps the fire under the thruster, pointing out of the back of the rocket, and only emits while burning.
        void LateUpdate()
        {
            if (simulator == null || fire == null) return;

            Transform rocket = simulator.transform;
            fire.transform.position = rocket.TransformPoint(nozzle);
            fire.transform.rotation = Quaternion.LookRotation(-rocket.up, rocket.forward);

            var emission = fire.emission;
            emission.enabled = simulator.Burning;
        }
    }
}
