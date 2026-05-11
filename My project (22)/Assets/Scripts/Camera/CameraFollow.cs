using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 0.6f);
    [SerializeField] private bool useBounds;
    [SerializeField] private Vector2 minBounds = new Vector2(-8f, -3f);
    [SerializeField] private Vector2 maxBounds = new Vector2(8f, 3f);

    private Vector3 velocity;
    private Camera cachedCamera;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();

        if (target == null)
        {
            GameObject player = GameObject.Find("Player_LinZhaoying");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            -10f);

        if (useBounds && cachedCamera != null && cachedCamera.orthographic)
        {
            float halfHeight = cachedCamera.orthographicSize;
            float halfWidth = halfHeight * cachedCamera.aspect;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
            desiredPosition.z = -10f;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }
}
