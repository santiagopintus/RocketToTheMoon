using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Shows the assembling base that matches the highest tier the player ever bought, and sizes the pad collider to its platform.
    public class BaseSwap : MonoBehaviour
    {
        // One base per tier, tier 1 first. They are the same model at different sizes.
        [SerializeField]
        GameObject[] bases;

        [SerializeField]
        BoxCollider platform;

        // The platform's size for the tier 1 base, in the pad's own units.
        [SerializeField]
        Vector3 platformSize = new Vector3(1f, 0.1f, 1f);

        // Shows the right base before the saved rocket is built on it.
        void Awake()
        {
            Show();
        }

        // Starts listening for a new best tier.
        void OnEnable()
        {
            PlayerSession.TierChanged += Show;
        }

        void OnDisable()
        {
            PlayerSession.TierChanged -= Show;
        }

        // Turns on the base for the best tier, turns off the others and grows the collider as much as the base grew.
        void Show()
        {
            int index = Mathf.Clamp(PlayerSession.BestTier, 1, bases.Length) - 1;
            for (int i = 0; i < bases.Length; i++)
                bases[i].SetActive(i == index);

            float scale = bases[index].transform.localScale.x / bases[0].transform.localScale.x;
            platform.size = platformSize * scale;
            platform.center = new Vector3(0f, platformSize.y / 2f * scale, 0f);
        }
    }
}
