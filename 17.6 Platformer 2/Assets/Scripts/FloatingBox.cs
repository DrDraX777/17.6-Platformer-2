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

    // FloatingBox.cs
    void Respawn()
    {
        Debug.Log(gameObject.name + " упал в DeathZone. Респаун...");

        // --- НАЧАЛО НОВОГО КОДА: ОТКРЕПЛЕНИЕ ДОЧЕРНИХ ИГРОКОВ ---
        // Перебираем всех прямых дочерних объектов
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            // Проверяем, является ли дочерний объект игроком (по тегу или компоненту)
            if (child.CompareTag("Player")) // Убедись, что у твоего игрока есть тег "Player"
            {
                // Открепляем игрока
                child.SetParent(null);
                Debug.Log("Игрок " + child.name + " откреплен от ящика перед респауном ящика.");

                // Опционально: можно попытаться безопасно "сбросить" игрока с ящика,
                // например, придав ему небольшой импульс или переместив на ближайшую безопасную точку,
                // но простое открепление уже предотвратит телепортацию вместе с ящиком.
                // Игрок просто упадет дальше под действием гравитации.
            }
            // Если нужно проверять по компоненту:
            // KnightController playerOnBox = child.GetComponent<KnightController>();
            // if (playerOnBox != null) {
            //     child.SetParent(null);
            // }
        }
        // --- КОНЕЦ НОВОГО КОДА ---

        Vector3 respawnPosition = (spawnPoint != null) ? spawnPoint.position : initialPosition;
        transform.position = respawnPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
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