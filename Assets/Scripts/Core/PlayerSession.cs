using UnityEngine;

namespace RocketToTheMoon.Core
{
    /// <summary>Keeps the player name between scenes. Saves the last name for next time.</summary>
    public static class PlayerSession
    {
        public const int MaxNameLength = 16;
        const string NameKey = "PlayerName";

        public static string PlayerName { get; private set; } = "";

        public static string LastUsedName => PlayerPrefs.GetString(NameKey, "");

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
