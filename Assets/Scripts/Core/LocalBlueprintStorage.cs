using System;
using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Keeps blueprints in PlayerPrefs, so they stay in this browser (or on this computer).
    public class LocalBlueprintStorage : BlueprintStorage
    {
        const string NamesKey = "BlueprintNames";
        const string DesignKey = "Blueprint_";
        const string TimeLowKey = "BlueprintTimeLow_";
        const string TimeHighKey = "BlueprintTimeHigh_";

        // Reads the saved names
        public override List<string> Names()
        {
            string saved = PlayerPrefs.GetString(NamesKey, "");
            return new List<string>(saved.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        // Saves the design as JSON and the time as two ints, and adds the name to the list if it is new.
        public override void Save(string name, RocketDesign design)
        {
            if (!Exists(name))
                PlayerPrefs.SetString(NamesKey, PlayerPrefs.GetString(NamesKey, "") + name + ",");

            PlayerPrefs.SetString(DesignKey + name, design.ToJson());

            // PlayerPrefs cannot save a long, so the union splits the time into two ints.
            var time = new TimeBits { ticks = DateTime.Now.Ticks };
            PlayerPrefs.SetInt(TimeLowKey + name, time.low);
            PlayerPrefs.SetInt(TimeHighKey + name, time.high);
            PlayerPrefs.Save();
        }

        // Reads the design JSON saved under the name.
        public override RocketDesign Load(string name)
        {
            return RocketDesign.FromJson(PlayerPrefs.GetString(DesignKey + name, ""));
        }

        // Removes the name from the list and deletes its design and time.
        public override void Delete(string name)
        {
            List<string> names = Names();
            names.Remove(name);

            string saved = "";
            foreach (string n in names)
                saved += n + ",";

            PlayerPrefs.SetString(NamesKey, saved);
            PlayerPrefs.DeleteKey(DesignKey + name);
            PlayerPrefs.DeleteKey(TimeLowKey + name);
            PlayerPrefs.DeleteKey(TimeHighKey + name);
            PlayerPrefs.Save();
        }

        // Joins the two saved ints back into the time with the union.
        public override DateTime SavedAt(string name)
        {
            var time = new TimeBits
            {
                low = PlayerPrefs.GetInt(TimeLowKey + name, 0),
                high = PlayerPrefs.GetInt(TimeHighKey + name, 0)
            };
            return new DateTime(time.ticks);
        }
    }
}
