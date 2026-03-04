using TMPro;
using UnityEngine;

public class PlayPauseHelper : MonoBehaviour
{
    public ParticleSystem[] particleSystems;
    public AudioSource[] sounds;
    private bool isPaused = true;

    public void Toggle()
    {
        isPaused = !isPaused;
        transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = isPaused ? "Play" : "Pause";

        if (!isPaused)
        {
            foreach (ParticleSystem particles in particleSystems)
            {
                ParticleSystem.MainModule particlesMain = particles.main;
                particlesMain.loop = true;
                particles.Play();
            }
            foreach (AudioSource sound in sounds)
            {
                sound.loop = true;
                if (!sound.isPlaying)
                {
                    sound.Play();
                }
            }
        }
        else
        {
            foreach (ParticleSystem particles in particleSystems)
            {
                ParticleSystem.MainModule particlesMain = particles.main;
                particlesMain.loop = false;
                particles.Stop();
            }
            foreach (AudioSource sound in sounds)
            {
                sound.Stop();
                sound.loop = false;
            }
        }
    }
}
