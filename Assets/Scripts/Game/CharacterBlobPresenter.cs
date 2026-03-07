using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display widget for one character blob.
/// Shows the coloured blob and the face expression for the current pitch level.
/// Handles voice playback and a singing animation (sway + bob).
///
/// NOT a P300 stimulus — the PitchButtonPresenters below each blob are the stimuli.
/// </summary>
public class CharacterBlobPresenter : MonoBehaviour
{
    [Header("Character Info")]
    public int    CharacterIndex;
    public string CharacterName = "Character";

    [Header("Blob Visuals")]
    public Image BlobImage;
    public Color BlobColor = Color.white;

    [Header("Face Visuals")]
    public Image     FaceImage;
    public Sprite[]  FaceSprites = new Sprite[4];  // 0=Calm 1=Happy 2=Excited 3=Yelling
    public string[]  FaceNames   = { "Calm", "Happy", "Excited", "Yelling" };

    [Header("Face Label")]
    public Text     FaceLabel;
    /// <summary>ASCII face expressions shown on the big blob — should match the note-head buttons.</summary>
    public string[] FaceExpressions = { "- -\n ~", "^ ^\n u", "o o\n D", "O O\n !" };

    [Header("Audio")]
    public AudioSource AudioSrc;
    /// <summary>One clip per pitch level (4 total).</summary>
    public AudioClip[] VoiceClips = new AudioClip[4];

    [Header("Pitch Height")]
    public float PitchHeightStep = 25f;  // px the blob rises per pitch level

    [Header("Pitch Scale — blob stretches taller for higher pitch")]
    public float[] PitchScaleY = { 400f, 500f, 600f, 750f };  // Calm → Yelling

    [Header("Singing Animation")]
    public float SwaySpeed  = 2.5f;
    public float SwayAmount = 0.12f;   // world-units (or pixels in UI)
    public float BobAmount  = 0.06f;

    [Header("Music Notes")]
    public float NoteInterval = 0.55f;  // average seconds between spawned notes

    // ── Runtime state ─────────────────────────────────────────────────────────
    public int  CurrentPitchIndex { get; private set; } = 1;  // start at neutral (level 2)
    public bool IsSinging         => _isSinging;
    private bool      _isSinging;
    private Coroutine _singRoutine;
    private Coroutine _noteRoutine;
    private Coroutine _moveRoutine;
    private Coroutine _scaleRoutine;
    private Vector3   _baseLocalPos;   // spawn position, unaffected by pitch
    private Vector3   _restLocalPos;   // base + pitch height offset
    private float     _targetScaleY = 1f;
    private RectTransform _blobImageTransform => ((RectTransform)BlobImage.transform);

    static readonly string[] NoteGlyphs = { "♩", "♪", "♫", "♬" };

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        if (AudioSrc == null) AudioSrc = GetComponent<AudioSource>();
        if (AudioSrc == null) AudioSrc = gameObject.AddComponent<AudioSource>();

        _baseLocalPos = transform.localPosition;
        _restLocalPos = _baseLocalPos;
        print($"BasePos: {_baseLocalPos}");

        // Snap to starting pitch scale immediately (no coroutine needed at Awake time)
        _targetScaleY = ScaleForPitch(CurrentPitchIndex);
        _blobImageTransform.sizeDelta = new Vector2(_blobImageTransform.sizeDelta.x, _targetScaleY);

