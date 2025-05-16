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
    public float jumpAttackDuration = 0.4f; // Может быть короче или длиннее
    private bool isAttacking = false;

    [Header("Проверка Земли")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Настройки Прыжка Вниз")]
    [Tooltip("Как долго игнорировать платформу после прыжка вниз (в секундах)")]
    public float ignorePlatformDuration = 0.3f;
    private bool isDroppingThroughPlatform = false; // Флаг, что мы в процессе падения
    private Collider2D playerCollider; // Коллайдер игрока
    private List<Collider2D> currentlyIgnoredColliders = new List<Collider2D>(); // Для хранения временно игнорируемых коллайдеров

    [Header("Взаимодействие с Платформами")]
    [Tooltip("Слой, на котором находятся движущиеся платформы (например, твой ящик)")]
    public LayerMask movingPlatformLayer; // <--- НОВАЯ ПЕРЕМЕННАЯ для слоя движущихся платформ

    private Transform currentMovingPlatform; // Платформа, на которой мы стоим
    private Vector3 lastPlatformPosition;    // Позиция платформы в предыдущем кадре
    private Rigidbody2D platformRigidbody;   // Rigidbody платформы (если есть)




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
        playerCollider = GetComponent<Collider2D>(); // <--- ДОБАВЬ ЭТУ СТРОКУ

        if (playerCollider == null) { Debug.LogError("Основной Collider2D игрока не найден!", this); enabled = false; return; } // <--- И ПРОВЕРКУ
        if (rb == null) { Debug.LogError("Rigidbody2D не найден!", this); enabled = false; return; }
        if (animator == null) { Debug.LogWarning("Animator не найден. Анимации не будут работать.", this); }
        if (spriteRenderer == null) { Debug.LogWarning("SpriteRenderer не найден. Поворот спрайта может не работать.", this); }
        if (groundCheck == null) { Debug.LogError("'Ground Check' не назначен!", this); enabled = false; return; }
        if (groundLayer.value == 0) { Debug.LogWarning("'Ground Layer' не назначен. Проверка земли может не работать.", this); }
    }

    void Update()
    {
        // Если мы уже в какой-либо атаке, прерываем обработку нового ввода для атаки/движения
        if (isAttacking || isDroppingThroughPlatform) // <--- ДОБАВЬ isDroppingThroughPlatform ЗДЕСЬ
        {
            // Если атакуем или падаем сквозь платформу, не обрабатываем новый ввод для движения/атаки
            // Но корутина DropThroughPlatformCoroutine должна продолжать работать
            if (isAttacking && isDroppingThroughPlatform) { /* Позволяем обоим состояниям существовать, если нужно */ }
            else if (isAttacking) { return; }
            // Если isDroppingThroughPlatform, Update все равно должен выполняться для CheckIfGrounded, но ввод ниже блокируется
        }

        CheckIfGrounded();
        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool isCurrentlyCrouching = (animator != null && animator.GetBool(ANIM_IS_CROUCH));
        bool wantsToCrouchInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow); // Получаем ввод приседания
        // Обработка прыжка
        // --- НАЧАЛО НОВОГО КОДА ДЛЯ ПРЫЖКА ВНИЗ ---
        if (wantsToCrouchInput && Input.GetButtonDown("Jump") && isGrounded && !isAttacking && !isDroppingThroughPlatform)
        {
            // Попытка прыгнуть вниз
            TryDropThroughPlatform();
        }
        // --- КОНЕЦ НОВОГО КОДА ДЛЯ ПРЫЖКА ВНИЗ ---
        // Обычный прыжок, если не зажато приседание (или если TryDropThroughPlatform не сработал и вернул управление)
        // и если мы не в процессе isDroppingThroughPlatform
        else if (Input.GetButtonDown("Jump") && isGrounded && !isCurrentlyCrouching && !isAttacking && !isDroppingThroughPlatform)
        {
            Jump();
        }

        // Обработка АТАКИ (ЛКМ - Fire1 по умолчанию)
        if (Input.GetButtonDown("Fire1") && !isAttacking && !isDroppingThroughPlatform) // <--- ДОБАВЬ isDroppingThroughPlatform
        {
            HandleAttack();
        }

        if (!isDroppingThroughPlatform) // <--- ДОБАВЬ ПРОВЕРКУ
        {
            UpdateAnimationState();
            FlipSprite();
        }
    }

    void FixedUpdate()
    {
        Vector3 platformDeltaMovement = Vector3.zero;

        // Рассчитываем смещение платформы, если мы на ней стоим
        if (currentMovingPlatform != null && isGrounded) // Добавил isGrounded для большей надежности
        {
            if (platformRigidbody != null)
            {
                // Если у платформы есть Rigidbody, используем его velocity для более точного предсказания движения
                // Это лучше работает для платформ, движущихся через физику (AddForce, velocity)
                platformDeltaMovement = platformRigidbody.linearVelocity * Time.fixedDeltaTime;

                // Однако, если платформа движется через изменение transform.position или анимацию,
                // то velocity может быть неточным или нулевым.
                // В таком случае, лучше использовать разницу позиций:
                // platformDeltaMovement = currentMovingPlatform.position - lastPlatformPosition;
                // lastPlatformPosition = currentMovingPlatform.position;
                // Можно сделать гибридный подход или выбрать один. Для ящика, движимого Buoyancy Effector, velocity должно быть ОК.
            }
            else // Если у платформы нет Rigidbody (например, анимированная платформа)
            {
                platformDeltaMovement = currentMovingPlatform.position - lastPlatformPosition;
            }
            lastPlatformPosition = currentMovingPlatform.position; // Обновляем позицию платформы для следующего кадра
        }


        // Логика обработки атаки и прыжка вниз (остается прежней)
        if (isAttacking || isDroppingThroughPlatform)
        {
            if (isGrounded && !isDroppingThroughPlatform)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            return;
        }

        // Движение игрока
        float currentAppliedSpeed = moveSpeed;
        bool isCurrentlyCrouchingOnGround = (animator != null && animator.GetBool(ANIM_IS_CROUCH) && isGrounded);

        if (isCurrentlyCrouchingOnGround)
        {
            if (canMoveWhileCrouching)
            {
                currentAppliedSpeed *= crouchMoveSpeedFactor;
            }
            else if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                currentAppliedSpeed = 0;
            }
        }

        // Применяем собственное движение игрока
        Vector2 playerVelocity = new Vector2(horizontalInput * currentAppliedSpeed, rb.linearVelocity.y);
        rb.linearVelocity = playerVelocity; // Устанавливаем скорость игрока

        // ПРИМЕНЯЕМ ДВИЖЕНИЕ ПЛАТФОРМЫ К ИГРОКУ
        // Мы должны перемещать игрока напрямую, а не через velocity, чтобы избежать наложения скоростей
        // и странного поведения физики, особенно если трение низкое.
        if (currentMovingPlatform != null && isGrounded && platformDeltaMovement != Vector3.zero)
        {
            // Перемещаем игрока на дельту движения платформы
            // Этот способ лучше, чем пытаться добавить скорость, так как он не зависит от массы игрока
            // и лучше работает с кинематическими платформами или платформами с низкой инерцией.
            transform.position += platformDeltaMovement;

            // Важное замечание: если платформа движется очень быстро, могут быть проблемы с прохождением сквозь коллайдеры.
            // Rigidbody.MovePosition() может быть более безопасным, но transform.position += ... обычно работает для умеренных скоростей.
            // rb.MovePosition(rb.position + (Vector2)platformDeltaMovement);
        }
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

    void TryDropThroughPlatform()
    {
        // Проверяем, стоим ли мы на платформе, через которую можно провалиться
        // Для этого найдем все коллайдеры под ногами на слое groundLayer
        Collider2D[] collidersUnderneath = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);

        Collider2D platformToIgnore = null;
        foreach (Collider2D col in collidersUnderneath)
        {
            // Ищем платформу с компонентом Platform Effector 2D
            if (col.GetComponent<PlatformEffector2D>() != null)
            {
                // Дополнительная проверка: убедимся, что это не тот же коллайдер, что и у игрока (на всякий случай)
                if (col != playerCollider)
                {
                    platformToIgnore = col;
                    break; // Нашли первую подходящую платформу
                }
            }
        }

        if (platformToIgnore != null)
        {
            StartCoroutine(DropThroughPlatformCoroutine(platformToIgnore));
        }
        // Если подходящая платформа не найдена, ничего не делаем (можно добавить звук "нельзя" и т.п.)
    }

    IEnumerator DropThroughPlatformCoroutine(Collider2D platformCollider)
    {
        isDroppingThroughPlatform = true;

        // Сохраняем коллайдер, чтобы восстановить взаимодействие позже
        currentlyIgnoredColliders.Add(platformCollider);
        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        Debug.Log($"Игнорируем платформу: {platformCollider.name}");

        // Опционально: небольшая задержка или небольшой импульс вниз
        // rb.AddForce(Vector2.down * 0.1f, ForceMode2D.Impulse); // Очень маленький, чтобы просто "отлепиться"

        // Установим анимацию падения (если нужно)
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_JUMPING, true); // Активируем анимацию прыжка/падения
            animator.SetBool(ANIM_IS_CROUCH, false); // Выходим из приседания, если были в нем
            animator.SetBool(ANIM_IS_CROUCH_WALK, false);
        }

        yield return new WaitForSeconds(ignorePlatformDuration);

        // Восстанавливаем коллизию
        // Важно проверить, что объекты еще существуют, на случай их уничтожения
        if (playerCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            Debug.Log($"Больше не игнорируем платформу: {platformCollider.name}");
        }
        currentlyIgnoredColliders.Remove(platformCollider); // Удаляем из списка (или очищаем весь список, если логика другая)

        isDroppingThroughPlatform = false;
        // После завершения, CheckIfGrounded в следующем Update сам определит, на земле мы или нет.
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
        bool wasGrounded = isGrounded;
        Transform previousPlatform = currentMovingPlatform; // Запоминаем предыдущую платформу

        // Используем Physics2D.OverlapCircle, чтобы получить коллайдер объекта под ногами
        Collider2D groundCollider = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = groundCollider != null;

        if (isGrounded)
        {
            // Проверяем, является ли объект под ногами движущейся платформой
            // Сравниваем слой объекта с нашей маской movingPlatformLayer
            if (((1 << groundCollider.gameObject.layer) & movingPlatformLayer) != 0)
            {
                currentMovingPlatform = groundCollider.transform;
                platformRigidbody = groundCollider.GetComponent<Rigidbody2D>(); // Пытаемся получить Rigidbody платформы
            }
            else
            {
                currentMovingPlatform = null;
                platformRigidbody = null;
            }
        }
        else
        {
            currentMovingPlatform = null;
            platformRigidbody = null;
        }

        // Если мы только что приземлились на новую платформу (или на ту же, но были в воздухе)
        if (currentMovingPlatform != null && currentMovingPlatform != previousPlatform)
        {
            lastPlatformPosition = currentMovingPlatform.position;
        }
        // Если мы сошли с платформы
        if (currentMovingPlatform == null && previousPlatform != null)
        {
            // Здесь можно добавить логику "отвязки", если нужно (например, если мы делали игрока дочерним)
            // В нашем случае, просто currentMovingPlatform = null уже достаточно
        }


        // Стандартная логика сброса анимации прыжка
        if (!wasGrounded && isGrounded && animator != null && !animator.GetBool(ANIM_IS_JUMP_ATTACK))
        {
            animator.SetBool(ANIM_IS_JUMPING, false);
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