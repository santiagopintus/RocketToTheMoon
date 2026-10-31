using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Base data shared by every rocket part.
    public abstract class PartData : ScriptableObject
    {
        public string id;
        public string partName;
        public Sprite icon;
        public GameObject prefab;
        public int price;

        // Better parts have a higher tier. The part shows up in the store once the best altitude reaches unlockAltitude.
        public int tier = 1;
        public float unlockAltitude;

        // Physics, in real units. Mass in kg. Drag area (m^2) slows the rocket down.
        // Side area (m^2) is how hard the air pushes the part sideways when the rocket flies at an angle.
        public float mass = 0.02f;
        public float dragArea = 0.0002f;
        public float sideArea;

        // The kind of part this is. Each part class sets it.
        public abstract PartType Type { get; }

        // Returns true if a part of this type can attach to the given side. By default nothing can.
        public virtual bool Accepts(SnapSide side, PartType type)
        {
            return false;
        }

        // Shared rule for stackable parts (body, tank): noses and tanks on top, thrusters and tanks below, fins on the sides.
        protected static bool StackAccepts(SnapSide side, PartType type)
        {
            if (side == SnapSide.Top)
                return type == PartType.Nose || type == PartType.FuelTank;
            if (side == SnapSide.Bottom)
                return type == PartType.Thruster || type == PartType.FuelTank;
            return type == PartType.Fin;
        }
    }
}
