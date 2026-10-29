using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RocketToTheMoon.Core
{
    // Lets the player build the rocket. A part dragged from the store snaps to the closest free snap point and stays there when dropped.
    // A placed part can be dragged too: drop it on another free snap point to move it, or away from the rocket to remove it.
    public class RocketBuilder : MonoBehaviour
    {
        [SerializeField] float maxSnapDistance = 150f;
        [SerializeField] int maxParts = 12;

        // Called every time a part is added, moved or removed.
        public event Action Changed;

        SnapPoint padSnap;
        PartData dragData;
        GameObject preview;
        SnapPoint previewSnap;
        GameObject mirrorPreview;
        SnapPoint mirrorSnap;
        bool movingPlaced;

        // Creates the snap point on the pad where the first body goes.
        void Awake()
        {
            padSnap = new GameObject("PadSnap").AddComponent<SnapPoint>();
            padSnap.transform.SetParent(transform, false);
            padSnap.side = SnapSide.Top;
        }

        // Picks up a placed part when the player presses on it, and moves or drops it until they let go.
        void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;
            Vector2 position = pointer.position.ReadValue();

            if (movingPlaced)
            {
                if (pointer.press.isPressed) Drag(position);
                else EndDrag();
            }
            else if (pointer.press.wasPressedThisFrame && preview == null && !GameState.IsFlying && !IsOverUI())
            {
                RocketPart part = PartUnder(position);
                if (part == null) return;

                PickUp(part);
                Drag(position);
            }
        }

        // Lifts a placed part (with everything attached to it) off the rocket. A fin brings its pair along.
        void PickUp(RocketPart part)
        {
            dragData = part.data;
            preview = Lift(part);
            if (part.pair != null) mirrorPreview = Lift(part.pair);
            movingPlaced = true;
        }

        // Frees the snap points a placed part was using and tints it like a store preview.
        GameObject Lift(RocketPart part)
        {
            SnapPoint snap = part.attachedTo;
            if (snap != null)
            {
                snap.taken = false;
                SnapPoint ownSnap = OwnSnap(part.gameObject, Opposite(snap.side));
                if (ownSnap != null && snap != padSnap) ownSnap.taken = false;
            }

            part.attachedTo = null;
            part.transform.SetParent(transform);
            Tint(part.gameObject, true);
            return part.gameObject;
        }

        // Finds the placed part under the cursor, or null. When boxes overlap (a fin over the body), the smaller part wins.
        RocketPart PartUnder(Vector2 screenPosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RocketPart found = null;
            float smallest = float.MaxValue;

            foreach (MeshRenderer r in GetComponentsInChildren<MeshRenderer>())
            {
                RocketPart part = r.GetComponentInParent<RocketPart>();
                float size = r.bounds.size.magnitude;
                if (part != null && size < smallest && r.bounds.IntersectRay(ray))
                {
                    found = part;
                    smallest = size;
                }
            }

            return found;
        }

        // Returns true if the cursor is over a button, the store or any other UI.
        static bool IsOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // Creates a hidden, tinted copy of the part to drag around. Fins come in pairs, so they get a second copy.
        // Called when a store card starts dragging.
        public void StartDrag(PartData data)
        {
            bool isFin = data.Type == PartType.Fin;
            int newParts = isFin ? 2 : 1;
            if (GameState.IsFlying || PartCount() + newParts > maxParts) return;

            dragData = data;
            preview = CreatePreview(data);
            if (isFin) mirrorPreview = CreatePreview(data);
        }

        // Shows the preview on the free snap point closest to the cursor, or hides it if none is close enough.
        public void Drag(Vector2 screenPosition)
        {
            if (preview == null) return;

            // Pick the snap point while the preview is hidden, so the preview moving the rocket does not change the choice.
            preview.SetActive(false);
            if (mirrorPreview != null) mirrorPreview.SetActive(false);
            SitOnPad();
            previewSnap = ClosestSnap(screenPosition);
            if (previewSnap == null) return;

            PlaceAt(preview, previewSnap);
            preview.SetActive(true);

            // The second fin goes on the other side of the same part.
            if (mirrorPreview != null)
            {
                mirrorSnap = MirrorSnap(previewSnap);
                PlaceAt(mirrorPreview, mirrorSnap);
                mirrorPreview.SetActive(true);
            }

            SitOnPad();
        }

        // Keeps the preview (and its mirror fin) as real parts if they are on a snap point, otherwise removes them.
        public void EndDrag()
        {
            if (preview == null) return;

            if (preview.activeSelf)
            {
                Keep(preview, previewSnap);
                if (mirrorPreview != null)
                {
                    Keep(mirrorPreview, mirrorSnap);
                    RocketPart first = preview.GetComponent<RocketPart>();
                    RocketPart second = mirrorPreview.GetComponent<RocketPart>();
                    first.pair = second;
                    second.pair = first;
                }
            }
            else
            {
                Destroy(preview);
                if (mirrorPreview != null) Destroy(mirrorPreview);
            }

            preview = null;
            previewSnap = null;
            mirrorPreview = null;
            mirrorSnap = null;
            movingPlaced = false;
            Changed?.Invoke();
        }

        // Creates a hidden, tinted copy of a part.
        GameObject CreatePreview(PartData data)
        {
            GameObject part = Instantiate(data.prefab, transform);
            part.SetActive(false);
            Tint(part, true);
            return part;
        }

        // Turns a preview into a real part: normal colours, and its snap points are marked as taken.
        void Keep(GameObject part, SnapPoint snap)
        {
            Tint(part, false);
            snap.taken = true;
            part.GetComponent<RocketPart>().attachedTo = snap;

            // The part's own facing point is now taken too. Not on the pad: the body's bottom stays free for parts below it.
            SnapPoint ownSnap = OwnSnap(part, Opposite(snap.side));
            if (ownSnap != null && snap != padSnap) ownSnap.taken = true;
        }

        // Finds the free snap point that accepts the dragged part and is closest to the cursor on screen.
        SnapPoint ClosestSnap(Vector2 screenPosition)
        {
            SnapPoint closest = null;
            float closestDistance = maxSnapDistance;

            foreach (SnapPoint snap in GetComponentsInChildren<SnapPoint>())
            {
                if (snap.taken || !Accepts(snap, dragData.Type)) continue;
                if (mirrorPreview != null && MirrorSnap(snap) == null) continue;

                Vector2 snapOnScreen = Camera.main.WorldToScreenPoint(snap.transform.position);
                float distance = Vector2.Distance(snapOnScreen, screenPosition);
                if (distance < closestDistance)
                {
                    closest = snap;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        // Returns true if a part of this type can attach to the snap point. The pad only takes a body.
        static bool Accepts(SnapPoint snap, PartType type)
        {
            RocketPart owner = snap.transform.parent.GetComponent<RocketPart>();
            if (owner == null) return type == PartType.Body;
            return owner.data.Accepts(snap.side, type);
        }

        // Puts a part on a snap point. If the part has a snap point on the opposite side, that point lines up with it.
        static void PlaceAt(GameObject part, SnapPoint snap)
        {
            part.transform.SetParent(snap.transform.parent);
            part.transform.SetPositionAndRotation(snap.transform.position, snap.transform.rotation);

            SnapPoint ownSnap = OwnSnap(part, Opposite(snap.side));
            if (ownSnap != null)
                part.transform.position += snap.transform.position - ownSnap.transform.position;
        }

        // Finds a part's own snap point on the given side, or null if it has none.
        static SnapPoint OwnSnap(GameObject part, SnapSide side)
        {
            foreach (SnapPoint snap in part.GetComponentsInChildren<SnapPoint>(true))
                if (snap.side == side && snap.transform.parent == part.transform)
                    return snap;
            return null;
        }

        // Finds the free snap point on the other side of the same part (left and right), or null if there is none.
        static SnapPoint MirrorSnap(SnapPoint snap)
        {
            SnapPoint other = OwnSnap(snap.transform.parent.gameObject, Opposite(snap.side));
            return other != null && !other.taken ? other : null;
        }

        // The side that faces the given one (top faces bottom, left faces right).
        static SnapSide Opposite(SnapSide side)
        {
            if (side == SnapSide.Top) return SnapSide.Bottom;
            if (side == SnapSide.Bottom) return SnapSide.Top;
            if (side == SnapSide.Left) return SnapSide.Right;
            return SnapSide.Left;
        }

        // Colours the part with light teal, or gives it back its normal colours.
        static void Tint(GameObject part, bool on)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Palette.LightTeal);
            foreach (MeshRenderer r in part.GetComponentsInChildren<MeshRenderer>(true))
                r.SetPropertyBlock(on ? block : null);
        }

        // Lifts or lowers the rocket so its lowest point touches the top of the pad.
        void SitOnPad()
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return;

            float lowest = float.MaxValue;
            foreach (MeshRenderer r in renderers)
                lowest = Mathf.Min(lowest, r.bounds.min.y);

            float lift = transform.position.y - lowest;
            foreach (Transform child in transform)
                if (child.GetComponent<RocketPart>() != null)
                    child.position += Vector3.up * lift;
        }

        // Returns true if the rocket (or the part being dragged) uses this part.
        public bool IsUsed(PartData data)
        {
            if (dragData == data && preview != null) return true;

            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
                if (part.data == data) return true;
            return false;
        }

        // How many parts the rocket has (the hidden preview does not count).
        int PartCount()
        {
            return GetComponentsInChildren<RocketPart>().Length;
        }
    }
}
