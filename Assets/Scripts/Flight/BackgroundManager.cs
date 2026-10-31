using System;
using System.Collections.Generic;
using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Fades between backgrounds as the rocket climbs. Each background fades in from its start altitude
    // until it is fully shown at its full altitude. Backgrounds that cannot be seen are not drawn.
    public class BackgroundManager : MonoBehaviour
    {
        [Serializable]
        public class Background
        {
            public string name;
            public Renderer renderer;
            public float startAltitude;
            public float fullAltitude;

            [Tooltip("When fully shown, hides the backgrounds above it in the list.")]
            public bool coversBelow = true;

            [HideInInspector]
            public float alpha;

            [HideInInspector]
            public Color baseColor;
        }

        [Tooltip("Lowest background first, highest last.")]
        [SerializeField]
        List<Background> backgrounds = new List<Background>();

        [SerializeField]
        float fadeSpeed = 3f;

        // Remembers each background's colour and shows the right ones for the ground.
        void Start()
        {
            foreach (Background b in backgrounds)
            {
                if (b.renderer == null)
                    continue;
                b.baseColor = b.renderer.material.GetColor("_BaseColor");
                b.alpha = TargetAlpha(b.startAltitude, b.fullAltitude, GameState.Altitude);
            }
            ShowBackgrounds();
        }

        // Moves each background's fade towards what the current altitude asks for.
        void Update()
        {
            foreach (Background b in backgrounds)
            {
                float target = TargetAlpha(b.startAltitude, b.fullAltitude, GameState.Altitude);
                b.alpha = Mathf.Lerp(b.alpha, target, fadeSpeed * Time.deltaTime);
                if (Mathf.Abs(b.alpha - target) < 0.001f)
                    b.alpha = target;
            }
            ShowBackgrounds();
        }

        // Draws the visible backgrounds with their fade. Turns off the ones that are invisible or covered by a higher one.
        void ShowBackgrounds()
        {
            bool covered = false;
            for (int i = backgrounds.Count - 1; i >= 0; i--)
            {
                Background b = backgrounds[i];
                if (b.renderer == null)
                    continue;

                bool show = b.alpha > 0f && !covered;
                b.renderer.enabled = show;
                if (show)
                {
                    Color color = b.baseColor;
                    color.a *= b.alpha;
                    b.renderer.material.SetColor("_BaseColor", color);
                }
                if (b.coversBelow && b.alpha >= 1f)
                    covered = true;
            }
        }

        // How visible a background should be at this altitude: 0 below start, 1 from full, a blend in between.
        public static float TargetAlpha(float startAltitude, float fullAltitude, float altitude)
        {
            if (fullAltitude <= startAltitude)
                return altitude >= startAltitude ? 1f : 0f;
            return Mathf.Clamp01((altitude - startAltitude) / (fullAltitude - startAltitude));
        }
    }
}
