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

        // A background is hidden below its start altitude, half shown halfway, and fully shown from its full altitude.
        [Test]
        public void Background_FadesBetweenStartAndFull()
        {
            Assert.AreEqual(0f, BackgroundManager.TargetAlpha(250f, 350f, 100f));
            Assert.AreEqual(0.5f, BackgroundManager.TargetAlpha(250f, 350f, 300f), 0.001f);
            Assert.AreEqual(1f, BackgroundManager.TargetAlpha(250f, 350f, 500f));
            Assert.AreEqual(1f, BackgroundManager.TargetAlpha(0f, 0f, 0f));
        }

        // Rocket stats add up mass, thrust and fuel, and thrust/weight uses the full mass.
        [Test]
        public void RocketStats_AddsPartsAndThrustToWeight()
        {
            var thruster = UnityEngine.ScriptableObject.CreateInstance<ThrusterData>();
            thruster.mass = 0.05f;
            thruster.thrust = 2f;
            thruster.fuelPerSecond = 0.05f;
            var tank = UnityEngine.ScriptableObject.CreateInstance<FuelTankData>();
            tank.mass = 0.05f;
            tank.fuelCapacity = 0.1f;

            var stats = new RocketStats();
            stats.Add(thruster);
            stats.Add(tank);

            Assert.AreEqual(2, stats.PartCount);
            Assert.AreEqual(0.1f, stats.DryMass, 0.0001f);
            Assert.AreEqual(0.2f, stats.FullMass, 0.0001f);
            Assert.AreEqual(2f, stats.BurnTime, 0.0001f);
            Assert.AreEqual(1f, stats.ThrustToWeight(stats.FullMass, 10f), 0.0001f);
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
