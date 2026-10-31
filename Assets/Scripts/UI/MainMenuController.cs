using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    /// <summary>Controls the main menu. Play asks for a name, then loads the game. New Game erases the save first.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button newGameButton;
        [SerializeField] GameObject namePanel;
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] Button confirmButton;
        [SerializeField] Button backButton;

        string newGameText;
        bool askingNewGame;

        // Hides the name panel and connects the buttons.
        void Awake()
        {
            namePanel.SetActive(false);
            nameInput.characterLimit = PlayerSession.MaxNameLength;

            playButton.onClick.AddListener(OpenNamePanel);
            quitButton.onClick.AddListener(Quit);
            newGameText = newGameButton.GetComponentInChildren<TMP_Text>().text;
            newGameButton.onClick.AddListener(NewGame);
#if UNITY_WEBGL && !UNITY_EDITOR
            // A browser tab cannot be closed by the game.
            quitButton.gameObject.SetActive(false);
#endif
            confirmButton.onClick.AddListener(Confirm);
            backButton.onClick.AddListener(CloseNamePanel);
            nameInput.onValueChanged.AddListener(_ => RefreshConfirm());
            nameInput.onSubmit.AddListener(_ => Confirm());
        }

        // Closes the name panel when Escape is pressed.
        void Update()
        {
            if (namePanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseNamePanel();
        }

        // The first click asks the player to click again. The second click erases the save and asks for a name.
        void NewGame()
        {
            if (!askingNewGame)
            {
                SetAskingNewGame(true);
                return;
            }

            PlayerSession.NewGame();
            SetAskingNewGame(false);
            OpenNamePanel();
        }

        // Shows "click again" on the New Game button, or puts its normal text back.
        void SetAskingNewGame(bool asking)
        {
            askingNewGame = asking;
            newGameButton.GetComponentInChildren<TMP_Text>().text = asking ? "SURE? CLICK AGAIN" : newGameText;
        }

        // Shows the name panel with the last used name.
        void OpenNamePanel()
        {
            SetAskingNewGame(false);
            namePanel.SetActive(true);
            nameInput.text = PlayerSession.LastUsedName;
            RefreshConfirm();
            nameInput.Select();
            nameInput.ActivateInputField();
        }

        // Hides the name panel and goes back to the Play button.
        void CloseNamePanel()
        {
            namePanel.SetActive(false);
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        // Turns the confirm button on only when the name is not empty.
        void RefreshConfirm()
        {
            confirmButton.interactable = PlayerSession.Sanitize(nameInput.text).Length > 0;
        }

        // Saves the name and loads the game scene.
        void Confirm()
        {
            if (PlayerSession.TrySetName(nameInput.text))
                SceneManager.LoadScene(SceneNames.Game);
        }

        // Closes the game (or stops play mode in the editor).
        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
