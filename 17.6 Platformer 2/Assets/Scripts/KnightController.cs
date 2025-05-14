using UnityEngine;

public class KnightController : MonoBehaviour
{
    [Header("Настройки Движения")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Проверка Земли")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;

    // Имена параметров аниматора
    private const string ANIM_IS_RUNNING = "IsRunning";
    private const string ANIM_IS_JUMPING = "IsJumping";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null) { Debug.LogError("Rigidbody2D не найден!", this); enabled = false; return; }
        if (animator == null) { Debug.LogWarning("Animator не найден. Анимации не будут работать.", this); }
        if (spriteRenderer == null) { Debug.LogWarning("SpriteRenderer не найден. Поворот спрайта может не работать.", this); }
        if (groundCheck == null) { Debug.LogError("'Ground Check' не назначен!", this); enabled = false; return; }
        if (groundLayer.value == 0) { Debug.LogWarning("'Ground Layer' не назначен. Проверка земли может не работать.", this); }
    }

    void Update()
    {
        // 1. Проверка, на земле ли мы
        CheckIfGrounded();
        Debug.Log("isGrounded: " + isGrounded + " | Animator IsJumping: " + (animator != null ? animator.GetBool(ANIM_IS_JUMPING).ToString() : "N/A")); // <--- ЛОГ ДЛЯ ISGROUNDED

        // 2. Получаем горизонтальный ввод
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 3. Обработка прыжка
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // 4. Обновляем анимацию
        UpdateAnimationState();

        // 5. Поворот спрайта
        FlipSprite();
    }

    void FixedUpdate()
    {
        // ВОЗВРАЩАЕМ ДВИЖЕНИЕ ПЕРСОНАЖА
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        // animator.SetBool(ANIM_IS_JUMPING, true); // По-прежнему убрано, пусть решает UpdateAnimationState
    }

    void CheckIfGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void UpdateAnimationState()
    {
        if (animator == null) return;

        if (!isGrounded) // Если мы в воздухе
        {
            animator.SetBool(ANIM_IS_JUMPING, true);
            animator.SetBool(ANIM_IS_RUNNING, false); // Бег выключен в воздухе
        }
        else // Если мы на земле
        {
            animator.SetBool(ANIM_IS_JUMPING, false);

            // Логика для бега (когда мы на земле)
            bool isRunning = Mathf.Abs(horizontalInput) > 0.01f;
            animator.SetBool(ANIM_IS_RUNNING, isRunning);
        }
    }

    void FlipSprite()
    {
        if (spriteRenderer == null) return;

        // Поворачиваем, только если есть горизонтальный ввод
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            if ((horizontalInput > 0 && !isFacingRight) || (horizontalInput < 0 && isFacingRight))
            {
                isFacingRight = !isFacingRight;
                spriteRenderer.flipX = !isFacingRight;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}