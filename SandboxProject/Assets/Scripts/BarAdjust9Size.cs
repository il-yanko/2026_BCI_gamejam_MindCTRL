using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class BarAdjust9Size : MonoBehaviour
{
    public float height;

    private SpriteRenderer barSprite;
    public Vector2 targetSize;
    private Vector2 currentVelocity;

    public void Start()
    {
        barSprite = GetComponent<SpriteRenderer>();
        targetSize = barSprite.size;
        currentVelocity = new();
    }

    public void Update()
    {
        //print($"Current: {barSprite.size.y}, Target: {targetSize.y}, Dist: {Vector2.Distance(barSprite.size, targetSize)}");
        if (Vector2.Distance(barSprite.size, targetSize) > 0.01f)
        {
            barSprite.size = Vector2.SmoothDamp(barSprite.size, targetSize, ref currentVelocity, 0.5f);
        }
    }

    public void SetHeight(float height) {
        targetSize = new Vector2(barSprite.size.x, height);
    }
}
