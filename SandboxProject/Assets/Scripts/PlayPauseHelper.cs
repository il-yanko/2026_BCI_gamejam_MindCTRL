using TMPro;
using UnityEngine;

public class PlayPauseHelper : MonoBehaviour
{
    public AudioSource[] sounds;
    private bool isPaused = true;

    public void Toggle()
    {
        isPaused = !isPaused;
        this.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = isPaused ? "Play" : "Pause";

        if (isPaused)
        {
            foreach (AudioSource sound in sounds)
            {
                sound.Stop();
                sound.loop = false;
            }
        }
        else
        {
            foreach (AudioSource sound in sounds)
            {
                sound.loop = true;
                if (!sound.isPlaying)
                {
                    sound.Play();
                }
            }
        }
    }
}
