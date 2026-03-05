using UnityEngine;
using System.Collections;

public class BarLevelButton : MonoBehaviour
{
    [SerializeField] private AudioSource sound;
    [SerializeField] private ParticleSystem particles;
    public float pitch = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Trigger() {
        StartCoroutine(DoStuff());
    }

    private IEnumerator DoStuff() {
        //particles.transform.position /*= face.transform.position*/ = new Vector3(position.x, bar.transform.position.y, position.z);

        //face.GetComponent<Image>().overrideSprite = faceExpression;

        particles.Play();

        sound.pitch = pitch;
        if (!(sound.isPlaying && sound.loop))
        {
            sound.Play();
        }
        yield return new WaitUntil(() => sound.isPlaying == false);

        //face.GetComponent<Image>().overrideSprite = null;

        particles.Stop();
    }
}
