using System;
using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    /// <summary>Keeps the player name, credits and unlocked parts between scenes. Saves them for next time.</summary>
    public static class PlayerSession
    {
        public const int MaxNameLength = 16;
        public const int StartingCredits = 350;
        const string NameKey = "PlayerName";
        const string CreditsKey = "Credits";
        const string UnlockedKey = "UnlockedParts";
        const string BestAltitudeKey = "BestAltitude";
        const string SeenKey = "SeenParts";

        public static string PlayerName { get; private set; } = "";

        public static string LastUsedName => PlayerPrefs.GetString(NameKey, "");

        public static int Credits => PlayerPrefs.GetInt(CreditsKey, StartingCredits);

        // The highest altitude ever reached, in metres. New parts appear in the store as it grows.
        public static float BestAltitude => PlayerPrefs.GetFloat(BestAltitudeKey, 0f);

        public static event Action CreditsChanged;

        // Saves the altitude if it beats the best one.
        public static void ReportAltitude(float altitude)
        {
            if (altitude <= BestAltitude) return;
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
            return part.unlockAltitude > 0f && IsAvailable(part) && !IsUnlocked(part.id) && !ReadIds(SeenKey).Contains(part.id);
        }

        // Remembers that the player saw this part in the store.
        public static void MarkSeen(string partId)
        {
            if (ReadIds(SeenKey).Contains(partId)) return;
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

        // Returns true if the player already bought this part.
        public static bool IsUnlocked(string partId)
        {
            return UnlockedIds().Contains(partId);
        }

        // Pays for a part and unlocks it. Returns false if it is already owned or too expensive.
        public static bool TryBuy(string partId, int price)
        {
            if (IsUnlocked(partId) || Credits < price)
                return false;

            PlayerPrefs.SetString(UnlockedKey, PlayerPrefs.GetString(UnlockedKey, "") + partId + ",");
            AddCredits(-price);
            return true;
        }

        // Gives a part back for the same price it was bought for. Returns false if the player does not own it.
        public static bool TrySell(string partId, int price)
        {
            List<string> ids = UnlockedIds();
            if (!ids.Remove(partId))
                return false;

            PlayerPrefs.SetString(UnlockedKey, ids.Count == 0 ? "" : string.Join(",", ids) + ",");
            AddCredits(price);
            return true;
        }

        // Reads the saved list of unlocked part ids.
        public static List<string> UnlockedIds()
        {
            return ReadIds(UnlockedKey);
        }

        // Reads a saved list of part ids (saved as "id1,id2,").
        static List<string> ReadIds(string key)
        {
            return new List<string>(PlayerPrefs.GetString(key, "").Split(',', StringSplitOptions.RemoveEmptyEntries));
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
