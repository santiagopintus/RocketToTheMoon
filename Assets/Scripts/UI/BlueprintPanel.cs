using System;
using System.Collections.Generic;
using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // The blueprints window: saves the rocket on the pad under a name, and loads or deletes saved ones.
    // Loading sells the current rocket back and buys every part of the blueprint.
    public class BlueprintPanel : MonoBehaviour
    {
        [SerializeField]
        Button openButton;

        [SerializeField]
        GameObject panel;

        [SerializeField]
        TMP_InputField nameInput;

        [SerializeField]
        Button saveButton;

        [SerializeField]
        Button closeButton;

        [SerializeField]
        TMP_Text messageLabel;

        // One row per slot. Each has the children "Name", "Info", "LoadButton" and "DeleteButton".
        [SerializeField]
        Transform[] rows;

        [SerializeField]
        RocketBuilder builder;

        [SerializeField]
        ShopPanel shop;

        [SerializeField]
        FlightSimulator simulator;

        // The game only knows it is "some storage". Swapping in a cloud storage later changes only this line.
        readonly BlueprintStorage storage = new LocalBlueprintStorage();

        List<string> names = new List<string>();

        // Hides the window and connects all the buttons.
        void Awake()
        {
            panel.SetActive(false);
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            saveButton.onClick.AddListener(SaveRocket);

            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                Row(i, "LoadButton").onClick.AddListener(() => LoadBlueprint(index));
                Row(i, "DeleteButton").onClick.AddListener(() => DeleteBlueprint(index));
            }
        }

        // Hides the Blueprints button (and closes the window) while a rocket is flying or waiting for Continue.
        void Update()
        {
            bool onPad = !simulator.HasFlight;
            openButton.gameObject.SetActive(onPad);
            if (!onPad && panel.activeSelf)
                Close();
        }

        // Clears the name and message, fills in the rows and shows the window.
        void Open()
        {
            nameInput.text = "";
            messageLabel.text = "";
            ShowRows();
            panel.SetActive(true);
        }

        // Hides the window.
        void Close()
        {
            panel.SetActive(false);
        }

        // Saves the rocket on the pad under the typed name.
        void SaveRocket()
        {
            string name = PlayerSession.Sanitize(nameInput.text).Replace(",", "");
            RocketDesign design = builder.ToDesign();

            if (name.Length == 0)
                messageLabel.text = "Type a name first";
            else if (design.parts.Count == 0)
                messageLabel.text = "Build a rocket first";
            else if (!storage.CanSave(name))
                messageLabel.text = "All " + BlueprintStorage.MaxSlots + " slots are full. Delete one first";
            else
            {
                storage.Save(name, design);
                messageLabel.text = "Saved " + name;
                nameInput.text = "";
                ShowRows();
            }
        }

        // Sells the current rocket, buys the blueprint's parts and builds it on the pad.
        void LoadBlueprint(int index)
        {
            BlueprintInfo info = ReadInfo(names[index]);
            int money = PlayerSession.Credits + builder.RocketValue();

            if (!info.allUnlocked)
            {
                messageLabel.text = "Some parts are still locked";
                return;
            }
            if (money < info.price)
            {
                messageLabel.text = "Not enough credits (" + info.price + " needed)";
                return;
            }

            builder.Clear();
            PlayerSession.TrySpend(info.price);
            builder.Load(storage.Load(info.name), shop.Parts);
            PlayerSession.SaveRocket(builder.ToDesign());
            Close();
        }

        // Deletes a blueprint and updates the rows.
        void DeleteBlueprint(int index)
        {
            messageLabel.text = "Deleted " + names[index];
            storage.Delete(names[index]);
            ShowRows();
        }

        // Fills every row: a saved blueprint with its details, or an empty slot.
        void ShowRows()
        {
            names = storage.Names();
            int money = PlayerSession.Credits + builder.RocketValue();

            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < names.Count;
                TMP_Text nameLabel = rows[i].Find("Name").GetComponent<TMP_Text>();
                TMP_Text infoLabel = rows[i].Find("Info").GetComponent<TMP_Text>();
                Button load = Row(i, "LoadButton");
                load.gameObject.SetActive(used);
                Row(i, "DeleteButton").gameObject.SetActive(used);

                if (!used)
                {
                    nameLabel.text = "Empty slot";
                    infoLabel.text = "";
                    continue;
                }

                BlueprintInfo info = ReadInfo(names[i]);
                nameLabel.text = info.name;
                infoLabel.text = info.partCount + " parts, " + info.price + " cr, " + Ago(info.savedAt);
                if (!info.allUnlocked)
                    infoLabel.text = info.partCount + " parts, LOCKED";
                load.interactable = info.allUnlocked && money >= info.price;
            }
        }

        // Adds up the parts and price of a blueprint, and checks that every part is unlocked.
        BlueprintInfo ReadInfo(string name)
        {
            RocketDesign design = storage.Load(name);
            var info = new BlueprintInfo
            {
                name = name,
                partCount = design.parts.Count,
                allUnlocked = true,
                savedAt = storage.SavedAt(name)
            };

            float price = 0f;
            foreach (DesignPart saved in design.parts)
            {
                PartData data = Array.Find(shop.Parts, p => p.id == saved.partId);
                if (data == null)
                {
                    info.allUnlocked = false;
                    continue;
                }

                // A fin's price buys the pair, and a pair is saved as 2 parts.
                price += data.Type == PartType.Fin ? data.price / 2f : data.price;
                if (!PlayerSession.IsAvailable(data))
                    info.allUnlocked = false;
            }

            info.price = Mathf.RoundToInt(price);
            return info;
        }

        // How long ago a time was, in simple words.
        static string Ago(DateTime time)
        {
            TimeSpan span = DateTime.Now - time;
            if (span.TotalMinutes < 1)
                return "just now";
            if (span.TotalHours < 1)
                return (int)span.TotalMinutes + " min ago";
            if (span.TotalDays < 1)
                return (int)span.TotalHours + " h ago";
            return (int)span.TotalDays + " days ago";
        }

        // Finds a button by name in one of the rows.
        Button Row(int index, string buttonName)
        {
            return rows[index].Find(buttonName).GetComponent<Button>();
        }
    }
}
