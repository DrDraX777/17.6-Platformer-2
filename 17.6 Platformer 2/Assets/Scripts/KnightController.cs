using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public float jumpAttackDuration = 0.4f;
    private bool isAttacking = false;

    [Header("Проверка Земли")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; // Этот слой должен включать и обычную землю, и платформы

    [Header("Настройки Прыжка Вниз")]
    [Tooltip("Как долго игнорировать платформу после прыжка вниз (в секундах)")]
    public float ignorePlatformDuration = 0.3f;
    private bool isDroppingThroughPlatform = false;
    private Collider2D playerCollider;
    private List<Collider2D> currentlyIgnoredColliders = new List<Collider2D>();

    [Header("Взаимодействие с Платформами")]
    [Tooltip("Слой, на котором находятся движущиеся платформы (для парентинга).")]
    public LayerMask movingPlatformLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;

    // Для логики с движущимися платформами
    private bool jumpInitiatedThisFrame = false;
    private Rigidbody2D parentPlatformRigidbody;

    // Имена параметров аниматора
    private const string ANIM_IS_RUNNING = "IsRunning";
    private const string ANIM_IS_JUMPING = "IsJumping";
    private const string ANIM_IS_CROUCH = "IsCrouch";
    private const string ANIM_IS_CROUCH_WALK = "IsCrouchWalk";
    private const string ANIM_IS_ATTACK_1 = "IsAttack1";
    private const string ANIM_IS_CROUCH_ATTACK = "IsCrouchAttack";
    private const string ANIM_IS_JUMP_ATTACK = "IsJumpAttack";


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (playerCollider == null) { Debug.LogError("Основной Collider2D игрока не найден!", this); enabled = false; return; }
        if (rb == null) { Debug.LogError("Rigidbody2D не найден!", this); enabled = false; return; }
        if (animator == null) { Debug.LogWarning("Animator не найден. Анимации не будут работать.", this); }
        if (spriteRenderer == null) { Debug.LogWarning("SpriteRenderer не найден. Поворот спрайта может не работать.", this); }
        if (groundCheck == null) { Debug.LogError("'Ground Check' не назначен!", this); enabled = false; return; }
        if (groundLayer.value == 0) { Debug.LogWarning("'Ground Layer' не назначен. Проверка земли может не работать.", this); }
        if (movingPlatformLayer.value == 0) { Debug.LogWarning("'Moving Platform Layer' не назначен. Взаимодействие с движущимися платформами может не работать.", this); }
    }

    void Update()
    {
        if (isAttacking || isDroppingThroughPlatform)
        {
            if (isAttacking && isDroppingThroughPlatform) { /* Позволяем обоим */ }
            else if (isAttacking) { return; }
            // Если isDroppingThroughPlatform, Update продолжается для CheckIfGrounded
        }

        CheckIfGrounded();
        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool isCurrentlyCrouching = (animator != null && animator.GetBool(ANIM_IS_CROUCH));
        bool wantsToCrouchInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Сбрасываем флаг инициации прыжка в начале каждого Update
        jumpInitiatedThisFrame = false;

        if (wantsToCrouchInput && Input.GetButtonDown("Jump") && isGrounded && !isAttacking && !isDroppingThroughPlatform)
        {
            // Вызываем TryDropThroughPlatform. Он сам решит, открепляться или нет.
            TryDropThroughPlatform();
        }
        else if (Input.GetButtonDown("Jump") && isGrounded && !isCurrentlyCrouching && !isAttacking && !isDroppingThroughPlatform)
        {
            // Обычный прыжок: открепляемся, если были припарентены
            if (transform.parent != null)
            {
                transform.SetParent(null);
                parentPlatformRigidbody = null;
                // Debug.Log("Откреплен от платформы для обычного прыжка.");
            }
            Jump();
        }
        if (Input.GetButtonDown("Fire1") && !isAttacking && !isDroppingThroughPlatform)
        {
            HandleAttack();
        }

        if (!isDroppingThroughPlatform)
        {
            UpdateAnimationState();
            FlipSprite();
        }
    }

    void FixedUpdate()
    {
        // --- Рассчитываем желаемую скорость игрока от ЕГО ВВОДА ---
        float playerInputHorizontalSpeed = 0f;
        if (!(isAttacking || isDroppingThroughPlatform))
        {
            float currentSpeedSetting = moveSpeed; // Базовая скорость для этого кадра
            bool isCrouchingNow = (animator != null && animator.GetBool(ANIM_IS_CROUCH) && isGrounded);

            if (isCrouchingNow)
            {
                if (canMoveWhileCrouching)
                {
                    currentSpeedSetting *= crouchMoveSpeedFactor;
                }
                else // Если не можем двигаться в приседе
                {
                    currentSpeedSetting = 0; // Скорость от ввода = 0
                }
            }
            playerInputHorizontalSpeed = horizontalInput * currentSpeedSetting;
        }
        // Если атакуем или падаем, playerInputHorizontalSpeed остается 0 (или можно добавить инерцию от предыдущего движения)


        // --- Применяем скорости ---
        if (transform.parent != null && parentPlatformRigidbody != null && isGrounded) // Если мы на движущейся платформе с Rigidbody
        {
            Vector2 platformVelocity = parentPlatformRigidbody.linearVelocity;

            // Целевая скорость игрока = его собственный ввод по X + скорость платформы по X,
            // его текущая вертикальная скорость (от прыжка/гравитации) + скорость платформы по Y (если он не прыгает)

            float targetHorizontalVelocity = playerInputHorizontalSpeed + platformVelocity.x;
            float targetVerticalVelocity;

            if (!jumpInitiatedThisFrame) // Если не прыгнули в этом кадре Update
            {
                // "Прилипаем" к вертикальному движению платформы
                targetVerticalVelocity = platformVelocity.y;
                // Мы хотим, чтобы игрок оставался на платформе, поэтому его итоговая вертикальная скорость
                // должна быть как у платформы, плюс его собственный вертикальный импульс, если он есть (но его нет, если не прыжок).
                // Однако, гравитация все еще будет действовать на rb.velocity.y игрока между FixedUpdate.
                // Чтобы игрок не "проваливался" сквозь платформу, движущуюся вверх, или не "отставал",
                // когда она движется вниз, важно, чтобы его позиция корректировалась.
                // Парентинг должен это делать. Но если velocity конфликтует...

                // Попробуем так: если на платформе, и не прыгает, то его Y скорость = Y скорость платформы.
                // Это может сделать его "невесомым" на платформе, но должно предотвратить отскоки.
                // Гравитация на игрока все еще будет пытаться тянуть его вниз.
                // Платформа, как родитель, должна его "поддерживать".
                targetVerticalVelocity = platformVelocity.y; // Заставляем Y скорость игрока быть как у платформы

                // Компенсация "проседания" из-за гравитации игрока, когда платформа движется вверх:
                // Если platformVelocity.y > 0 (едет вверх) и rb.velocity.y < platformVelocity.y (игрок "отстает")
                // то можно немного подтолкнуть игрока, но это уже усложнение.
                // Парентинг должен справляться с этим, если скорость игрока не мешает.
            }
            else // Если прыжок был инициирован
            {
                // rb.velocity.y уже содержит импульс от Jump().
                // Добавляем к нему вертикальную скорость платформы.
                targetVerticalVelocity = rb.linearVelocity.y + platformVelocity.y;
            }

            rb.linearVelocity = new Vector2(targetHorizontalVelocity, targetVerticalVelocity);

        }
        else // Если не на платформе (или платформа без Rigidbody - такой случай пока не обрабатываем детально)
        {
            // Применяем только скорость от ввода (горизонтальную), вертикальная от rb.velocity.y (гравитация/прыжок)
            rb.linearVelocity = new Vector2(playerInputHorizontalSpeed, rb.linearVelocity.y);
        }

        // Обработка остановки при атаке
        if (isAttacking && isGrounded && !isDroppingThroughPlatform)
        {
            if (transform.parent != null && parentPlatformRigidbody != null)
            {
                // Двигаемся только со скоростью платформы по X, Y уже учтен
                rb.linearVelocity = new Vector2(parentPlatformRigidbody.linearVelocity.x, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & movingPlatformLayer) != 0)
        {
            bool landedOnTop = false;
            foreach (ContactPoint2D point in collision.contacts)
            {
                if (Vector2.Dot(point.normal, Vector2.up) > 0.7f) // Более строгая проверка стояния сверху
                {
                    landedOnTop = true;
                    break;
                }
            }

            if (landedOnTop)
            {
                // Парентимся, только если еще не припарентены к этой платформе
                if (transform.parent != collision.transform)
                {
                    transform.SetParent(collision.transform);
                    parentPlatformRigidbody = collision.transform.GetComponent<Rigidbody2D>(); // Получаем RB платформы
                    if (parentPlatformRigidbody == null)
                    {
                        // Debug.LogWarning("Платформа " + collision.transform.name + " не имеет Rigidbody2D. Следование может быть неточным.");
                    }
                    // Debug.Log("Приземлился на платформу: " + collision.transform.name);
                }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform == transform.parent) // Если это была наша родительская платформа
        {
            transform.SetParent(null);
            parentPlatformRigidbody = null;
            // Debug.Log("Сошел с платформы: " + collision.transform.name);
        }
    }

    void Jump()
    {
        // Открепление от родителя происходит в Update перед вызовом Jump
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpInitiatedThisFrame = true; // Устанавливаем флаг, что прыжок был инициирован
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_JUMPING, true);
            animator.SetBool(ANIM_IS_RUNNING, false);
            animator.SetBool(ANIM_IS_CROUCH, false);
            animator.SetBool(ANIM_IS_CROUCH_WALK, false);
            animator.SetBool(ANIM_IS_ATTACK_1, false);
            animator.SetBool(ANIM_IS_CROUCH_ATTACK, false);
            animator.SetBool(ANIM_IS_JUMP_ATTACK, false);
        }
    }

    void HandleAttack()
    {
        isAttacking = true;
        if (!isGrounded)
        {
            animator.SetBool(ANIM_IS_JUMP_ATTACK, true);
            StartCoroutine(AttackCooldown(jumpAttackDuration, false, false, true));
        }
        else
        {
            bool isCurrentlyCrouching = animator.GetBool(ANIM_IS_CROUCH);
            bool isCurrentlyCrouchWalking = animator.GetBool(ANIM_IS_CROUCH_WALK);
            if (isCurrentlyCrouching || isCurrentlyCrouchWalking)
            {
                animator.SetBool(ANIM_IS_CROUCH_ATTACK, true);
                StartCoroutine(AttackCooldown(groundAttackDuration, false, true, false));
            }
            else
            {
                animator.SetBool(ANIM_IS_ATTACK_1, true);
                StartCoroutine(AttackCooldown(groundAttackDuration, true, false, false));
            }
            animator.SetBool(ANIM_IS_RUNNING, false);
            animator.SetBool(ANIM_IS_JUMPING, false);
        }
    }

    IEnumerator AttackCooldown(float duration, bool isNormalAttack, bool isCrouchAttack, bool isJumpAttackFlag)
    {
        yield return new WaitForSeconds(duration);
        if (isNormalAttack) animator.SetBool(ANIM_IS_ATTACK_1, false);
        if (isCrouchAttack) animator.SetBool(ANIM_IS_CROUCH_ATTACK, false);
        if (isJumpAttackFlag) animator.SetBool(ANIM_IS_JUMP_ATTACK, false);
        isAttacking = false;
        if (isJumpAttackFlag && !isGrounded && animator != null)
        {
            animator.SetBool(ANIM_IS_JUMPING, true);
        }
    }

    void TryDropThroughPlatform()
    {
        // Проверяем, стоим ли мы на платформе, через которую можно провалиться
        Collider2D[] collidersUnderneath = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);

        Collider2D platformToDropThrough = null; // Ищем платформу ИМЕННО с эффектором
        foreach (Collider2D col in collidersUnderneath)
        {
            if (col.GetComponent<PlatformEffector2D>() != null) // <--- КЛЮЧЕВАЯ ПРОВЕРКА
            {
                if (col != playerCollider) // Убедимся, что это не коллайдер самого игрока
                {
                    platformToDropThrough = col;
                    break;
                }
            }
        }

        if (platformToDropThrough != null) // Если нашли платформу с эффектором
        {
            // Только теперь, когда мы уверены, что есть куда падать, открепляемся (если были припарентены)
            if (transform.parent == platformToDropThrough.transform) // Открепляемся только если это наш родитель
            {
                transform.SetParent(null);
                parentPlatformRigidbody = null; // Сбрасываем ссылку, т.к. мы больше не на этой платформе как на родителе
                Debug.Log("Откреплен от платформы с эффектором для прыжка вниз: " + platformToDropThrough.name);
            }
            // Если transform.parent был null или другой, ничего страшного, просто проигнорируем эту платформу
            // и не будем ее делать родителем.

            StartCoroutine(DropThroughPlatformCoroutine(platformToDropThrough));
        }
        // Если подходящая платформа с PlatformEffector2D не найдена,
        // то комбинация "Вниз + Прыжок" ничего не делает (не происходит открепления, не запускается корутина).
        // Игрок остается припарентенным, если был, и не двигается (т.к. isDroppingThroughPlatform не установится).
        // Можно добавить здесь звук "нельзя" или другую реакцию.
        else
        {
            Debug.Log("Попытка спрыгнуть вниз, но подходящая платформа с PlatformEffector2D не найдена.");
            // Если мы хотим, чтобы на обычной платформе "Вниз + Прыжок" работало как обычный прыжок,
            // то здесь можно было бы вызвать Jump(), но тогда нужно быть осторожным с флагом isCurrentlyCrouching.
            // Пока оставим так: если нет платформы с эффектором, "Вниз+Прыжок" ничего не делает.
        }
    }

    IEnumerator DropThroughPlatformCoroutine(Collider2D platformToIgnore) // platformToIgnore - это платформа с эффектором
    {
        isDroppingThroughPlatform = true; // Устанавливаем флаг, что мы в процессе

        currentlyIgnoredColliders.Add(platformToIgnore);
        Physics2D.IgnoreCollision(playerCollider, platformToIgnore, true); // Игнорируем ТОЛЬКО эту платформу
        Debug.Log($"Игнорируем платформу с эффектором: {platformToIgnore.name}");

        if (animator != null)
        {
            animator.SetBool(ANIM_IS_JUMPING, true);
            animator.SetBool(ANIM_IS_CROUCH, false);
            animator.SetBool(ANIM_IS_CROUCH_WALK, false);
        }

        yield return new WaitForSeconds(ignorePlatformDuration);

        if (playerCollider != null && platformToIgnore != null) // Проверка на null перед отменой IgnoreCollision
        {
            Physics2D.IgnoreCollision(playerCollider, platformToIgnore, false);
            // Debug.Log($"Больше не игнорируем платформу: {platformToIgnore.name}");
        }
        if (currentlyIgnoredColliders.Contains(platformToIgnore))
        {
            currentlyIgnoredColliders.Remove(platformToIgnore);
        }

        isDroppingThroughPlatform = false;
        // CheckIfGrounded() в следующем Update определит новое состояние
    }

    void CheckIfGrounded()
    {
        // Просто обновляем флаг isGrounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Логика сброса IsJumping здесь больше не нужна, так как она перенесена в UpdateAnimationState.
        // bool wasGrounded = isGrounded; // Это больше не нужно для анимации
        // if (!wasGrounded && isGrounded && animator != null && !animator.GetBool(ANIM_IS_JUMP_ATTACK))
        // {
        //     animator.SetBool(ANIM_IS_JUMPING, false);
        // }
    }

    void UpdateAnimationState()
    {
        if (animator == null) return; // Убрал isAttacking отсюда, т.к. атаки сами управляют своими анимациями

        // Если мы атакуем, специфическая анимация атаки должна иметь приоритет.
        // Логика ниже не должна перезаписывать анимацию атаки.
        if (isAttacking)
        {
            // Во время атаки, анимации бега, прыжка, приседания обычно не должны быть активны,
            // так как их перекрывает анимация атаки.
            // Но если атака завершилась, а isAttacking еще не сброшен корутиной,
            // то isGrounded может уже сбросить IsJumping, что хорошо.
            return;
        }

        bool wantsToCrouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.01f;

        if (isGrounded) // Если мы на земле
        {
            // Принудительно сбрасываем анимацию прыжка, если мы на земле и не в атаке в прыжке
            // (атаку в прыжке мы уже проверили выше через isAttacking, но для надежности можно еще раз)
            if (!animator.GetBool(ANIM_IS_JUMP_ATTACK))
            {
                animator.SetBool(ANIM_IS_JUMPING, false);
            }

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
                // Анимация бега включается, только если есть горизонтальный ввод
                // и мы не в какой-либо наземной атаке (isAttacking уже проверено выше,
                // но если бы не было, нужно было бы добавить:
                // !animator.GetBool(ANIM_IS_ATTACK_1) && !animator.GetBool(ANIM_IS_CROUCH_ATTACK)
                animator.SetBool(ANIM_IS_RUNNING, hasHorizontalInput);
            }
        }
        else // Если мы в воздухе
        {
            // Если мы в воздухе и не в анимации атаки в прыжке, то должна быть анимация прыжка/падения
            if (!animator.GetBool(ANIM_IS_JUMP_ATTACK))
            {
                animator.SetBool(ANIM_IS_JUMPING, true);
            }
            // В воздухе мы не бегаем и не приседаем
            animator.SetBool(ANIM_IS_RUNNING, false);
            animator.SetBool(ANIM_IS_CROUCH, false);
            animator.SetBool(ANIM_IS_CROUCH_WALK, false);
        }
    }

    void FlipSprite()
    {
        if (spriteRenderer == null) return;
        if (isAttacking && isGrounded) return; // Не поворачивать во время наземной атаки

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