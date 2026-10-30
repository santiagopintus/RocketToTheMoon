using System.Collections.Generic;
using NUnit.Framework;
using RocketToTheMoon.Core;
using UnityEditor;
using UnityEngine;

namespace RocketToTheMoon.Tests
{
    // Checks that the flight physics behaves like a real rocket, using the real part assets and prefab sizes.
    public class FlightTests
    {
        static readonly string[] Starter = { "Thruster_Small", "Tank_Normal_1", "Body_Short_1", "Nose_Normal" };

        // Loads a part asset by file name.
        static PartData Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<PartData>("Assets/Data/Parts/" + name + ".asset");
        }

        // The box around a part's prefab meshes.
        static Bounds PrefabBounds(PartData data)
        {
            GameObject part = Object.Instantiate(data.prefab);
            Bounds bounds = part.GetComponent<RocketPart>().OwnBounds();
            Object.DestroyImmediate(part);
            return bounds;
        }

        // Turns a part asset into a FlightPart at a position.
        static FlightPart ToFlightPart(PartData data, Vector2 position, float length)
        {
            var part = new FlightPart { position = position, length = length, mass = data.mass, dragArea = data.dragArea, sideArea = data.sideArea };
            if (data is ThrusterData thruster)
            {
                part.thrust = thruster.thrust;
                part.fuelPerSecond = thruster.fuelPerSecond;
            }
            if (data is FuelTankData tank) part.fuel = tank.fuelCapacity;
            return part;
        }

        // Stacks parts from the bottom up and puts fins on stack[finOn] at a height step (0 = bottom, 9 = top).
        // sides says which fins it has: -1 left, 1 right.
        static List<FlightPart> Build(string[] stack, int finOn, string fin, int step, params int[] sides)
        {
            var parts = new List<FlightPart>();
            float bottom = 0f;
            float finY = 0f;
            float finX = 0f;
            Bounds finBounds = PrefabBounds(Load(fin));

            for (int i = 0; i < stack.Length; i++)
            {
                PartData data = Load(stack[i]);
                Bounds bounds = PrefabBounds(data);
                parts.Add(ToFlightPart(data, new Vector2(0f, bottom + bounds.extents.y), bounds.size.y));

                if (i == finOn)
                {
                    finY = bottom + (step + 0.5f) / SnapPoint.LineSteps * bounds.size.y;
                    finX = bounds.extents.x + finBounds.extents.x;
                }
                bottom += bounds.size.y;
            }

            foreach (int side in sides)
                parts.Add(ToFlightPart(Load(fin), new Vector2(side * finX, finY), finBounds.size.y));
            return parts;
        }

        // The starter rocket with small fins on both sides of the body at a height step.
        static List<FlightPart> StarterRocket(int step)
        {
            return Build(Starter, 2, "Fin_Small_1", step, -1, 1);
        }

        // Flies a rocket to the end and returns the sim.
        static RocketSim Fly(List<FlightPart> parts, int seed, float maxGust = 3f)
        {
            var sim = new RocketSim(parts, seed) { maxGust = maxGust };
            sim.RunToEnd();
            return sim;
        }

        // The average best altitude over a few winds.
        static float AverageAltitude(System.Func<List<FlightPart>> rocket)
        {
            float total = 0f;
            for (int seed = 0; seed < 5; seed++) total += Fly(rocket(), seed).MaxAltitude;
            return total / 5f;
        }

        // The same rocket and seed always fly exactly the same.
        [Test]
        public void SameSeed_GivesSameFlight()
        {
            RocketSim a = Fly(StarterRocket(0), 42);
            RocketSim b = Fly(StarterRocket(0), 42);
            Assert.AreEqual(a.MaxAltitude, b.MaxAltitude);
            Assert.AreEqual(a.Centre, b.Centre);
        }

        // The starter rocket with fins low on the body reaches roughly 100 m.
        [Test]
        public void StarterRocket_ReachesAbout100Metres()
        {
            float altitude = AverageAltitude(() => StarterRocket(0));
            Assert.That(altitude, Is.InRange(50f, 150f));
        }

        // Fins low on the rocket keep it straight; fins high up make it flip.
        [Test]
        public void LowFins_FlyHigherThanHighFins()
        {
            Assert.Greater(AverageAltitude(() => StarterRocket(0)), AverageAltitude(() => StarterRocket(9)));
        }

        // Without wind, a rocket with fins on both sides goes straight up.
        [Test]
        public void SymmetricRocket_NoWind_FliesStraight()
        {
            var sim = new RocketSim(StarterRocket(0), 0) { maxGust = 0f };
            while (!sim.Finished && sim.Velocity.y >= 0f) sim.Step();
            Assert.AreEqual(0f, sim.Angle, 0.001f);
            Assert.AreEqual(0f, sim.Centre.x, 0.001f);
        }

        // A single fin pulls the balance point to one side, so the thrust turns the rocket and it drifts.
        [Test]
        public void OneFin_MakesTheRocketDrift()
        {
            RocketSim sim = Fly(Build(Starter, 2, "Fin_Small_1", 0, 1), 0, 0f);
            Assert.Greater(Mathf.Abs(sim.Centre.x), 1f);
        }

