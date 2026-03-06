using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SegmentedAudioPlayer : MonoBehaviour
{
    public AudioClip start, middle, end;
    public bool loop = false;
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlaySingle()
    {
        loop = false;
        StartCoroutine(Play());
    }

    public void PlayLoop()
    {
        loop = true;
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        audioSource.generator = start;
        audioSource.loop = false;
        audioSource.Play();
        yield return new WaitUntil(() => audioSource.isPlaying == false);

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

            audioSource.generator = end;
            audioSource.Play();
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

            audioSource.generator = end;
            audioSource.Play();
        }
    }
}
