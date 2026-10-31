using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] float floatInFront = 0.3f;
        [SerializeField] float popSize = 1.15f;
        [SerializeField] float popTime = 0.15f;
        [SerializeField] AudioClip snapClick;
        [SerializeField] Texture2D openHand;
        [SerializeField] Texture2D closedHand;
        [SerializeField] Collider padBase;
        [SerializeField] float floatGap = 0.25f;

        // Called every time a part is added, moved or removed.
        public event Action Changed;

        // The most parts a rocket can have.
        public int MaxParts => maxParts;

        // The store card under the cursor, or null. Set by the store cards.
        public PartData HoveredCard { get; set; }

        SnapPoint padSnap;
        PartData dragData;
        GameObject preview;
        SnapPoint previewSnap;
        int previewStep = -1;
        bool movingPlaced;
        float iconPixels;
        Texture2D shownCursor;
        AudioSource audioSource;

        // Creates the snap point on the pad where the first body goes, and the audio source for the click.
        void Awake()
        {
            padSnap = new GameObject("PadSnap").AddComponent<SnapPoint>();
            padSnap.transform.SetParent(transform, false);
            padSnap.side = SnapSide.Top;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Moves the dragged part every frame, picks up a placed part when the player presses on it, and updates the hand cursor and outline.
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
            else if (pointer.press.wasPressedThisFrame)
            {
                RocketPart part = GrabbablePart(position);
                if (part != null)
                {
                    PickUp(part);
                    Move(position);
                }
            }

            UpdateCursor(position);
            UpdateOutline(position);
        }

        // Outlines the dragged part, or else the part a press would pick up right now (the same check as picking up, so an outline always means it can be dragged).
        void UpdateOutline(Vector2 position)
        {
            if (preview != null) PartOutline.Show(preview.activeSelf ? preview : null);
            else
            {
                RocketPart part = GrabbablePart(position);
                PartOutline.Show(part != null ? part.gameObject : null);
            }
        }

        // The placed part a press would pick up now, or null (none while flying, dragging or over the UI).
        RocketPart GrabbablePart(Vector2 position)
        {
            if (preview != null || GameState.IsFlying || IsOverUI()) return null;
            return PartUnder(position);
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
            if (HoveredCard != null) return PlayerSession.IsUnlocked(HoveredCard.id) && CanAdd(HoveredCard);
            return GrabbablePart(position) != null;
        }

        // Lifts a placed part (with everything attached to it) off the rocket.
        void PickUp(RocketPart part)
        {
            dragData = part.data;
            iconPixels = 0f;
            preview = Lift(part);
            movingPlaced = true;
        }

        // Frees the snap point (or fin step) a placed part was using and tints it like a store preview.
        GameObject Lift(RocketPart part)
        {
            SnapPoint snap = part.attachedTo;
            if (snap != null && part.step >= 0)
            {
                snap.takenSteps[part.step] = false;
            }
            else if (snap != null)
            {
                snap.taken = false;
                SnapPoint ownSnap = OwnSnap(part.gameObject, Opposite(snap.side));
                if (ownSnap != null && snap != padSnap) ownSnap.taken = false;
            }

            part.attachedTo = null;
            part.step = -1;
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

        // Creates a hidden, tinted copy of the part to drag around.
        // Called when a store card starts dragging. iconHeight is the card picture's height in pixels, so the part can appear at that size.
        public void StartDrag(PartData data, float iconHeight)
        {
            if (GameState.IsFlying || IsFull() || !CanAdd(data)) return;

            dragData = data;
            iconPixels = iconHeight;
            preview = CreatePreview(data);
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

            // Where the part is now. A part that was hidden starts at the cursor.
            bool wasShown = preview.activeSelf;
            Pose from = wasShown ? new Pose(preview.transform.position, preview.transform.rotation) : new Pose(CursorPoint(cursor), transform.rotation);
            float fromSize = preview.transform.localScale.x;

            // Put the part at its goal at normal size.
            Hide();
            previewSnap = ClosestSnap(cursor, out previewStep);
            if (previewSnap != null) Snap();
            else Float(cursor, from.position);

            // A part that just appeared is as big as the store picture.
            if (!wasShown) fromSize = AppearSize();

            Ease(preview, from, fromSize);
        }

        // Hides the dragged part and lets the rocket sit back on the pad without it.
        void Hide()
        {
            preview.SetActive(false);
            previewSnap = null;
            previewStep = -1;
            SitOnPad();
        }

        // Puts the dragged part on the chosen snap point (or fin step).
        void Snap()
        {
            PlaceAt(preview, previewSnap, previewStep);
            preview.SetActive(true);
            SitOnPad();
        }

        // Hangs the dragged part from the cursor by the middle of its top edge, tilted against the way it moves so it feels like it has weight.
        void Float(Vector2 cursor, Vector3 from)
        {
            Transform cam = Camera.main.transform;
            Vector3 point = CursorPoint(cursor);

            // Upright first, to find how far the top is from the part's pivot.
            preview.transform.SetParent(transform);
            preview.transform.localScale = Vector3.one * FloatSize();
            preview.transform.SetPositionAndRotation(point, transform.rotation);
            preview.SetActive(true);
            Vector3 hang = TopCentre(preview) - point;

            // Then tilt it around the top, which stays on the cursor.
            float lag = Vector3.Dot(point - (from + hang), cam.right);
            Quaternion tilt = Quaternion.AngleAxis(Mathf.Clamp(-lag * tiltPerMetre, -maxTilt, maxTilt), cam.forward);
            preview.transform.SetPositionAndRotation(point - tilt * hang, tilt * transform.rotation);
            RestOnBase();
        }

        // Lifts the dragged part so it rests on top of the rocket base instead of sinking into it.
        void RestOnBase()
        {
            if (padBase == null) return;

            Bounds part = PartBounds(preview);
            Bounds pad = padBase.bounds;
            if (part.Intersects(pad) && part.min.y < pad.max.y)
                preview.transform.position += Vector3.up * (pad.max.y - part.min.y);
        }

        // Moves a part from where it was a bit towards where it is now (its goal), and its size towards its goal size.
        void Ease(GameObject part, Pose from, float fromSize)
        {
            float t = followSpeed * Time.deltaTime;
            Transform goal = part.transform;
            goal.SetPositionAndRotation(Vector3.Lerp(from.position, goal.position, t), Quaternion.Slerp(from.rotation, goal.rotation, t));
            goal.localScale = Vector3.one * Mathf.Lerp(fromSize, goal.localScale.x, t);
        }

        // The point under the cursor, a bit in front of the rocket so the rocket base never hides a floating part.
        Vector3 CursorPoint(Vector2 cursor)
        {
            Transform cam = Camera.main.transform;
            Ray ray = Camera.main.ScreenPointToRay(cursor);
            Plane plane = new Plane(-cam.forward, transform.position - cam.forward * floatInFront);
            plane.Raycast(ray, out float distance);
            return ray.GetPoint(distance);
        }

        // The size that makes a floating part look as big as it would on the rocket, since it is closer to the camera.
        float FloatSize()
        {
            Transform cam = Camera.main.transform;
            float rocketDistance = Vector3.Dot(transform.position - cam.position, cam.forward);
            return (rocketDistance - floatInFront) / rocketDistance;
        }

        // The middle of the top edge of a part and everything attached to it.
        static Vector3 TopCentre(GameObject part)
        {
            Bounds bounds = PartBounds(part);
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        // The box around all the meshes of a part and everything attached to it.
        static Bounds PartBounds(GameObject part)
        {
            MeshRenderer[] renderers = part.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return new Bounds(part.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            foreach (MeshRenderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        // The size that makes the dragged part look as big as its store picture (1 is normal size).
        float AppearSize()
        {
            if (iconPixels <= 0f) return 1f;

            Bounds bounds = PartBounds(preview);
            Camera cam = Camera.main;
            float distance = Vector3.Dot(bounds.center - cam.transform.position, cam.transform.forward);
            float metresPerPixel = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            float partPixels = Mathf.Max(bounds.size.x, bounds.size.y) / metresPerPixel;
            return preview.transform.localScale.x * iconPixels / partPixels;
        }

        // Keeps the preview as a real part if it is on a snap point, otherwise removes it.
        public void EndDrag()
        {
            if (preview == null) return;

            if (previewSnap != null)
            {
                Keep(preview, previewSnap, previewStep);
                SitOnPad();
                StartCoroutine(Pop(preview.transform));
                if (snapClick != null) audioSource.PlayOneShot(snapClick);
            }
            else
            {
                Destroy(preview);
            }

            preview = null;
            previewSnap = null;
            previewStep = -1;
            movingPlaced = false;
            PlayerSession.SaveRocket(ToDesign());
            Changed?.Invoke();
        }

        // Turns the placed parts into a RocketDesign. Parts that are not attached (or hang from one that is not) are left out.
        public RocketDesign ToDesign()
        {
            var design = new RocketDesign();
            var indexes = new Dictionary<RocketPart, int>();

            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
            {
                if (part.attachedTo == null) continue;

                int parent = -1;
                if (part.attachedTo != padSnap)
                {
                    RocketPart owner = part.attachedTo.transform.parent.GetComponent<RocketPart>();
                    if (!indexes.TryGetValue(owner, out parent)) continue;
                }

                indexes[part] = design.parts.Count;
                design.parts.Add(new DesignPart { partId = part.data.id, parent = parent, side = part.attachedTo.side, step = part.step });
            }

            return design;
        }

        // Builds a saved rocket on the pad. Parts the player does not own (and parts attached to them) are skipped.
        public void Load(RocketDesign design, PartData[] catalog)
        {
            var placed = new List<RocketPart>();

            foreach (DesignPart saved in design.parts)
            {
                PartData data = Array.Find(catalog, p => p.id == saved.partId);
                RocketPart parent = saved.parent >= 0 && saved.parent < placed.Count ? placed[saved.parent] : null;
                SnapPoint snap = saved.parent < 0 ? padSnap : parent != null ? OwnSnap(parent.gameObject, saved.side) : null;

                if (data == null || snap == null || !PlayerSession.IsUnlocked(data.id))
                {
                    placed.Add(null);
                    continue;
                }

                GameObject part = Instantiate(data.prefab, transform);
                Keep(part, snap, saved.step);
                placed.Add(part.GetComponent<RocketPart>());
            }

            SitOnPad();
            Changed?.Invoke();
        }

        // Makes a part that just snapped bounce: it grows a little and shrinks back to normal size. Stops if the part is picked up again.
        IEnumerator Pop(Transform part)
        {
            for (float time = 0f; time < popTime; time += Time.deltaTime)
            {
                if (part == null || IsDragged(part)) yield break;
                float bounce = Mathf.Sin(time / popTime * Mathf.PI);
                part.localScale = Vector3.one * Mathf.Lerp(1f, popSize, bounce);
                yield return null;
            }

            if (part != null && !IsDragged(part)) part.localScale = Vector3.one;
        }

        // Returns true if the part is the one being dragged.
        bool IsDragged(Transform part)
        {
            return part.gameObject == preview;
        }

        // Creates a hidden, tinted copy of a part.
        GameObject CreatePreview(PartData data)
        {
            GameObject part = Instantiate(data.prefab, transform);
            part.SetActive(false);
            Tint(part, true);
            return part;
        }

        // Turns a preview into a real part: exactly on its snap point (or fin step) at normal size, normal colours, and marks what it uses as taken.
        void Keep(GameObject part, SnapPoint snap, int step)
        {
            PlaceAt(part, snap, step);
            Tint(part, false);
            RocketPart rocketPart = part.GetComponent<RocketPart>();
            rocketPart.attachedTo = snap;
            rocketPart.step = step;

            if (step >= 0)
            {
                snap.takenSteps[step] = true;
                return;
            }

            // The part's own facing point is now taken too. Not on the pad: the body's bottom stays free for parts below it.
            snap.taken = true;
            SnapPoint ownSnap = OwnSnap(part, Opposite(snap.side));
            if (ownSnap != null && snap != padSnap) ownSnap.taken = true;
        }

        // Finds the free snap point that accepts the dragged part and is closest to the cursor on screen.
        // On a side line it also picks the closest free height step (step is -1 for normal points).
        SnapPoint ClosestSnap(Vector2 screenPosition, out int step)
        {
            SnapPoint closest = null;
            float closestDistance = maxSnapDistance;
            step = -1;

            foreach (SnapPoint snap in GetComponentsInChildren<SnapPoint>())
            {
                if (!Accepts(snap, dragData.Type)) continue;

                if (snap.IsLine)
                {
                    for (int i = 0; i < SnapPoint.LineSteps; i++)
                    {
                        if (snap.takenSteps[i]) continue;
                        float distance = ScreenDistance(snap.StepPosition(i), screenPosition);
                        if (distance < closestDistance)
                        {
                            closest = snap;
                            closestDistance = distance;
                            step = i;
                        }
                    }
                }
                else if (!snap.taken)
                {
                    float distance = ScreenDistance(snap.transform.position, screenPosition);
                    if (distance < closestDistance)
                    {
                        closest = snap;
                        closestDistance = distance;
                        step = -1;
                    }
                }
            }

            return closest;
        }

        // How far a point in the world is from a screen position, in pixels.
        static float ScreenDistance(Vector3 worldPoint, Vector2 screenPosition)
        {
            Vector2 onScreen = Camera.main.WorldToScreenPoint(worldPoint);
            return Vector2.Distance(onScreen, screenPosition);
        }

        // Returns true if a part of this type can attach to the snap point. The pad only takes a body.
        static bool Accepts(SnapPoint snap, PartType type)
        {
            RocketPart owner = snap.transform.parent.GetComponent<RocketPart>();
            if (owner == null) return type == PartType.Body;
            return owner.data.Accepts(snap.side, type);
        }

        // Puts a part on a snap point at normal size. A fin goes to its height step on the side line.
        // If the part has a snap point on the opposite side, that point lines up with it.
        static void PlaceAt(GameObject part, SnapPoint snap, int step)
        {
            Vector3 position = step >= 0 ? snap.StepPosition(step) : snap.transform.position;
            part.transform.SetParent(snap.transform.parent);
            part.transform.localScale = Vector3.one;
            part.transform.SetPositionAndRotation(position, snap.transform.rotation);

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

        // Lifts or lowers the rocket so its lowest point floats just above the pad.
        void SitOnPad()
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return;

            float lowest = float.MaxValue;
            foreach (MeshRenderer r in renderers)
                lowest = Mathf.Min(lowest, r.bounds.min.y);

            float lift = FloatHeight() - lowest;
            foreach (Transform child in transform)
                if (child.GetComponent<RocketPart>() != null)
                    child.position += Vector3.up * lift;
        }

        // The height where the rocket's bottom floats: the top of the pad plus part of the pad's height, so a bigger pad gives a bigger gap.
        float FloatHeight()
        {
            if (padBase == null) return transform.position.y;

            float top = padBase.bounds.max.y;
            float height = top - padBase.transform.position.y;
            return top + height * floatGap;
        }

        // The box around the parts on the rocket, without the part being dragged. Has no size when the rocket is empty.
        public Bounds PlacedBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (MeshRenderer r in GetComponentsInChildren<MeshRenderer>())
            {
                if (preview != null && r.transform.IsChildOf(preview.transform)) continue;
                if (first) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                first = false;
            }
            return bounds;
        }

        // Returns true if the rocket (or the part being dragged) uses this part.
        public bool IsUsed(PartData data)
        {
            if (dragData == data && preview != null) return true;

            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
                if (part.data == data) return true;
            return false;
        }

        // Returns true if the part can be added now. Every part except a body needs a body on the pad first.
        public bool CanAdd(PartData data)
        {
            return data.Type == PartType.Body || PadBody() != null;
        }

        // The body sitting on the pad, or null if there is none.
        BodyData PadBody()
        {
            foreach (RocketPart part in GetComponentsInChildren<RocketPart>())
                if (part.attachedTo == padSnap) return part.data as BodyData;
            return null;
        }

        // Returns true if the rocket already has the most parts allowed.
        public bool IsFull()
        {
            return PartCount() >= maxParts;
        }

        // How many parts the rocket has (the hidden preview does not count).
        int PartCount()
        {
            return GetComponentsInChildren<RocketPart>().Length;
        }
    }
}
