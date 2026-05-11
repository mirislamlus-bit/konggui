using UnityEngine;

public sealed class PlayerBoundsLimiter : MonoBehaviour
{
    [SerializeField] private float margin = 0.6f;
    [SerializeField] private float fallbackMinX = -7.5f;
    [SerializeField] private float fallbackMaxX = 7.5f;

    private Camera targetCamera;

    public float CurrentMinX { get; private set; }
    public float CurrentMaxX { get; private set; }

    private void Awake()
    {
        targetCamera = Camera.main;
        RecalculateBounds();
    }

    private void LateUpdate()
    {
        RecalculateBounds();

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, CurrentMinX, CurrentMaxX);
        transform.position = position;
    }

    private void RecalculateBounds()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || !targetCamera.orthographic)
        {
            CurrentMinX = fallbackMinX;
            CurrentMaxX = fallbackMaxX;
            return;
        }

        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        CurrentMinX = targetCamera.transform.position.x - halfWidth + margin;
        CurrentMaxX = targetCamera.transform.position.x + halfWidth - margin;
    }
}
