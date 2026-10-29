using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RocketToTheMoon.Core
{
    // Lets the player build the rocket. A part dragged from the store floats under the cursor, glides onto the closest free snap point and stays there when dropped.
    // A placed part can be dragged too: drop it on another free snap point to move it, or away from the rocket to remove it.
    public class RocketBuilder : MonoBehaviour
    {
        [SerializeField] float maxSnapDistance = 150f;
        [SerializeField] int maxParts = 12;
        [SerializeField] float followSpeed = 15f;
        [SerializeField] float tiltPerMetre = 200f;
        [SerializeField] float maxTilt = 20f;
        [SerializeField] Texture2D openHand;
        [SerializeField] Texture2D closedHand;

        // Called every time a part is added, moved or removed.
        public event Action Changed;

        // The store card under the cursor, or null. Set by the store cards.
        public PartData HoveredCard { get; set; }

        SnapPoint padSnap;
        PartData dragData;
        GameObject preview;
        SnapPoint previewSnap;
        GameObject mirrorPreview;
        SnapPoint mirrorSnap;
        bool movingPlaced;
        float iconPixels;
        Texture2D shownCursor;

        // Creates the snap point on the pad where the first body goes.
        void Awake()
        {
            padSnap = new GameObject("PadSnap").AddComponent<SnapPoint>();
            padSnap.transform.SetParent(transform, false);
            padSnap.side = SnapSide.Top;
        }

        // Moves the dragged part every frame, picks up a placed part when the player presses on it, and updates the hand cursor.
        void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;
            Vector2 position = pointer.position.ReadValue();

            if (preview != null)
            {
                if (movingPlaced && !pointer.press.isPressed) EndDrag();
                else Move(position);
            }
            else if (pointer.press.wasPressedThisFrame && !GameState.IsFlying && !IsOverUI())
            {
                RocketPart part = PartUnder(position);
                if (part != null)
                {
                    PickUp(part);
                    Move(position);
                }
            }

            UpdateCursor(position);
        }

        // Shows a closed hand while dragging, an open hand over something that can be grabbed, and the normal arrow otherwise.
        void UpdateCursor(Vector2 position)
        {
            Texture2D wanted = null;
            if (preview != null) wanted = closedHand;
            else if (CanGrab(position)) wanted = openHand;
            if (wanted == shownCursor) return;

            shownCursor = wanted;
            Vector2 hotspot = wanted != null ? new Vector2(wanted.width, wanted.height) / 2f : Vector2.zero;
            Cursor.SetCursor(wanted, hotspot, CursorMode.Auto);
        }

        // Returns true if the cursor is over an owned store card or a placed part.
        bool CanGrab(Vector2 position)
        {
            if (GameState.IsFlying) return false;
            if (HoveredCard != null) return PlayerSession.IsUnlocked(HoveredCard.id);
            return !IsOverUI() && PartUnder(position) != null;
        }

        // Lifts a placed part (with everything attached to it) off the rocket. A fin brings its pair along.
        void PickUp(RocketPart part)
        {
            dragData = part.data;
            iconPixels = 0f;
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
        // Called when a store card starts dragging. iconHeight is the card picture's height in pixels, so the part can appear at that size.
        public void StartDrag(PartData data, float iconHeight)
        {
            bool isFin = data.Type == PartType.Fin;
            int newParts = isFin ? 2 : 1;
            if (GameState.IsFlying || PartCount() + newParts > maxParts) return;

            dragData = data;
            iconPixels = iconHeight;
            preview = CreatePreview(data);
            if (isFin) mirrorPreview = CreatePreview(data);
        }

        // Moves the dragged part a bit closer to its goal: the closest free snap point, or the cursor when none is close enough.
        // Over the store (or any other UI) the part stays hidden, as if it was still on the shelf.
        void Move(Vector2 cursor)
        {
            if (IsOverUI())
            {
                Hide();
                return;
            }

            // Where the parts are now. A part that was hidden starts at the cursor.
            bool wasShown = preview.activeSelf;
            Pose from = wasShown ? new Pose(preview.transform.position, preview.transform.rotation) : new Pose(CursorPoint(cursor), transform.rotation);
            float fromSize = preview.transform.localScale.x;
            bool mirrorWasShown = mirrorPreview != null && mirrorPreview.activeSelf;
            Pose mirrorFrom = mirrorWasShown ? new Pose(mirrorPreview.transform.position, mirrorPreview.transform.rotation) : from;
            float mirrorFromSize = mirrorWasShown ? mirrorPreview.transform.localScale.x : fromSize;

            // Put the parts at their goal at normal size.
            Hide();
            previewSnap = ClosestSnap(cursor);
            if (previewSnap != null) Snap();
            else Float(cursor, from.position);

            // A part that just appeared is as big as the store picture.
            if (!wasShown) fromSize = AppearSize();
            if (!mirrorWasShown) mirrorFromSize = fromSize;

            Ease(preview, from, fromSize);
            if (mirrorPreview != null && mirrorPreview.activeSelf) Ease(mirrorPreview, mirrorFrom, mirrorFromSize);
        }

        // Hides the dragged parts and lets the rocket sit back on the pad without them.
        void Hide()
        {
            preview.SetActive(false);
            if (mirrorPreview != null) mirrorPreview.SetActive(false);
            previewSnap = null;
            mirrorSnap = null;
            SitOnPad();
        }

        // Puts the dragged part (and its mirror fin on the other side) on the chosen snap point.
        void Snap()
        {
            preview.transform.localScale = Vector3.one;
            PlaceAt(preview, previewSnap);
            preview.SetActive(true);

            if (mirrorPreview != null)
            {
                mirrorSnap = MirrorSnap(previewSnap);
                mirrorPreview.transform.localScale = Vector3.one;
                PlaceAt(mirrorPreview, mirrorSnap);
                mirrorPreview.SetActive(true);
            }

            SitOnPad();
        }

        // Puts the dragged part under the cursor, tilted against the way it moves so it feels like it has weight.
        void Float(Vector2 cursor, Vector3 from)
        {
            Transform cam = Camera.main.transform;
            Vector3 point = CursorPoint(cursor);
            float lag = Vector3.Dot(point - from, cam.right);
            float angle = Mathf.Clamp(-lag * tiltPerMetre, -maxTilt, maxTilt);

            preview.transform.SetParent(transform);
            preview.transform.localScale = Vector3.one;
            preview.transform.SetPositionAndRotation(point, Quaternion.AngleAxis(angle, cam.forward) * transform.rotation);
            preview.SetActive(true);
        }

        // Moves a part from where it was a bit towards where it is now (its goal), and grows it towards normal size.
        void Ease(GameObject part, Pose from, float fromSize)
        {
            float t = followSpeed * Time.deltaTime;
            Transform goal = part.transform;
            goal.SetPositionAndRotation(Vector3.Lerp(from.position, goal.position, t), Quaternion.Slerp(from.rotation, goal.rotation, t));
            goal.localScale = Vector3.one * Mathf.Lerp(fromSize, 1f, t);
        }

        // The point under the cursor, at the rocket's distance from the camera.
        Vector3 CursorPoint(Vector2 cursor)
        {
            Ray ray = Camera.main.ScreenPointToRay(cursor);
            Plane plane = new Plane(-Camera.main.transform.forward, transform.position);
            plane.Raycast(ray, out float distance);
            return ray.GetPoint(distance);
        }

        // The size that makes the dragged part look as big as its store picture (1 is normal size).
        float AppearSize()
        {
            if (iconPixels <= 0f) return 1f;

            Bounds bounds = new Bounds(preview.transform.position, Vector3.zero);
            foreach (MeshRenderer r in preview.GetComponentsInChildren<MeshRenderer>())
                bounds.Encapsulate(r.bounds);

            Camera cam = Camera.main;
            float distance = Vector3.Dot(bounds.center - cam.transform.position, cam.transform.forward);
            float metresPerPixel = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            float partPixels = Mathf.Max(bounds.size.x, bounds.size.y) / metresPerPixel;
            return iconPixels / partPixels;
        }

        // Keeps the preview (and its mirror fin) as real parts if they are on a snap point, otherwise removes them.
        public void EndDrag()
        {
            if (preview == null) return;

            if (previewSnap != null)
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
                SitOnPad();
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

        // Turns a preview into a real part: exactly on its snap point at normal size, normal colours, and its snap points are marked as taken.
        void Keep(GameObject part, SnapPoint snap)
        {
            part.transform.localScale = Vector3.one;
            PlaceAt(part, snap);
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
