using System;
using System.Collections.Generic;

namespace RocketToTheMoon.Core
{
    // A place where rocket blueprints are kept. It only says WHAT can be done, not HOW.
    // Subclasses decide where the blueprints live: LocalBlueprintStorage (this browser) now, a cloud one later.
    public abstract class BlueprintStorage
    {
        public const int MaxSlots = 5;

        // The names of all saved blueprints, oldest first.
        public abstract List<string> Names();

        // Saves a design under a name. Overwrites a blueprint with the same name.
        public abstract void Save(string name, RocketDesign design);

        // Reads the design saved under a name. Empty if there is none.
        public abstract RocketDesign Load(string name);

        // Removes the blueprint with this name.
        public abstract void Delete(string name);

        // When the blueprint with this name was saved.
        public abstract DateTime SavedAt(string name);

        // Returns true if a blueprint with this name is saved.
        public bool Exists(string name)
        {
            return Names().Contains(name);
        }

        // Returns true if a design can be saved under this name: it replaces an old one or a slot is free.
        public bool CanSave(string name)
        {
            return Exists(name) || Names().Count < MaxSlots;
        }
    }
}
