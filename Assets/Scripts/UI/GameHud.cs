using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    /// <summary>Shows the player name, credits and a menu button in the game scene.</summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] TMP_Text playerNameLabel;
        [SerializeField] TMP_Text creditsLabel;
        [SerializeField] Button menuButton;

        // Shows the player name and connects the menu button.
        void Awake()
        {
            // If we start from this scene in the editor, use the saved name.
            var name = PlayerSession.PlayerName;
            if (name.Length == 0)
                name = PlayerSession.LastUsedName.Length > 0 ? PlayerSession.LastUsedName : "Engineer";

            playerNameLabel.text = name;
            menuButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.MainMenu));
        }

        // Starts listening for credit changes and shows the current credits.
        void OnEnable()
        {
            PlayerSession.CreditsChanged += ShowCredits;
            ShowCredits();
        }

        // Stops listening.
        void OnDisable()
        {
            PlayerSession.CreditsChanged -= ShowCredits;
        }

        // Writes the player's credits.
        void ShowCredits()
        {
            if (creditsLabel != null) creditsLabel.text = PlayerSession.Credits + " credits";
        }
    }
}
