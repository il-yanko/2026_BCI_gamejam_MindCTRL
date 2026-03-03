using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class BarAdjust : MonoBehaviour
{
    public GameObject bar;
    public GameObject face;
    public Sprite faceExpression;
    public float height;

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

    public void Trigger() {
        StartCoroutine(DoStuff());
    }

    private IEnumerator DoStuff() {
        Vector3 position = bar.transform.position;
        Vector3 scale = bar.transform.localScale;
        float bottomY = position.y - scale.y / 2;
        float newY = bottomY + height / 2;
        face.transform.position = bar.transform.position = new Vector3(position.x, newY, position.z);
        bar.transform.localScale = new Vector3(scale.x, height, scale.y);

        face.GetComponent<Image>().overrideSprite = faceExpression;

        audioSource.Play();
        yield return new WaitUntil(() => audioSource.isPlaying == false);

        face.GetComponent<Image>().overrideSprite = null;
    }
}
