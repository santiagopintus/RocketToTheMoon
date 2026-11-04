using System.Collections;
using RocketToTheMoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // One part in the store: picture, name, stats and price. Dragging it onto the rocket buys it; dragging it off the rocket sells it back.
    // A card that cannot be dragged is greyed out and shows a short message over itself when the player presses it.
    public class ShopCard
        : MonoBehaviour,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerDownHandler
    {
        [SerializeField]
        Image icon;

        [SerializeField]
        TMP_Text nameLabel;

        [SerializeField]
        TMP_Text statsLabel;

        [SerializeField]
        TMP_Text priceLabel;

        [SerializeField]
        CanvasGroup lockedOverlay;

        [SerializeField]
        TMP_Text lockedLabel;

        [SerializeField]
        float lockedShowTime = 1f;

        [SerializeField]
        float lockedFadeTime = 0.3f;

        PartData part;
        RocketBuilder builder;
        Color nameColor;

        // Remembers the name colour (so it can come back after greying out) and hides the locked message.
        void Awake()
        {
            nameColor = nameLabel.color;
            if (lockedOverlay != null)
                lockedOverlay.alpha = 0f;
        }

        // Starts listening for credit changes, since they change what the player can afford.
        void OnEnable()
        {
            PlayerSession.CreditsChanged += Refresh;
        }

        void OnDisable()
        {
            PlayerSession.CreditsChanged -= Refresh;
        }

        // Fills the card with a part's data (and a NEW tag if it just unlocked) and remembers the builder that dragged parts go to.
        public void Show(PartData data, RocketBuilder rocketBuilder, bool isNew)
        {
            part = data;
            builder = rocketBuilder;
            builder.Changed += Refresh;
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
            nameLabel.text = data.partName + " <size=70%>T" + data.tier + "</size>";
            if (isNew)
                nameLabel.text += "  <size=70%><color=#F2DCB3>NEW</color></size>";
            statsLabel.text = StatsText(data);
            Refresh();
        }

        // Stops listening to the builder.
        void OnDestroy()
        {
            if (builder == null)
                return;
            builder.Changed -= Refresh;
            if (builder.HoveredCard == part)
                builder.HoveredCard = null;
        }

        // Tells the builder the cursor is over this card, so it can show the open hand.
        public void OnPointerEnter(PointerEventData eventData)
        {
            builder.HoveredCard = part;
        }

        // Tells the builder the cursor left this card.
        public void OnPointerExit(PointerEventData eventData)
        {
            if (builder.HoveredCard == part)
                builder.HoveredCard = null;
        }

        // Starts dragging a copy of the part (the builder checks it can be taken). The part appears at the same size as the card's picture.
        public void OnBeginDrag(PointerEventData eventData)
        {
            float iconHeight = icon.rectTransform.rect.height * icon.canvas.scaleFactor;
            builder.StartDrag(part, iconHeight);
        }

        // Blinks the part on the rocket if it is placed, and shows why the part cannot be dragged, if it cannot.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (builder.IsUsed(part))
                builder.Highlight(part);

            string reason = LockedReason();
            if (reason == null || lockedOverlay == null)
                return;

            lockedLabel.text = reason;
            StopAllCoroutines();
            StartCoroutine(FlashLocked());
        }

        // The message for a part that cannot be dragged now, or null if it can.
        string LockedReason()
        {
            if (!PlayerSession.IsAvailable(part))
                return "UNLOCK AT " + part.unlockAltitude.ToString("0") + " m";
            if (!builder.CanAdd(part))
                return "PLACE A BODY FIRST";
            if (builder.AtLimit(part))
                return LimitText();
            if (PlayerSession.Credits < part.price)
                return "NOT ENOUGH CREDITS";
            if (!builder.HasFreeSpot(part))
                return "NO FREE SPOT";
            return null;
        }

        // How many parts of this type a rocket can have, e.g. "MAX 2 PER ROCKET". Fins count in pairs.
        string LimitText()
        {
            int limit = builder.Limit(part.Type);
            if (part.Type == PartType.Fin)
                return "MAX " + limit / 2 + " PAIR PER ROCKET";
            return "MAX " + limit + " PER ROCKET";
        }

        // Shows the locked message for a moment, then fades it out.
        IEnumerator FlashLocked()
        {
            lockedOverlay.alpha = 1f;
            yield return new WaitForSeconds(lockedShowTime);
            for (float time = 0f; time < lockedFadeTime; time += Time.deltaTime)
            {
                lockedOverlay.alpha = 1f - time / lockedFadeTime;
                yield return null;
            }
            lockedOverlay.alpha = 0f;
        }

        // Does nothing: the builder moves the part every frame. Unity only sends begin and end drag when this exists.
        public void OnDrag(PointerEventData eventData) { }

        // Drops the part: it stays if it is on a snap point.
        public void OnEndDrag(PointerEventData eventData)
        {
            builder.EndDrag();
        }

        // Shows the price, or why the part cannot be dragged now. A card that cannot be dragged is greyed out.
        // A part on the rocket says "ON ROCKET" once no more of its type fit.
        void Refresh()
        {
            if (part == null)
                return;

            bool available = PlayerSession.IsAvailable(part);
            bool needsBody = !builder.CanAdd(part);
            bool atLimit = !needsBody && builder.AtLimit(part);
            bool noSpot = !needsBody && !atLimit && !builder.HasFreeSpot(part);
            bool canTake = builder.CanTake(part);

            icon.color = canTake ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            nameLabel.color = canTake ? nameColor : Palette.Teal;

            if (!available)
                priceLabel.text =
                    "<size=70%>REACH " + part.unlockAltitude.ToString("0") + " m</size>";
            else if (atLimit && builder.IsUsed(part))
                priceLabel.text = "<size=70%>ON ROCKET</size>";
            else if (atLimit)
                priceLabel.text = "<size=70%>LIMIT REACHED</size>";
            else if (needsBody)
                priceLabel.text = "<size=70%>BODY FIRST</size>";
            else if (noSpot)
                priceLabel.text = "<size=70%>NO FREE SPOT</size>";
            else
                priceLabel.text = part.price + " cr";

            priceLabel.color = canTake ? Palette.Moonlight : Palette.Teal;
        }

        // Writes the numbers that matter for this kind of part. Masses in grams.
        static string StatsText(PartData data)
        {
            string text = "Mass " + Grams(data.mass);
            if (data is ThrusterData thruster)
                text +=
                    "\nThrust "
                    + thruster.thrust.ToString(
                        "0.#",
                        System.Globalization.CultureInfo.InvariantCulture
                    )
                    + " N\nBurn "
                    + Grams(thruster.fuelPerSecond)
                    + "/s";
            if (data is FuelTankData tank)
                text += "\nFuel " + Grams(tank.fuelCapacity);
            return text;
        }

        // A mass in kg written as whole grams.
        static string Grams(float kg)
        {
            return Mathf.RoundToInt(kg * 1000f) + " g";
        }
    }
}
