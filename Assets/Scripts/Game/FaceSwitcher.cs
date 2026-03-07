using UnityEngine;
using UnityEngine.UI;

public class FaceSwitcher : MonoBehaviour
{
    public UnityEngine.UI.Image faceImage;
    public Sprite idleFace;
    public Sprite[] singingFaces = new Sprite[4];

    private int _pitchLevel = 0;
    private bool _isSinging = false;

    public void SetPitchLevel(int level)
    {
        _pitchLevel = level;
        UpdateFace();
    }

    public void SetIsSinging(bool isSinging)
    {
        _isSinging = isSinging;
        UpdateFace();
    }

    private void UpdateFace()
    {
        faceImage.sprite = _isSinging ? singingFaces[_pitchLevel] : idleFace;
    }
}
