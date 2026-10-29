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
        public float mass = 1f;

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
            if (side == SnapSide.Top) return type == PartType.Nose || type == PartType.FuelTank;
            if (side == SnapSide.Bottom) return type == PartType.Thruster || type == PartType.FuelTank;
            return type == PartType.Fin;
        }
    }
}
