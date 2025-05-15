using UnityEngine;
using System.Collections;

public class KnightController : MonoBehaviour
{
    [Header("Настройки Движения")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Настройки Приседания")]
    public bool canMoveWhileCrouching = true;
    public float crouchMoveSpeedFactor = 0.5f;

    [Header("Настройки Атаки")]
    [Tooltip("Длительность состояния обычной/присевшей атаки")]
    public float groundAttackDuration = 0.5f;
    [Tooltip("Длительность состояния атаки в прыжке")]
    public float jumpAttackDuration = 0.4f; // Может быть короче или длиннее
    private bool isAttacking = false;

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
    private const string ANIM_IS_CROUCH = "IsCrouch";
    private const string ANIM_IS_CROUCH_WALK = "IsCrouchWalk";
    private const string ANIM_IS_ATTACK_1 = "IsAttack1";
    private const string ANIM_IS_CROUCH_ATTACK = "IsCrouchAttack";
    private const string ANIM_IS_JUMP_ATTACK = "IsJumpAttack"; // <--- НОВЫЙ ПАРАМЕТР

    void Awake()
    {
        // ... (Awake без изменений) ...
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
        // Если мы уже в какой-либо атаке, прерываем обработку нового ввода для атаки/движения
        if (isAttacking)
        {
            return;
        }

        CheckIfGrounded();
        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool isCurrentlyCrouching = (animator != null && animator.GetBool(ANIM_IS_CROUCH));

        // Обработка прыжка
        if (Input.GetButtonDown("Jump") && isGrounded && !isCurrentlyCrouching && !isAttacking)
        {
            Jump();
        }

        // Обработка АТАКИ (ЛКМ - Fire1 по умолчанию)
        if (Input.GetButtonDown("Fire1") && !isAttacking) // Проверяем !isAttacking здесь, чтобы не начать новую атаку во время кулдауна
        {
            HandleAttack();
        }

        UpdateAnimationState();
        FlipSprite();
    }

    void FixedUpdate()
    {
        // Если атакуем (любой тип атаки), возможно, захотим ограничить движение
        if (isAttacking)
        {
            // Для атаки в прыжке можно разрешить небольшое горизонтальное движение или оставить как есть
            // Для наземных атак мы останавливали: rb.velocity = new Vector2(0, rb.velocity.y);
            // Пока оставим универсальную остановку, но это можно настроить
            if (isGrounded) // Останавливаем горизонтальное движение только для наземных атак
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            // Для jump attack можно не менять rb.velocity.x, чтобы сохранить инерцию
            return;
        }
        // ... (остальной код FixedUpdate для движения без изменений) ...
        float currentAppliedSpeed = moveSpeed;
        bool isCurrentlyCrouchingOnGround = (animator != null && animator.GetBool(ANIM_IS_CROUCH) && isGrounded);

        if (isCurrentlyCrouchingOnGround)
        {
            if (canMoveWhileCrouching)
            {
                currentAppliedSpeed *= crouchMoveSpeedFactor;
            }
        }
        rb.linearVelocity = new Vector2(horizontalInput * currentAppliedSpeed, rb.linearVelocity.y);
    }

    void HandleAttack()
    {
        isAttacking = true; // Устанавливаем общий флаг атаки

        if (!isGrounded) // Атака в прыжке
        {
            animator.SetBool(ANIM_IS_JUMP_ATTACK, true);
            Debug.Log("Jump Attack!");
            // Сбрасываем другие потенциально активные состояния
            //animator.SetBool(ANIM_IS_JUMPING, false); // Атака в прыжке заменяет обычную анимацию прыжка
            StartCoroutine(AttackCooldown(jumpAttackDuration, false, false, true));
        }
        else // Наземные атаки
        {
            bool isCurrentlyCrouching = animator.GetBool(ANIM_IS_CROUCH);
            bool isCurrentlyCrouchWalking = animator.GetBool(ANIM_IS_CROUCH_WALK);

            if (isCurrentlyCrouching || isCurrentlyCrouchWalking)
            {
                animator.SetBool(ANIM_IS_CROUCH_ATTACK, true);
                Debug.Log("Crouch Attack!");
                StartCoroutine(AttackCooldown(groundAttackDuration, false, true, false));
            }
            else
            {
                animator.SetBool(ANIM_IS_ATTACK_1, true);
                Debug.Log("Normal Attack!");
                StartCoroutine(AttackCooldown(groundAttackDuration, true, false, false));
            }
            // Сбросить флаги других состояний, которые могут конфликтовать с наземной атакой
            animator.SetBool(ANIM_IS_RUNNING, false);
            animator.SetBool(ANIM_IS_JUMPING, false); // На земле мы не прыгаем
        }
    }

    // Изменяем AttackCooldown, чтобы он принимал информацию о типе атаки для сброса
    IEnumerator AttackCooldown(float duration, bool isNormalAttack, bool isCrouchAttack, bool isJumpAttackFlag)
    {
        yield return new WaitForSeconds(duration);

        if (isNormalAttack) animator.SetBool(ANIM_IS_ATTACK_1, false);
        if (isCrouchAttack) animator.SetBool(ANIM_IS_CROUCH_ATTACK, false);
        if (isJumpAttackFlag) animator.SetBool(ANIM_IS_JUMP_ATTACK, false);

        isAttacking = false;

        // После завершения атаки в прыжке, если мы все еще в воздухе, нужно вернуть анимацию прыжка/падения
        if (isJumpAttackFlag && !isGrounded && animator != null)
        {
            animator.SetBool(ANIM_IS_JUMPING, true); // Или IsFalling, если есть
        }
    }


    void Jump()
    {
        if (isAttacking) return;
        // ... (остальной код Jump без изменений) ...
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_JUMPING, true);
            animator.SetBool(ANIM_IS_RUNNING, false);
            animator.SetBool(ANIM_IS_CROUCH, false);
            animator.SetBool(ANIM_IS_CROUCH_WALK, false);
            animator.SetBool(ANIM_IS_ATTACK_1, false);
            animator.SetBool(ANIM_IS_CROUCH_ATTACK, false);
            animator.SetBool(ANIM_IS_JUMP_ATTACK, false); // Добавляем сброс
        }
    }

    void CheckIfGrounded()
    {
        bool wasGrounded = isGrounded; // Запоминаем предыдущее состояние
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Если мы только что приземлились И НЕ атакуем в прыжке
        if (!wasGrounded && isGrounded && animator != null && !animator.GetBool(ANIM_IS_JUMP_ATTACK))
        {
            animator.SetBool(ANIM_IS_JUMPING, false);
            // animator.SetBool(ANIM_IS_FALLING, false); // Если есть
        }
    }

    void UpdateAnimationState()
    {
        if (animator == null || isAttacking) return; // Не обновляем, если атакуем
        // ... (остальной код UpdateAnimationState без изменений) ...
        bool wantsToCrouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.01f;

        if (!isGrounded) // В воздухе (и не атакуем в прыжке, т.к. isAttacking проверен выше)
        {
            // Если мы не в анимации атаки в прыжке, то устанавливаем IsJumping
            if (!animator.GetBool(ANIM_IS_JUMP_ATTACK))
            {
                animator.SetBool(ANIM_IS_JUMPING, true);
            }
            animator.SetBool(ANIM_IS_RUNNING, false);
            animator.SetBool(ANIM_IS_CROUCH, false);
            animator.SetBool(ANIM_IS_CROUCH_WALK, false);
        }
        else // На земле
        {
            animator.SetBool(ANIM_IS_JUMPING, false); // На земле мы не в прыжке (если не только что приземлились и isAttacking еще true)

            if (wantsToCrouch)
            {
                animator.SetBool(ANIM_IS_CROUCH, true);
                animator.SetBool(ANIM_IS_RUNNING, false);
                if (canMoveWhileCrouching && hasHorizontalInput)
                {
                    animator.SetBool(ANIM_IS_CROUCH_WALK, true);
                }
                else
                {
                    animator.SetBool(ANIM_IS_CROUCH_WALK, false);
                }
            }
            else
            {
                animator.SetBool(ANIM_IS_CROUCH, false);
                animator.SetBool(ANIM_IS_CROUCH_WALK, false);
                animator.SetBool(ANIM_IS_RUNNING, hasHorizontalInput);
            }
        }
    }

    void FlipSprite()
    {
        // ... (код FlipSprite без изменений, возможно, добавить проверку на атаку в прыжке для блокировки поворота) ...
        if (spriteRenderer == null) return;
        // if (isAttacking && (animator.GetBool(ANIM_IS_ATTACK_1) || animator.GetBool(ANIM_IS_CROUCH_ATTACK) || animator.GetBool(ANIM_IS_JUMP_ATTACK)))
        // {
        //     return; // Не поворачивать во время любой атаки
        // }
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
        // ... (OnDrawGizmosSelected без изменений) ...
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}