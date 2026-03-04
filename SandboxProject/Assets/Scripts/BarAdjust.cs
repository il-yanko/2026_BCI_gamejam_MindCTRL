using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BarAdjust : MonoBehaviour
{
    [SerializeField] private GameObject bar;
    [SerializeField] private GameObject face;
    [SerializeField] private AudioSource sound;
    public Sprite faceExpression;
    public float height;
    public float pitch = 1;

    public void Trigger() {
        StartCoroutine(DoStuff());
    }

    private IEnumerator DoStuff() {
        RectTransform barTransform = bar.GetComponent<RectTransform>();
        Vector3 position = barTransform.position;
        Vector3 scale = barTransform.localScale;
        float bottomY = position.y - scale.y / 2;
        float newY = position.y + height / 2;
        barTransform.transform.localScale = new Vector3(scale.x, height, scale.y);
        face.transform.position = new Vector3(position.x, bar.transform.GetChild(0).position.y, position.z);

        face.GetComponent<Image>().overrideSprite = faceExpression;

        sound.pitch = pitch;
        if (!(sound.isPlaying && sound.loop))
        {
            sound.Play();
        }
        yield return new WaitUntil(() => sound.isPlaying == false);

        face.GetComponent<Image>().overrideSprite = null;
    }
}
