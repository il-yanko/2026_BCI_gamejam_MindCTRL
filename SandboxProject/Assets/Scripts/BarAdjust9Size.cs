using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BarAdjust9Size : MonoBehaviour
{
    [SerializeField] private GameObject bar;
    [SerializeField] private GameObject face;
    [SerializeField] private AudioSource sound;
    [SerializeField] private ParticleSystem particles;
    public Sprite faceExpression;
    public float height;
    public float pitch = 1;

    public void Trigger() {
        StartCoroutine(DoStuff());
    }

    private IEnumerator DoStuff() {
        SpriteRenderer barSprite = bar.GetComponent<SpriteRenderer>();
        Vector3 position = bar.transform.position;
        Vector3 scale = bar.transform.localScale;
        barSprite.size = new Vector2(barSprite.size.x, height);
        particles.transform.position = face.transform.position = new Vector3(position.x, bar.transform.GetChild(0).position.y, position.z);

        face.GetComponent<Image>().overrideSprite = faceExpression;

        particles.Play();

        sound.pitch = pitch;
        if (!(sound.isPlaying && sound.loop))
        {
            sound.Play();
        }
        yield return new WaitUntil(() => sound.isPlaying == false);

        face.GetComponent<Image>().overrideSprite = null;

        particles.Stop();
    }
}
