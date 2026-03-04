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

    [Header("Face Label (text fallback — used when no FaceSprites are assigned)")]
    public Text FaceLabel;

    [Header("Audio")]
    public AudioSource AudioSrc;
    /// <summary>One clip per pitch level (4 total).</summary>
    public AudioClip[] VoiceClips = new AudioClip[4];

    [Header("Singing Animation")]
    public float SwaySpeed  = 2.5f;
    public float SwayAmount = 0.12f;   // world-units (or pixels in UI)
    public float BobAmount  = 0.06f;

    [Header("Music Notes")]
    public float NoteInterval = 0.55f;  // average seconds between spawned notes

    // ── Runtime state ─────────────────────────────────────────────────────────
    public int CurrentPitchIndex { get; private set; } = 1;  // start at neutral (level 2)
    private bool      _isSinging;
    private Coroutine _singRoutine;
    private Coroutine _noteRoutine;
    private Vector3   _restLocalPos;

    static readonly string[] NoteGlyphs = { "♩", "♪", "♫", "♬" };

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        if (AudioSrc == null)
            AudioSrc = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        _restLocalPos = transform.localPosition;

        if (BlobImage != null) BlobImage.color = BlobColor;

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
        if (_isSinging) SwitchVoice();
    }

    // ── Audio ──────────────────────────────────────────────────────────────────

    public void PlayVoice()
    {
        // Animation always starts regardless of audio availability.
        _isSinging = true;
        if (_singRoutine == null)
            _singRoutine = StartCoroutine(SingAnimation());
        if (_noteRoutine == null)
            _noteRoutine = StartCoroutine(NoteLoop());

        if (GameConfig.Instance == null || !GameConfig.Instance.enableAudio) return;
        AudioClip clip = ClipAt(CurrentPitchIndex);
        if (clip == null) return;

        AudioSrc.volume = GameConfig.Instance.masterVolume;
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
        transform.localPosition = _restLocalPos;
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
        if (FaceLabel == null || FaceNames == null || index >= FaceNames.Length) return;
        FaceLabel.text = FaceNames[index].ToUpper();
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
