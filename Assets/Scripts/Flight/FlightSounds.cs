using UnityEngine;

namespace RocketToTheMoon.Core
{
    // Plays the flight sounds: thruster start and loop, the fall just before a crash, and the explosion.
    // The explosion comes from the crash event, so it never waits for the fall sound.
    public class FlightSounds : MonoBehaviour
    {
        [SerializeField]
        FlightSimulator simulator;

        [SerializeField]
        AudioClip thrusterStart;

        [SerializeField]
        AudioClip thrusterLoop;

        [SerializeField]
        AudioClip falling;

        [SerializeField]
        AudioClip explosion;

        [SerializeField]
        ScoreManager score;

        [SerializeField]
        AudioClip newHighscore;

        [Tooltip("How many seconds before hitting the ground the fall sound starts.")]
        [SerializeField]
        float fallLeadTime = 1f;

        [Tooltip("How many seconds the thruster loop takes to fade out when the fuel runs out.")]
        [SerializeField]
        float fadeTime = 0.2f;

        AudioSource shotSource;
        AudioSource loopSource;
        AudioSource fallSource;
        bool wasBurning;
        bool fallPlayed;

        // Creates one audio source for each kind of sound.
        void Awake()
        {
            shotSource = gameObject.AddComponent<AudioSource>();
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.clip = thrusterLoop;
            loopSource.loop = true;
            fallSource = gameObject.AddComponent<AudioSource>();
            fallSource.clip = falling;
        }

        void OnEnable()
        {
            GameState.FlightStarted += OnFlightStarted;
            GameState.Crashed += OnCrashed;
            GameState.Landed += OnLanded;
        }

        void OnDisable()
        {
            GameState.FlightStarted -= OnFlightStarted;
            GameState.Crashed -= OnCrashed;
            GameState.Landed -= OnLanded;
        }

        // Plays the high score sound when the result shows a new best altitude.
        void OnLanded()
        {
            if (score.IsNewBest)
                shotSource.PlayOneShot(newHighscore);
        }

        // Gets the fall sound ready for the new flight.
        void OnFlightStarted()
        {
            fallSource.Stop();
            fallPlayed = false;
        }

        // Swaps the fall sound for the explosion.
        void OnCrashed(Vector3 position)
        {
            fallSource.Stop();
            shotSource.PlayOneShot(explosion);
        }

        void Update()
        {
            UpdateThruster();
            UpdateFall();
        }

        // Plays the start sound when the thrusters light, then the loop, and fades the loop out when they stop.
        void UpdateThruster()
        {
            bool burning = simulator.Burning;
            if (burning && !wasBurning)
            {
                shotSource.PlayOneShot(thrusterStart);
                loopSource.volume = 1f;
                loopSource.PlayDelayed(thrusterStart.length);
            }
            wasBurning = burning;

            if (burning || !loopSource.isPlaying)
                return;
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, 0f, Time.deltaTime / fadeTime);
            if (loopSource.volume <= 0f)
                loopSource.Stop();
        }

        // Starts the fall sound just before the crash, and stops it if the rocket went back to the pad.
        // Time scale counts because the flight can still be sped up while the rocket falls.
        void UpdateFall()
        {
            if (!simulator.HasFlight)
                fallSource.Stop();
            if (fallPlayed || !simulator.WillCrashIn(fallLeadTime * Time.timeScale))
                return;
            fallPlayed = true;
            fallSource.Play();
        }
    }
}
