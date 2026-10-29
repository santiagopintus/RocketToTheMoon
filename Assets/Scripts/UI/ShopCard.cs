using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // One part in the store: picture, name, stats, price and a Buy button. Owned parts can be dragged onto the rocket or sold back at the same price.
    public class ShopCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text statsLabel;
        [SerializeField] TMP_Text priceLabel;
        [SerializeField] Button buyButton;

        PartData part;
        RocketBuilder builder;
        TMP_Text buttonLabel;
        Color nameColor;

        // Connects the Buy button and remembers the name colour, so it can come back after greying out.
        void Awake()
        {
            buttonLabel = buyButton.GetComponentInChildren<TMP_Text>();
            nameColor = nameLabel.color;
            buyButton.onClick.AddListener(BuyOrSell);
        }

        // Starts listening for credit changes, since they change what the player can afford.
        void OnEnable()
        {
            PlayerSession.CreditsChanged += Refresh;
        }

        // Stops listening.
        void OnDisable()
        {
            PlayerSession.CreditsChanged -= Refresh;
        }

        // Fills the card with a part's data and remembers the builder that dragged parts go to.
        public void Show(PartData data, RocketBuilder rocketBuilder)
        {
            part = data;
            builder = rocketBuilder;
            builder.Changed += Refresh;
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
            nameLabel.text = data.partName;
            statsLabel.text = StatsText(data);
            Refresh();
        }

        // Stops listening to the builder.
        void OnDestroy()
        {
            if (builder == null) return;
            builder.Changed -= Refresh;
            if (builder.HoveredCard == part) builder.HoveredCard = null;
        }

        // Tells the builder the cursor is over this card, so it can show the open hand.
        public void OnPointerEnter(PointerEventData eventData)
        {
            builder.HoveredCard = part;
        }

        // Tells the builder the cursor left this card.
        public void OnPointerExit(PointerEventData eventData)
        {
            if (builder.HoveredCard == part) builder.HoveredCard = null;
        }

        // Sells the part if the player owns it, otherwise buys it. The credits change and every card refreshes.
        void BuyOrSell()
        {
            if (PlayerSession.IsUnlocked(part.id)) PlayerSession.TrySell(part.id, part.price);
            else PlayerSession.TryBuy(part.id, part.price);
        }

        // Starts dragging a copy of the part, if the player owns it. The part appears at the same size as the card's picture.
        public void OnBeginDrag(PointerEventData eventData)
        {
            float iconHeight = icon.rectTransform.rect.height * icon.canvas.scaleFactor;
            if (PlayerSession.IsUnlocked(part.id)) builder.StartDrag(part, iconHeight);
        }

        // Does nothing: the builder moves the part every frame. Unity only sends begin and end drag when this exists.
        public void OnDrag(PointerEventData eventData)
        {
        }

        // Drops the part: it stays if it is on a snap point.
        public void OnEndDrag(PointerEventData eventData)
        {
            builder.EndDrag();
        }

        // Shows the price and Buy button, or "OWNED" and a Sell button. A part on the rocket cannot be sold.
        // Parts that need a body are greyed out until one is on the pad, and owned ones say so in small text.
        void Refresh()
        {
            if (part == null) return;

            bool owned = PlayerSession.IsUnlocked(part.id);
            bool canAfford = PlayerSession.Credits >= part.price;
            bool blocked = !builder.CanAdd(part);

            icon.color = blocked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            nameLabel.color = blocked ? Palette.Teal : nameColor;

            priceLabel.text = owned ? (blocked ? "<size=70%>BODY FIRST</size>" : "OWNED") : part.price + " cr";
            priceLabel.color = owned ? Palette.LightTeal : canAfford ? Palette.Moonlight : Palette.Teal;
            buttonLabel.text = owned ? "SELL" : "BUY";
            buyButton.interactable = owned ? !builder.IsUsed(part) : canAfford;
        }

        // Writes the numbers that matter for this kind of part.
        static string StatsText(PartData data)
        {
            string text = "Mass " + data.mass + " kg";
            if (data is ThrusterData thruster)
                text += "\nThrust " + thruster.thrust + " N\nBurn " + thruster.fuelPerSecond + " fuel/s";
            if (data is FuelTankData tank)
                text += "\nFuel " + tank.fuelCapacity;
            return text;
        }
    }
}
