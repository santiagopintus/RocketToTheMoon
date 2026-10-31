using System;
using System.Collections.Generic;
using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // The parts store on the right third of the screen. Has one tab per part type and slides in and out.
    // A dot on the STORE handle and on a tab means a part there just unlocked. It goes away once that tab is seen.
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] PartData[] parts;
        [SerializeField] Button toggleButton;
        [SerializeField] GameObject storeDot;
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
        PartType shownTab;

        // Sorts the parts by unlock altitude (then price), builds the tabs, shows the first one and opens the store.
        void Awake()
        {
            rect = (RectTransform)transform;
            Array.Sort(parts, (a, b) => a.unlockAltitude != b.unlockAltitude ? a.unlockAltitude.CompareTo(b.unlockAltitude) : a.price.CompareTo(b.price));
            toggleButton.onClick.AddListener(() => SetOpen(!isOpen));

            foreach (PartType type in Enum.GetValues(typeof(PartType)))
                AddTab(type);

            ShowTab(PartType.Body);
            SetOpen(true);
        }

        // Builds the rocket the player made last time, now that the cards are listening for changes.
        void Start()
        {
            builder.Load(PlayerSession.LoadRocket(), parts);
        }

        // Starts listening for launches, and for credit changes, which come right after a flight may have unlocked parts.
        void OnEnable()
        {
            GameState.FlightStarted += Close;
            PlayerSession.CreditsChanged += MarkShownSeen;
        }

        // Stops listening.
        void OnDisable()
        {
            GameState.FlightStarted -= Close;
            PlayerSession.CreditsChanged -= MarkShownSeen;
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
            MarkShownSeen();
        }

        // If the store is open, remembers that the player saw the unlocked parts on the shown tab. Then updates the dots.
        void MarkShownSeen()
        {
            if (isOpen)
                foreach (PartData part in parts)
                    if (part.Type == shownTab && PlayerSession.IsAvailable(part)) PlayerSession.MarkSeen(part.id);

            bool anyNew = false;
            for (int i = 0; i < tabs.Count; i++)
            {
                bool tabNew = HasNewPart((PartType)i);
                tabs[i].transform.Find("NewDot").gameObject.SetActive(tabNew);
                anyNew |= tabNew;
            }
            if (storeDot != null) storeDot.SetActive(anyNew);
        }

        // Returns true if a part of this type is new in the store.
        bool HasNewPart(PartType type)
        {
            foreach (PartData part in parts)
                if (part.Type == type && PlayerSession.IsNew(part)) return true;
            return false;
        }

        // Closes the store. Called when the rocket launches.
        void Close()
        {
            SetOpen(false);
        }

        // The first part that unlocks above an altitude, or null if every part unlocks below it.
        public PartData NextUnlock(float altitude)
        {
            foreach (PartData part in parts)
                if (part.unlockAltitude > altitude) return part;
            return null;
        }

        // Creates a tab button for one part type.
        void AddTab(PartType type)
        {
            Button tab = Instantiate(tabPrefab, tabBar);
            tab.GetComponentInChildren<TMP_Text>().text = TabName(type);
            tab.onClick.AddListener(() => ShowTab(type));
            tabs.Add(tab);
        }

        // Highlights the chosen tab, clears its new dot and shows a card for every part of that type.
        void ShowTab(PartType type)
        {
            shownTab = type;
            MarkShownSeen();

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
