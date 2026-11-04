using System;
using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    /// <summary>Keeps the player name, credits, best altitude and best tier between scenes. Saves them for next time.</summary>
    public static class PlayerSession
    {
        public const int MaxNameLength = 16;
        public const int StartingCredits = 350;
        const string NameKey = "PlayerName";
        const string CreditsKey = "Credits";
        const string UnlockedKey = "UnlockedParts";
        const string BestTierKey = "BestTier";
        const string BestAltitudeKey = "BestAltitude";
        const string SeenKey = "SeenParts";
        const string RocketKey = "LastRocket";

        public static string PlayerName { get; private set; } = "";

        public static string LastUsedName => PlayerPrefs.GetString(NameKey, "");

        public static int Credits => PlayerPrefs.GetInt(CreditsKey, StartingCredits);

        // The highest altitude ever reached, in metres. New parts appear in the store as it grows.
        public static float BestAltitude => PlayerPrefs.GetFloat(BestAltitudeKey, 0f);

        // The highest tier of any part ever bought. The assembling base grows with it.
        public static int BestTier => PlayerPrefs.GetInt(BestTierKey, 1);

        public static event Action CreditsChanged;

        public static event Action TierChanged;

        // Saves the altitude if it beats the best one.
        public static void ReportAltitude(float altitude)
        {
            if (altitude <= BestAltitude)
                return;
            PlayerPrefs.SetFloat(BestAltitudeKey, altitude);
            PlayerPrefs.Save();
        }

        // Returns true once the best altitude is high enough for the part to show up in the store.
        public static bool IsAvailable(PartData part)
        {
            return BestAltitude >= part.unlockAltitude;
        }

        // Returns true if the part showed up in the store and the player has not looked at it yet.
        // Parts that are there from the start (unlock at 0 m) are never new.
        public static bool IsNew(PartData part)
        {
            return part.unlockAltitude > 0f
                && IsAvailable(part)
                && !ReadIds(SeenKey).Contains(part.id);
        }

        // Remembers that the player saw this part in the store.
        public static void MarkSeen(string partId)
        {
            if (ReadIds(SeenKey).Contains(partId))
                return;
            PlayerPrefs.SetString(SeenKey, PlayerPrefs.GetString(SeenKey, "") + partId + ",");
            PlayerPrefs.Save();
        }

        // Adds credits, saves them and tells everyone about it.
        public static void AddCredits(int amount)
        {
            PlayerPrefs.SetInt(CreditsKey, Credits + amount);
            PlayerPrefs.Save();
            CreditsChanged?.Invoke();
        }

        // Pays a price. Returns false (and pays nothing) if the player cannot afford it.
        public static bool TrySpend(int price)
        {
            if (Credits < price)
                return false;
            AddCredits(-price);
            return true;
        }

        // Remembers the tier of a part the player bought, if it is the highest one so far, and tells everyone about it.
        public static void ReportTier(int tier)
        {
            if (tier <= BestTier)
                return;
            PlayerPrefs.SetInt(BestTierKey, tier);
            PlayerPrefs.Save();
            TierChanged?.Invoke();
        }

        // Saves the rocket the player built last.
        public static void SaveRocket(RocketDesign design)
        {
            PlayerPrefs.SetString(RocketKey, design.ToJson());
            PlayerPrefs.Save();
        }

        // Reads the rocket the player built last. Empty if there is none.
        public static RocketDesign LoadRocket()
        {
            return RocketDesign.FromJson(PlayerPrefs.GetString(RocketKey, ""));
        }

        // Erases credits, old bought parts, best altitude, seen parts, best tier and the saved rocket. The name is kept.
        public static void NewGame()
        {
            PlayerPrefs.DeleteKey(CreditsKey);
            PlayerPrefs.DeleteKey(UnlockedKey);
            PlayerPrefs.DeleteKey(BestAltitudeKey);
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.DeleteKey(BestTierKey);
            PlayerPrefs.DeleteKey(RocketKey);
            PlayerPrefs.Save();
            CreditsChanged?.Invoke();
        }

        // Reads a saved list of part ids (saved as "id1,id2,").
        static List<string> ReadIds(string key)
        {
            return new List<string>(
                PlayerPrefs.GetString(key, "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            );
        }

        // Saves the name. Returns false if the name is empty.
        public static bool TrySetName(string rawName)
        {
            var name = Sanitize(rawName);
            if (name.Length == 0)
                return false;

            PlayerName = name;
            PlayerPrefs.SetString(NameKey, name);
            PlayerPrefs.Save();
            return true;
        }

        // Removes extra spaces and cuts the name if it is too long.
        public static string Sanitize(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return "";

            var name = rawName.Trim();
            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
        }
    }
}
