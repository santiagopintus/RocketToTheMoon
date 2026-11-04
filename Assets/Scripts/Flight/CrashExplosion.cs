using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Plays an explosion where the rocket hits the ground.
    public class CrashExplosion : MonoBehaviour
    {
        [SerializeField]
        ParticleSystem explosion;

        void OnEnable()
        {
            GameState.Crashed += Explode;
        }

        void OnDisable()
        {
            GameState.Crashed -= Explode;
        }

        // Moves the explosion to the crash point and plays it.
        void Explode(Vector3 position)
        {
            if (explosion == null)
                return;
            explosion.transform.position = position;
            explosion.Play();
        }
    }
}
