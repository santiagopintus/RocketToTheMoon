using System;
using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // A rocket saved as plain data: which parts it has and where each one is attached. Saved as small JSON.
    [Serializable]
    public class RocketDesign
    {
        public List<DesignPart> parts = new List<DesignPart>();

        // Turns the design into JSON text.
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        // Reads a design from JSON text. Returns an empty design if the text is empty or broken.
        public static RocketDesign FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new RocketDesign();
            try { return JsonUtility.FromJson<RocketDesign>(json) ?? new RocketDesign(); }
            catch (ArgumentException) { return new RocketDesign(); }
        }
    }

    // One part of a saved rocket. Parents always come before their children in the list.
    [Serializable]
    public class DesignPart
    {
        public string partId;

        // Index of the part this one is attached to, or -1 for the body on the pad.
        public int parent = -1;

        // The side of the parent it is attached to, and for a fin its height step (-1 for other parts).
        public SnapSide side;
        public int step = -1;
    }
}
