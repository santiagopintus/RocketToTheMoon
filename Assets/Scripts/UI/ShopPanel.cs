using System;
using System.Collections.Generic;
using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // The parts store on the right third of the screen. Has one tab per part type and slides in and out.
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] PartData[] parts;
        [SerializeField] Button toggleButton;
        [SerializeField] Transform tabBar;
        [SerializeField] Button tabPrefab;
        [SerializeField] Transform cardList;
        [SerializeField] ShopCard cardPrefab;
        [SerializeField] CameraSideShift cameraShift;
        [SerializeField] RocketBuilder builder;
        [SerializeField] float slideSpeed = 8f;

        readonly List<Button> tabs = new List<Button>();
        RectTransform rect;
        bool isOpen;

        // Builds the tabs, shows the first one and opens the store.
        void Awake()
        {
            rect = (RectTransform)transform;
            toggleButton.onClick.AddListener(() => SetOpen(!isOpen));

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
                AddTab(type);

            ShowTab(PartType.Body);
            SetOpen(true);
        }

        // Starts listening for launches.
        void OnEnable()
        {
            GameState.FlightStarted += Close;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.FlightStarted -= Close;
        }

        // Slides the panel a bit closer to open (on screen) or closed (off the right edge).
        void Update()
        {
            Vector2 position = rect.anchoredPosition;
            float goal = isOpen ? 0f : rect.rect.width;
            position.x = Mathf.Lerp(position.x, goal, slideSpeed * Time.deltaTime);
            rect.anchoredPosition = position;
        }

        // Opens or closes the store and moves the camera so the rocket stays in the free space.
        public void SetOpen(bool open)
        {
            isOpen = open;
            if (cameraShift != null) cameraShift.SetShifted(open);
        }

        // Closes the store. Called when the rocket launches.
        void Close()
        {
            SetOpen(false);
        }

        // Creates a tab button for one part type.
        void AddTab(PartType type)
        {
            Button tab = Instantiate(tabPrefab, tabBar);
            tab.GetComponentInChildren<TMP_Text>().text = TabName(type);
            tab.onClick.AddListener(() => ShowTab(type));
            tabs.Add(tab);
        }

        // Highlights the chosen tab and shows a card for every part of that type.
        void ShowTab(PartType type)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                bool selected = i == (int)type;
                tabs[i].image.color = selected ? Palette.Teal : Palette.DeepNavy;
                tabs[i].GetComponentInChildren<TMP_Text>().color = selected ? Palette.Moonlight : Palette.LightTeal;
            }

            foreach (Transform card in cardList)
                Destroy(card.gameObject);

            foreach (PartData part in parts)
                if (part.Type == type)
                    Instantiate(cardPrefab, cardList).Show(part, builder);
        }

        // The text on a tab. Fuel tanks get a shorter name so it fits.
        static string TabName(PartType type)
        {
            return type == PartType.FuelTank ? "TANK" : type.ToString().ToUpper();
        }
    }
}
