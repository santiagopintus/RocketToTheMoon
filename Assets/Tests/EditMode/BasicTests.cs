using NUnit.Framework;
using RocketToTheMoon.Core;
using UnityEditor;

namespace RocketToTheMoon.Tests
{
    public class BasicTests
    {
        // Empty or blank names are rejected.
        [Test]
        public void Sanitize_BlankName_ReturnsEmpty()
        {
            Assert.AreEqual("", PlayerSession.Sanitize("   "));
            Assert.AreEqual("", PlayerSession.Sanitize(null));
        }

        // Extra spaces around the name are removed.
        [Test]
        public void Sanitize_TrimsSpaces()
        {
            Assert.AreEqual("Santi", PlayerSession.Sanitize("  Santi  "));
        }

        // Long names are cut to the max length.
        [Test]
        public void Sanitize_CutsLongNames()
        {
            var result = PlayerSession.Sanitize(new string('a', 50));
            Assert.AreEqual(PlayerSession.MaxNameLength, result.Length);
        }

        // Rocket stats add up mass, thrust and fuel, and thrust/weight uses the full mass.
        [Test]
        public void RocketStats_AddsPartsAndThrustToWeight()
        {
            var thruster = UnityEngine.ScriptableObject.CreateInstance<ThrusterData>();
            thruster.mass = 50f;
            thruster.thrust = 2000f;
            var tank = UnityEngine.ScriptableObject.CreateInstance<FuelTankData>();
            tank.mass = 50f;
            tank.fuelCapacity = 100f;

            var stats = new RocketStats();
            stats.Add(thruster);
            stats.Add(tank);

            Assert.AreEqual(2, stats.PartCount);
            Assert.AreEqual(100f, stats.DryMass);
            Assert.AreEqual(200f, stats.FullMass(1f));
            Assert.AreEqual(1f, stats.ThrustToWeight(stats.FullMass(1f), 10f), 0.0001f);
        }

        // Both scenes must be enabled in the build, or the web build has no game.
        [Test]
        public void BuildSettings_HasBothScenes()
        {
            var paths = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled)
                    paths.Add(scene.path);

            CollectionAssert.Contains(paths, "Assets/Scenes/" + SceneNames.MainMenu + ".unity");
            CollectionAssert.Contains(paths, "Assets/Scenes/" + SceneNames.Game + ".unity");
        }
    }
}
