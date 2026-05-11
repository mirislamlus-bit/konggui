using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.2f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D body;
    private LanternVisionController lanternVision;
    private float horizontalInput;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        lanternVision = FindObjectOfType<LanternVisionController>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool isMoving = Mathf.Abs(horizontalInput) > 0.01f;

        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            spriteRenderer.flipX = horizontalInput < 0f;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            animator.SetTrigger("Interact");
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            animator.SetTrigger("RaiseLantern");
        }

        SyncLanternVisionAnimator();
    }

    private void FixedUpdate()
    {
        body.velocity = new Vector2(horizontalInput * moveSpeed, body.velocity.y);
    }

    private void SyncLanternVisionAnimator()
    {
        if (lanternVision == null)
        {
            lanternVision = FindObjectOfType<LanternVisionController>();
        }

        animator.SetBool("IsLanternVision", lanternVision != null && lanternVision.IsLanternVisionEnabled);
    }
}
