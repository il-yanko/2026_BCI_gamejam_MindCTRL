using UnityEngine;
using System.Collections;

/// <summary>
/// Plays a three-part segmented audio clip: start → middle (looped or once) → end.
/// Attach alongside an AudioSource (one will be added automatically if absent).
/// </summary>
public class SegmentedAudioPlayer : MonoBehaviour
{
    public AudioClip start, middle, end;
    public bool loop = false;

    private AudioSource _audioSource;
    private Coroutine   _playRoutine;
    private Coroutine   _stopRoutine;

    // ── Convenience properties ────────────────────────────────────────────────

    public bool isPlaying => _audioSource != null && _audioSource.isPlaying;

    public float volume
    {
        get => _audioSource != null ? _audioSource.volume : 1f;
        set { if (_audioSource != null) _audioSource.volume = value; }
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Play()
    {
        if (loop) PlayLoop();
        else      PlaySingle();
    }

    public void PlaySingle()
    {
        loop = false;
        RestartPlay();
    }

    public void PlayLoop()
    {
        loop = true;
        RestartPlay();
    }

    public void Stop()
    {
        if (_playRoutine != null) { StopCoroutine(_playRoutine); _playRoutine = null; }
        if (_stopRoutine != null) { StopCoroutine(_stopRoutine); _stopRoutine = null; }
        _stopRoutine = StartCoroutine(StopImpl());
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RestartPlay()
    {
        if (_playRoutine != null) { StopCoroutine(_playRoutine); _playRoutine = null; }
        if (_stopRoutine != null) { StopCoroutine(_stopRoutine); _stopRoutine = null; }
        _playRoutine = StartCoroutine(PlayImpl());
    }

    private IEnumerator PlayImpl()
    {
        if (start != null)
        {
            _audioSource.clip = start;
            _audioSource.loop = false;
            _audioSource.Play();
            yield return new WaitUntil(() => !_audioSource.isPlaying);
        }

        if (middle != null)
        {
            _audioSource.clip = middle;
            if (loop)
            {
                _audioSource.loop = true;
                _audioSource.Play();
            }
            else
            {
                _audioSource.loop = false;
                _audioSource.Play();
                yield return new WaitUntil(() => !_audioSource.isPlaying);

                if (end != null)
                {
                    _audioSource.clip = end;
                    _audioSource.loop = false;
                    _audioSource.Play();
                }
            }
        }

        _playRoutine = null;
    }

    private IEnumerator StopImpl()
    {
        if (_audioSource.isPlaying && _audioSource.loop)
        {
            _audioSource.loop = false;
            yield return new WaitUntil(() => !_audioSource.isPlaying);
        }
        else
        {
            _audioSource.Stop();
        }

        if (end != null)
        {
            _audioSource.clip = end;
            _audioSource.loop = false;
            _audioSource.Play();
        }

        _stopRoutine = null;
    }
}