        // Show starting face
        ApplyFaceSprite(CurrentPitchIndex);
    }

    // ── Pitch & visuals ────────────────────────────────────────────────────────

    /// <summary>Updates the face sprite/label and, if singing, switches to the new voice.</summary>
    public void SetCurrentPitch(int pitchIndex)
    {
        CurrentPitchIndex = Mathf.Clamp(pitchIndex, 0, 3);
        ApplyFaceSprite(CurrentPitchIndex);
        ApplyFaceText(CurrentPitchIndex);
        UpdateRestPos();
        UpdatePitchScale();
        if (_isSinging) SwitchVoice();
    }

    // ── Audio ──────────────────────────────────────────────────────────────────

    public void PlayVoice()
    {
        // Animation always starts regardless of audio availability.
        _isSinging = true;
        /*if (_singRoutine == null && gameObject.activeInHierarchy)
            _singRoutine = StartCoroutine(SingAnimation());*/
        if (_noteRoutine == null && gameObject.activeInHierarchy)
            _noteRoutine = StartCoroutine(NoteLoop());

        if (GameConfig.Instance == null || !GameConfig.Instance.enableAudio) return;
        AudioClip clip = ClipAt(CurrentPitchIndex);
        if (clip == null) return;

        AudioSrc.volume = GameConfig.Instance.masterVolume;
        if (AudioSrc.isPlaying && AudioSrc.clip == clip) return;  // already on this note
        AudioSrc.clip   = clip;
        AudioSrc.loop   = true;
        AudioSrc.Play();
    }

    public void PauseVoice()
    {
        AudioSrc?.Pause();
        _isSinging = false;
        StopSingAnim();
        StopNoteAnim();
    }

    public void StopVoice()
    {
        AudioSrc?.Stop();
        _isSinging = false;
        StopSingAnim();
        StopNoteAnim();
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private IEnumerator SingAnimation()
    {
        float t = 0f;
        while (_isSinging)
        {
            t += Time.deltaTime * SwaySpeed;
            float sway = Mathf.Sin(t)        * SwayAmount;
            float bob  = Mathf.Abs(Mathf.Sin(t * 2f)) * BobAmount;
            transform.localPosition = _restLocalPos + new Vector3(sway, bob, 0f);
            yield return null;
        }
        transform.localPosition = _restLocalPos;
        _singRoutine = null;
    }

    private void StopSingAnim()
    {
        if (_singRoutine != null) { StopCoroutine(_singRoutine); _singRoutine = null; }
        if (_moveRoutine != null) { StopCoroutine(_moveRoutine); _moveRoutine = null; }
        transform.localPosition = _restLocalPos;
    }

    // ── Pitch scale ───────────────────────────────────────────────────────────

    private float ScaleForPitch(int index) =>
        (PitchScaleY != null && index >= 0 && index < PitchScaleY.Length)
            ? PitchScaleY[index] : PitchScaleY[0];

    private void UpdatePitchScale()
    {
        _targetScaleY = ScaleForPitch(CurrentPitchIndex);
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        if (!gameObject.activeInHierarchy)
            _blobImageTransform.sizeDelta = new Vector2(_blobImageTransform.sizeDelta.x, _targetScaleY);
        else
            _scaleRoutine = StartCoroutine(ScaleToPitch());
    }

    private IEnumerator ScaleToPitch()
    {
        while (Mathf.Abs(_blobImageTransform.sizeDelta.y - _targetScaleY) > 0.005f)
        {
            float newY = Mathf.Lerp(_blobImageTransform.sizeDelta.y, _targetScaleY, Time.deltaTime * 5f);
            _blobImageTransform.sizeDelta = new Vector2(_blobImageTransform.sizeDelta.x, newY);
            yield return null;
        }
        _blobImageTransform.sizeDelta = new Vector2(_blobImageTransform.sizeDelta.x, _targetScaleY);
        _scaleRoutine = null;
    }

    // ── Pitch height ──────────────────────────────────────────────────────────

    private void UpdateRestPos()
    {
        // Higher pitch → higher on screen. Centre the range around 0.
        //float yOff    = (CurrentPitchIndex - 1.5f) * PitchHeightStep;
        //_restLocalPos = _baseLocalPos + new Vector3(0f, yOff, 0f);

        if (_isSinging) return;  // SingAnimation reads _restLocalPos each frame — handled

        // Snap immediately if the GO is inactive (can't start coroutines while hidden)
        if (!gameObject.activeInHierarchy)
        {
            //transform.localPosition = _restLocalPos;
            return;
        }

        // Smoothly drift to new height when active and not singing
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveToRest());
    }

    private IEnumerator MoveToRest()
    {
        while ((_restLocalPos - transform.localPosition).sqrMagnitude > 0.25f)
        {
            //transform.localPosition = Vector3.Lerp(
                //transform.localPosition, _restLocalPos, Time.deltaTime * 8f);
            yield return null;
        }
        //transform.localPosition = _restLocalPos;
        _moveRoutine = null;
    }

    // ── Music notes ───────────────────────────────────────────────────────────

    private void StopNoteAnim()
    {
        if (_noteRoutine != null) { StopCoroutine(_noteRoutine); _noteRoutine = null; }
    }

    private IEnumerator NoteLoop()
    {
        while (_isSinging)
        {
            SpawnNote();
            yield return new WaitForSeconds(NoteInterval + Random.Range(-0.15f, 0.20f));
        }
        _noteRoutine = null;
    }

    private void SpawnNote()
    {
        // Parent to blobBox (transform.parent) so notes don't sway with the blob
        // but do originate from the blob's container position.
        var parent = transform.parent;
        if (parent == null) return;

        var noteGO = new GameObject("Note");
        noteGO.transform.SetParent(parent, false);
        noteGO.transform.SetAsLastSibling();  // render on top of the blob

        var txt       = noteGO.AddComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text      = NoteGlyphs[Random.Range(0, NoteGlyphs.Length)];
        txt.fontSize  = Random.Range(24, 42);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = new Color(BlobColor.r, BlobColor.g, BlobColor.b, 0.90f);

        var rt       = noteGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(54, 54);
        // Start near the top of the blob circle, randomly offset left/right
        rt.anchoredPosition = new Vector2(Random.Range(-28f, 28f), 90f);

        StartCoroutine(FloatNote(txt, rt));
    }

    private IEnumerator FloatNote(Text txt, RectTransform rt)
    {
        float duration = Random.Range(1.0f, 1.7f);
        float elapsed  = 0f;
        var   start    = rt.anchoredPosition;
        float driftX   = Random.Range(-20f, 20f);

        while (elapsed < duration)
        {
            if (rt == null) yield break;   // blob was destroyed mid-flight
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = start + new Vector2(driftX * t, 80f * t);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, Mathf.Lerp(0.90f, 0f, t));
            yield return null;
        }

        if (rt != null) Destroy(rt.gameObject);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void ApplyFaceSprite(int index)
    {
        if (FaceImage == null || FaceSprites == null || index >= FaceSprites.Length) return;
        FaceImage.sprite = FaceSprites[index];
    }

    private void ApplyFaceText(int index)
    {
        if (FaceLabel == null || FaceExpressions == null || index >= FaceExpressions.Length) return;
        FaceLabel.text = FaceExpressions[index];
    }

    private void SwitchVoice()
    {
        AudioClip clip = ClipAt(CurrentPitchIndex);
        if (clip == null) return;
        if (AudioSrc.clip == clip) return;  // same note — let it keep playing uninterrupted
        AudioSrc.clip = clip;
        AudioSrc.Play();
    }

    private AudioClip ClipAt(int i) =>
        (VoiceClips != null && i >= 0 && i < VoiceClips.Length) ? VoiceClips[i] : null;
}
