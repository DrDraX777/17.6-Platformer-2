using UnityEngine;
using System.Collections;

public class PlayerInteractions : MonoBehaviour
{
    // ... (все переменные остаются такими же, как в предыдущем варианте) ...
    [Header("Точка Респауна")]
    [Tooltip("Объект, на позицию которого игрок будет перемещен при респауне.")]
    public Transform startPoint;

    [Header("Настройки Респауна")]
    [Tooltip("Сколько раз игрок моргнет (вкл/выкл)")]
    public int flashCount = 3;
    [Tooltip("Длительность каждого этапа моргания (в секундах)")]
    public float flashDuration = 0.15f;
    // Убираем delayBeforeRespawn, так как длительность анимации смерти будет основным ожиданием
    // public float delayBeforeRespawn = 0.5f; 
    [Tooltip("Примерная длительность анимации смерти в секундах. Используется для ожидания.")]
    public float deathAnimationActualDuration = 1.0f; // <--- ВАЖНО: Настрой это значение!

    private Renderer playerRenderer;
    private Rigidbody2D playerRigidbody;
    private MonoBehaviour playerControllerScript;
    private Animator playerAnimator;

    private const string ANIM_IS_DEATH = "IsDeath";

    private bool isRespawning = false;
    private Vector3 initialStartPosition;

    void Awake()
    {
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();

        playerRigidbody = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();

        playerControllerScript = GetComponent<KnightController>(); // <--- ЗАМЕНИ НА ИМЯ ТВОЕГО СКРИПТА

        if (playerAnimator == null) Debug.LogWarning("Animator не найден. Анимация смерти не будет работать.", this);
        if (playerControllerScript == null) Debug.LogWarning("Скрипт контроллера игрока не найден. Отключение управления может не работать.", this);
        if (playerRenderer == null) Debug.LogError("Renderer не найден! Моргание не будет работать.", this);

        if (startPoint == null)
        {
            Debug.LogWarning("'Start Point' не назначен! Респаун на начальной позиции.", this);
            initialStartPosition = transform.position;
        }
    }

    // Оставляем только OnTriggerEnter2D, если вода - это триггер
    // Если есть и твердые опасности, OnCollisionEnter2D тоже нужен
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Если это не вода, а, например, шипы (твердый объект с тегом Hazard)
        if (collision.gameObject.CompareTag("Hazard") && collision.gameObject.GetComponent<BuoyancyEffector2D>() == null)
        {
            // Проверяем, что это не объект с BuoyancyEffector2D, чтобы не дублировать логику
            HandleHazardInteraction();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Этот метод сработает для триггер-зоны воды с тегом "Hazard"
        if (other.CompareTag("Hazard"))
        {
            HandleHazardInteraction();
        }
        if (other.CompareTag("DeathZone"))
        {
            HandleHazardInteraction();
        }
    }

    void HandleHazardInteraction()
    {
        if (isRespawning) return;

        Debug.Log("Контакт с Hazard! Начинаем процесс смерти и респауна...");
        StartCoroutine(DeathAndRespawnCoroutine());
    }

    IEnumerator DeathAndRespawnCoroutine()
    {
        isRespawning = true;

        // 1. НЕМЕДЛЕННО остановить игрока и отключить управление
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.isKinematic = true; // Останавливаем физическое движение
        }

        // 2. НЕМЕДЛЕННО ЗАПУСТИТЬ АНИМАЦИЮ СМЕРТИ
        // и ПРИНУДИТЕЛЬНО ПЕРЕЙТИ В НЕЕ, если аниматор еще не там
        if (playerAnimator != null)
        {
            // Сначала сбрасываем все другие потенциальные состояния движения, чтобы они не мешали
            // Это важно, если у тебя есть переходы с высокой приоритетностью из Any State в Jump/Fall
            playerAnimator.SetBool("IsJumping", false); // Используй точное имя твоего параметра прыжка/падения
            playerAnimator.SetBool("IsRunning", false); // Имя параметра бега
            playerAnimator.SetBool("IsCrouch", false);  // Имя параметра приседания
            // Добавь сюда сброс других релевантных булевых параметров анимаций движения

            // Устанавливаем флаг смерти
            playerAnimator.SetBool(ANIM_IS_DEATH, true);
            Debug.Log("Установлен флаг анимации смерти: " + ANIM_IS_DEATH);

            // Даем аниматору один кадр на обработку изменения параметров
            // и потенциальное начало перехода в состояние смерти.
            yield return null;

            // Проверка, действительно ли мы в состоянии смерти. Если нет - можно попробовать Play.
            // Это более "жесткий" способ, но может помочь, если переходы настроены сложно.
            // Однако, если переходы из Any State в Death настроены правильно (без Exit Time, по флагу IsDeath),
            // то этого обычно не требуется.
            /*
            if (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("YourDeathStateName")) // ЗАМЕНИ "YourDeathStateName"
            {
                playerAnimator.Play("YourDeathStateName", 0, 0f); // ЗАМЕНИ "YourDeathStateName" на имя состояния смерти
                Debug.Log("Принудительный запуск анимации смерти через Play.");
                yield return null; // Еще кадр на применение Play
            }
            */

            // ОЖИДАНИЕ ЗАВЕРШЕНИЯ АНИМАЦИИ СМЕРТИ
            if (deathAnimationActualDuration > 0)
            {
                // Если игрок должен быть виден во время анимации смерти, убедимся, что он видим
                if (playerRenderer != null) playerRenderer.enabled = true;
                yield return new WaitForSeconds(deathAnimationActualDuration);
            }
            Debug.Log("Анимация смерти должна была завершиться.");
        }
        else // Если аниматора нет
        {
            yield return new WaitForSeconds(0.5f); // Небольшая пауза по умолчанию
        }

        // 3. СБРОСИТЬ ФЛАГ АНИМАЦИИ СМЕРТИ и подготовиться к Idle
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(ANIM_IS_DEATH, false);
            // Animator Controller должен сам перейти в Idle (или состояние по умолчанию)
            yield return null; // Даем кадр на обновление аниматора
        }

        // 4. Перемещение игрока НА ТОЧКУ РЕСПАУНА
        // (Если игрок был невидим до этого, он станет видимым здесь, но по текущей логике он видим)
        if (playerRenderer != null) playerRenderer.enabled = true;

        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
        else
        {
            transform.position = initialStartPosition;
        }

        // 5. ВОССТАНОВИТЬ ФИЗИКУ перед морганием
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // 6. Моргание
        if (playerRenderer != null)
        {
            for (int i = 0; i < flashCount; i++)
            {
                playerRenderer.enabled = false;
                yield return new WaitForSeconds(flashDuration);
                playerRenderer.enabled = true;
                yield return new WaitForSeconds(flashDuration);
            }
            playerRenderer.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(flashCount * flashDuration * 2);
        }

        // 7. Включить управление игроком обратно
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = true;
        }

        isRespawning = false;
        Debug.Log("Респаун завершен, игрок в Idle.");
    
}

    public void SetNewStartPoint(Transform newStartPoint)
    {
        if (newStartPoint != null)
        {
            startPoint = newStartPoint;
            Debug.Log("Новая точка респауна установлена: " + newStartPoint.name);
        }
        else
        {
            Debug.LogWarning("Попытка установить пустую точку респауна.");
        }
    }
}