using UnityEngine;
using System.Collections;

public class SegmentedAudioPlayer : MonoBehaviour
{
    public AudioClip start, middle, end;
    public bool loop = false;
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = AddComponent<audioSource>();
        }
    }

    public void Play()
    {
        if (loop)
        {
            PlayLoop();
        }
        else
        {
            PlaySingle();
        }
    }

    public void PlaySingle()
    {
        loop = false;
        StartCoroutine(PlayImpl());
    }

    public void PlayLoop()
    {
        loop = true;
        StartCoroutine(PlayImpl());
    }

    private IEnumerator PlayImpl()
    {
        if (start != null)
        {
            audioSource.generator = start;
            audioSource.loop = false;
            audioSource.Play();
            yield return new WaitUntil(() => audioSource.isPlaying == false);
        }

        if (middle != null)
        {
            audioSource.generator = middle;
            if (loop)
            {
                audioSource.loop = true;
                audioSource.Play();
            }
            else
            {
                audioSource.Play();
                yield return new WaitUntil(() => audioSource.isPlaying == false);

                if (end != null)
                {
                    audioSource.generator = end;
                    audioSource.Play();
                }
            }
        }
    }

    public void Stop()
    {
        StartCoroutine(StopImpl());
    }

    private IEnumerator StopImpl()
    {
        if (audioSource.isPlaying && audioSource.loop)
        {
            audioSource.loop = false;
            yield return new WaitUntil(() => audioSource.isPlaying == false);

            if (end != null)
            {
                audioSource.generator = end;
                audioSource.Play();
            }
        }
    }
}
