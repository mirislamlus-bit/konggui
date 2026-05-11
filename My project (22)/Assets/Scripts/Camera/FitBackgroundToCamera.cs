using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class FitBackgroundToCamera : MonoBehaviour
{
    [SerializeField] private float coverOverscan = 1.12f;

    private SpriteRenderer spriteRenderer;
    private Camera targetCamera;

    public float LastAppliedScale { get; private set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetCamera = Camera.main;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    public void Apply()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y) * coverOverscan;

        transform.position = new Vector3(0f, 0f, transform.position.z);
        transform.localScale = new Vector3(scale, scale, 1f);
        LastAppliedScale = scale;
    }
}
