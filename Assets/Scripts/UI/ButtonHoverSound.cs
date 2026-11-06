using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RocketToTheMoon.UI
{
    // Plays a sound when the pointer moves onto any Button that can be clicked. One of these per scene is enough.
    public class ButtonHoverSound : MonoBehaviour
    {
        [SerializeField]
        AudioClip hoverClip;

        readonly List<RaycastResult> hits = new List<RaycastResult>();
        AudioSource audioSource;
        Button hovered;

        // Creates the audio source.
        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Checks which button is under the pointer and plays the sound when it changes.
        void Update()
        {
            Button button = ButtonUnderPointer();
            if (button != null && button != hovered && button.interactable)
                audioSource.PlayOneShot(hoverClip);
            hovered = button;
        }

        // Returns the button under the pointer, or null if there is none.
        Button ButtonUnderPointer()
        {
            if (Pointer.current == null || EventSystem.current == null)
                return null;

            var pointer = new PointerEventData(EventSystem.current);
            pointer.position = Pointer.current.position.ReadValue();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits.Count > 0 ? hits[0].gameObject.GetComponentInParent<Button>() : null;
        }
    }
}
