using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Shows fire coming out of the lowest thruster while the thrusters are firing.
    // The particles stay where they were born, so a fast rocket leaves a long trail behind it.
    // The fire glows with bloom, leaves smoke and lights the tail of the rocket.
    public class EngineFire : MonoBehaviour
    {
        [SerializeField]
        FlightSimulator simulator;

        [SerializeField]
        ParticleSystem fire;

        [SerializeField]
        ParticleSystem smoke;

        [Header("Fire length")]
        [Tooltip("Total thrust, in newtons, that gives the full-length fire set on the particle system.")]
        [SerializeField]
        float fullLengthThrust = 150f;

        [Tooltip("Shortest fire, as a fraction of the full length, so weak thrusters still show a flame.")]
        [SerializeField]
        float minLength = 0.3f;

        [Header("Tail light")]
        [SerializeField]
        Color lightColour = new Color(1f, 0.34f, 0f);

        [SerializeField]
        float lightIntensity = 5f;

        [SerializeField]
        float lightRange = 1f;

        [Tooltip("How far below the nozzle the light sits, in metres.")]
        [SerializeField]
        float lightOffset = 0.1f;

        Vector3 nozzle;
        Light tailLight;
        ParticleSystem.MinMaxCurve fullSpeed;

        // Starts listening for launches and creates the tail light.
        void OnEnable()
        {
            GameState.FlightStarted += FindNozzle;
            GameState.FlightStarted += SetFireLength;
            fullSpeed = fire.main.startSpeed;

            if (tailLight == null)
            {
                var go = new GameObject("TailLight");
                go.transform.SetParent(transform, false);
                tailLight = go.AddComponent<Light>();
                tailLight.type = LightType.Point;
                tailLight.shadows = LightShadows.None;
                tailLight.color = lightColour;
                tailLight.range = lightRange;
                tailLight.enabled = false;
            }
        }

        void OnDisable()
        {
            GameState.FlightStarted -= FindNozzle;
            GameState.FlightStarted -= SetFireLength;
        }

        // Remembers the bottom middle of the lowest thruster, measured from the rocket root.
        // Runs at launch, while the rocket still stands straight on the pad.
        void FindNozzle()
        {
            float lowest = float.MaxValue;
            foreach (RocketPart part in simulator.GetComponentsInChildren<RocketPart>())
            {
                if (!(part.data is ThrusterData))
                    continue;
                Bounds bounds = part.OwnBounds();
                if (bounds.min.y >= lowest)
                    continue;
                lowest = bounds.min.y;
                nozzle = simulator.transform.InverseTransformPoint(
                    new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)
                );
            }
        }

        // Makes the fire longer for stronger rockets by making the particles faster.
        void SetFireLength()
        {
            float thrust = 0f;
            foreach (RocketPart part in simulator.GetComponentsInChildren<RocketPart>())
            {
                if (part.data is ThrusterData thruster)
                    thrust += thruster.thrust;
            }

            float length = Mathf.Clamp(Mathf.Sqrt(thrust / fullLengthThrust), minLength, 1f);
            var main = fire.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(fullSpeed.constantMin * length, fullSpeed.constantMax * length);
        }

        // Keeps the fire under the thruster, pointing out of the back of the rocket, and only emits while burning.
        void LateUpdate()
        {
            if (simulator == null || fire == null)
                return;

            Transform rocket = simulator.transform;
            fire.transform.position = rocket.TransformPoint(nozzle);
            fire.transform.rotation = Quaternion.LookRotation(-rocket.up, rocket.forward);

            var emission = fire.emission;
            emission.enabled = simulator.Burning;

            if (smoke != null)
            {
                var smokeEmission = smoke.emission;
                smokeEmission.enabled = simulator.Burning;
            }

            // The light follows the nozzle and flickers while burning.
            tailLight.enabled = simulator.Burning;
            tailLight.transform.position = fire.transform.position - rocket.up * lightOffset;
            tailLight.intensity = lightIntensity * Random.Range(0.8f, 1.2f);
        }
    }
}
