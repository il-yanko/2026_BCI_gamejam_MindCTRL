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

    // ── Runtime state ─────────────────────────────────────────────────────────
    public int CurrentPitchIndex { get; private set; } = 1;  // start at neutral (level 2)
    private bool      _isSinging;
    private Coroutine _singRoutine;
    private Vector3   _restLocalPos;

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
    }

    public void StopVoice()
    {
        AudioSrc?.Stop();
        _isSinging = false;
        StopSingAnim();
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
