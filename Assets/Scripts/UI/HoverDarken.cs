using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // Darkens a button a little while the pointer is on it. Works even when the button is selected,
    // where the button's own hover colour would not show.
    public class HoverDarken : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("How bright the button stays on hover. 1 = no change, 0 = black.")]
        [SerializeField]
        float brightness = 0.85f;

        Graphic graphic;
        Color normalColor;

        // Remembers the button's graphic and its colour.
        void Awake()
        {
            graphic = GetComponent<Button>().targetGraphic;
            normalColor = graphic.color;
        }

        // Darkens the graphic, keeping its transparency.
        public void OnPointerEnter(PointerEventData eventData)
        {
            Color dark = normalColor * brightness;
            dark.a = normalColor.a;
            graphic.color = dark;
        }

        // Puts the colour back.
        public void OnPointerExit(PointerEventData eventData)
        {
            graphic.color = normalColor;
        }
    }
}
