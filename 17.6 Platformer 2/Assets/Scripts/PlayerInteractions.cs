using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Настройки Чекпоинтов")]
    [Tooltip("Список всех чекпоинтов на уровне в порядке их предполагаемого прохождения. PlayerInteractions будет использовать ID из скрипта Checkpoint.")]
    public List<Checkpoint> checkpointsOnLevel = new List<Checkpoint>(); // Список всех чекпоинтов
    private int lastActivatedCheckpointID = -1; // ID последнего активированного чекпоинта (-1 означает, что ни один еще не активирован)
    private Transform currentRespawnPoint; // Текущая активная точка респауна

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

       
        if (startPoint != null)
        {
            currentRespawnPoint = startPoint; // Начальная точка респауна по умолчанию
            initialStartPosition = startPoint.position; // Сохраняем на всякий случай
        }
        else
        {
            Debug.LogWarning("'Start Point' не назначен! Респаун на начальной позиции игрока в сцене.", this);
            initialStartPosition = transform.position;
            currentRespawnPoint = transform; // Респаун на месте старта игрока, если startPoint не указан
        }

        // Сортируем чекпоинты по их ID на всякий случай, если они добавлены в инспекторе не по порядку
        // Хотя основная логика будет полагаться на lastActivatedCheckpointID
        checkpointsOnLevel.Sort((cp1, cp2) => cp1.ID.CompareTo(cp2.ID));

        // Можно добавить логику загрузки сохраненного чекпоинта, если есть система сохранений
        // LoadLastCheckpoint(); 
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
        if (isRespawning) return; // Если уже респаунимся, не обрабатываем новые триггеры

        if (other.CompareTag("Hazard"))
        {
            HandleHazardInteraction();
        }
        else if (other.CompareTag("DeathZone")) // Если у тебя есть отдельный тег для зон мгновенной смерти
        {
            HandleHazardInteraction();
        }
        // --- НАЧАЛО НОВОГО КОДА ДЛЯ ЧЕКПОИНТОВ ---
        else if (other.CompareTag("Checkpoint")) // Убедись, что у объектов чекпоинтов есть этот тег
        {
            Checkpoint checkpoint = other.GetComponent<Checkpoint>();
            if (checkpoint != null)
            {
                TryActivateCheckpoint(checkpoint);
            }
        }
        // --- КОНЕЦ НОВОГО КОДА ДЛЯ ЧЕКПОИНТОВ ---
    }

    // Этот метод будет вызываться из скрипта Checkpoint.cs или из OnTriggerEnter2D здесь
    public bool TryActivateCheckpoint(Checkpoint checkpointToActivate)
    {
        if (checkpointToActivate == null || checkpointToActivate.IsActivated)
        {
            // Debug.Log("Попытка активировать null или уже активированный чекпоинт.");
            return false; // Чекпоинт не существует или уже активирован самим чекпоинтом
        }

        // Активируем чекпоинт только если его ID больше, чем у последнего активированного
        if (checkpointToActivate.ID > lastActivatedCheckpointID)
        {
            lastActivatedCheckpointID = checkpointToActivate.ID;
            if (checkpointToActivate.respawnTargetPoint != null)
            {
                currentRespawnPoint = checkpointToActivate.respawnTargetPoint; // Обновляем текущую точку респауна
                Debug.Log($"Новая точка респауна установлена: Чекпоинт ID {lastActivatedCheckpointID} ({currentRespawnPoint.name})");
            }
            else
            {
                Debug.Log($"Чекпоинт ID {lastActivatedCheckpointID} ({checkpointToActivate.name}) активирован, но не имеет своей точки респауна. Используется предыдущая.");
            }

            // Здесь можно добавить логику сохранения прогресса (например, PlayerPrefs.SetInt("LastCheckpointID", lastActivatedCheckpointID);)

            return true; // Чекпоинт успешно активирован
        }
        else
        {
            // Debug.Log($"Попытка активировать чекпоинт ID {checkpointToActivate.ID}, но уже активен чекпоинт с ID {lastActivatedCheckpointID} или более новым.");
            return false; // Этот чекпоинт старее или такой же, как уже активированный
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

        // 4. Перемещение игрока НА ТЕКУЩУЮ ТОЧКУ РЕСПАУНА
        if (playerRenderer != null) playerRenderer.enabled = true;

        // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
        if (currentRespawnPoint != null) // Используем currentRespawnPoint
        {
            transform.position = currentRespawnPoint.position;
        }
        else // Если currentRespawnPoint каким-то образом null, используем initialStartPosition
        {
            transform.position = initialStartPosition;
            Debug.LogWarning("currentRespawnPoint был null, респаун на initialStartPosition.");
        }
        // --- КОНЕЦ ИЗМЕНЕНИЯ ---

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

}