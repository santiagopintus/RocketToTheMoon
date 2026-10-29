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

        public static string PlayerName { get; private set; } = "";

        public static string LastUsedName => PlayerPrefs.GetString(NameKey, "");

        public static int Credits => PlayerPrefs.GetInt(CreditsKey, StartingCredits);

        public static event Action CreditsChanged;

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

        // Reads the saved list of unlocked part ids (saved as "id1,id2,").
        public static List<string> UnlockedIds()
        {
            return new List<string>(PlayerPrefs.GetString(UnlockedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries));
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