        // A rocket heavier than its thrust stays on the pad until the fuel runs out.
        [Test]
        public void TooHeavy_StaysOnPad()
        {
            List<FlightPart> parts = StarterRocket(0);
            parts[0].thrust = 0.5f;
            RocketSim sim = Fly(parts, 0);
            Assert.IsTrue(sim.Finished);
            Assert.AreEqual(0f, sim.MaxAltitude, 0.0001f);
        }

        // Not a real check: writes the average altitude of some rockets to the log, to help balance the part numbers.
        [Test]
        public void BalanceReport()
        {
            var report = new System.Text.StringBuilder("BALANCE\n");
            void Line(string name, System.Func<List<FlightPart>> rocket) => report.AppendLine(name + ": " + AverageAltitude(rocket).ToString("0") + " m");

            Line("starter, fins step 0", () => StarterRocket(0));
            Line("starter, fins step 5", () => StarterRocket(5));
            Line("starter, fins step 9", () => StarterRocket(9));
            Line("starter, no fins", () => Build(Starter, 2, "Fin_Small_1", 0));
            Line("starter, one fin", () => Build(Starter, 2, "Fin_Small_1", 0, 1));
            Line("starter, fins on tank", () => Build(Starter, 1, "Fin_Small_1", 0, -1, 1));
            Line("starter, large fins", () => Build(Starter, 2, "Fin_Large_1", 0, -1, 1));
            Line("sharp nose", () => Build(new[] { "Thruster_Small", "Tank_Normal_1", "Body_Short_1", "Nose_Sharp" }, 2, "Fin_Small_1", 0, -1, 1));
            Line("long body", () => Build(new[] { "Thruster_Small", "Tank_Normal_1", "Body_Long_1", "Nose_Normal" }, 2, "Fin_Small_1", 0, -1, 1));
            Line("two tanks", () => Build(new[] { "Thruster_Small", "Tank_Normal_1", "Tank_Normal_1", "Body_Short_1", "Nose_Normal" }, 3, "Fin_Small_1", 0, -1, 1));
            Line("tank large", () => Build(new[] { "Thruster_Small", "Tank_Large", "Body_Short_1", "Nose_Sharp" }, 2, "Fin_Small_1", 0, -1, 1));
            Line("T2 thruster, starter rest", () => Build(new[] { "Thruster_Medium", "Tank_Normal_1", "Body_Short_1", "Nose_Normal" }, 2, "Fin_Small_1", 0, -1, 1));
            Line("T2 thruster, tank large", () => Build(new[] { "Thruster_Medium", "Tank_Large", "Body_Long_1", "Nose_Sharp" }, 2, "Fin_Large_1", 0, -1, 1));
            Line("T2 thruster, 2 tanks large, fins on bottom tank", () => Build(new[] { "Thruster_Medium", "Tank_Large", "Tank_Large", "Body_Long_1", "Nose_Sharp" }, 1, "Fin_Large_1", 0, -1, 1));
            Line("thruster large, 2 tanks large, fins on bottom tank", () => Build(new[] { "Thruster_Large", "Tank_Large", "Tank_Large", "Body_Long_1", "Nose_Sharp" }, 1, "Fin_Large_1", 0, -1, 1));
            Line("T3 sipper, T2 rest", () => Build(new[] { "Thruster_1_T3", "Tank_Large", "Tank_Large", "Body_Long_1", "Nose_Sharp" }, 1, "Fin_Large_1", 0, -1, 1));
            Line("T3 sipper, 2 compact, mid, dome", () => Build(new[] { "Thruster_1_T3", "Tank_1_T3", "Tank_1_T3", "Body_2_T3", "Nose_1_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 sipper, 2 compact, mid, cone", () => Build(new[] { "Thruster_1_T3", "Tank_1_T3", "Tank_1_T3", "Body_2_T3", "Nose_2_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 sipper, 2 compact, mid, spike", () => Build(new[] { "Thruster_1_T3", "Tank_1_T3", "Tank_1_T3", "Body_2_T3", "Nose_3_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 sipper, tall, mid, cone", () => Build(new[] { "Thruster_1_T3", "Tank_2_T3", "Body_2_T3", "Nose_2_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 booster, 2 tall, mid, cone", () => Build(new[] { "Thruster_2_T3", "Tank_2_T3", "Tank_2_T3", "Body_2_T3", "Nose_2_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 booster, 2 tall, tower, cone", () => Build(new[] { "Thruster_2_T3", "Tank_2_T3", "Tank_2_T3", "Body_3_T3", "Nose_2_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 booster, giant, tower, cone", () => Build(new[] { "Thruster_2_T3", "Tank_3_T3", "Body_3_T3", "Nose_2_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 titan, giant, tower, spike", () => Build(new[] { "Thruster_3_T3", "Tank_3_T3", "Body_3_T3", "Nose_3_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 titan, 3 tall, tower, spike", () => Build(new[] { "Thruster_3_T3", "Tank_2_T3", "Tank_2_T3", "Tank_2_T3", "Body_3_T3", "Nose_3_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 titan, giant + tall, tower, spike", () => Build(new[] { "Thruster_3_T3", "Tank_3_T3", "Tank_2_T3", "Body_3_T3", "Nose_3_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Line("T3 titan, 2 giants, tower, spike", () => Build(new[] { "Thruster_3_T3", "Tank_3_T3", "Tank_3_T3", "Body_3_T3", "Nose_3_T3" }, 1, "Fin_1_T3", 0, -1, 1));
            Debug.Log(report);
        }
    }
}
