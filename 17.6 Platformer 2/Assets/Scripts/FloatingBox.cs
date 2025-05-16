using UnityEngine;

public class FloatingBox : MonoBehaviour
{
    [Header("Настройки Респауна")]
    [Tooltip("Точка, где ящик будет появляться снова.")]
    public Transform spawnPoint;

    private Rigidbody2D rb;
    private Vector3 initialPosition; // Запасная начальная позиция

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D не найден на ящике! Скрипт не будет работать.", this);
            enabled = false; // Отключаем скрипт, если нет Rigidbody2D
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("Точка респауна (Spawn Point) не назначена для ящика! " +
                             "Ящик будет использовать свою начальную позицию при старте сцены для респауна.", this);
            initialPosition = transform.position; // Запоминаем начальную позицию как точку респауна по умолчанию
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, вошел ли ящик в зону с тегом "DeathZone"
        if (other.CompareTag("DeathZone"))
        {
            Respawn();
        }
    }

    void Respawn()
    {
        Debug.Log(gameObject.name + " упал в DeathZone. Респаун...");

        // Определяем позицию для респауна
        Vector3 respawnPosition = (spawnPoint != null) ? spawnPoint.position : initialPosition;

        // Перемещаем ящик
        transform.position = respawnPosition;

        // Сбрасываем скорость и вращение Rigidbody2D
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Опционально: можно добавить здесь какие-либо другие действия при респауне,
        // например, проиграть звук или эффект частиц.
    }

    // Для отладки, чтобы видеть точку респауна в редакторе
    void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
        else if (Application.isPlaying) // Показываем initialPosition только во время игры, если spawnPoint не назначен
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(initialPosition, 0.4f);
        }
    }
}