using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SimpleLoopVFX : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int frameIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || framesPerSecond <= 0f)
        {
            return;
        }

        timer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;
        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
