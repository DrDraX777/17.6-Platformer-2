// Checkpoint.cs
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("Порядковый номер этого чекпоинта. Чекпоинты должны активироваться строго по возрастанию этого номера.")]
    public int checkpointID; // Уникальный ID для порядка

    [Tooltip("Точка, на которую будет установлен респаун игрока при активации этого чекпоинта. Оставьте пустым, если этот чекпоинт только для логики порядка.")]
    public Transform respawnTargetPoint;

    [Tooltip("Эффект, который проиграется при активации чекпоинта (опционально).")]
    public GameObject activationEffectPrefab;

    [Tooltip("Звук, который проиграется при активации чекпоинта (опционально).")]
    public AudioClip activationSound;

    private bool isActivated = false; // Флаг, что этот чекпоинт уже был активирован

    // Свойство только для чтения, чтобы узнать, активирован ли чекпоинт
    public bool IsActivated => isActivated;
    public int ID => checkpointID; // Свойство для доступа к ID

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return; // Если уже активирован, ничего не делаем

        if (other.CompareTag("Player")) // Убедись, что у игрока есть тег "Player"
        {
            PlayerInteractions playerInteractions = other.GetComponent<PlayerInteractions>();
            if (playerInteractions != null)
            {
                // Пытаемся активировать этот чекпоинт через PlayerInteractions
                // PlayerInteractions сам решит, можно ли его активировать (на основе ID)
                bool activatedSuccessfully = playerInteractions.TryActivateCheckpoint(this);

                if (activatedSuccessfully)
                {
                    isActivated = true; // Помечаем этот чекпоинт как активированный
                    PerformActivationFeedback();

                    // Опционально: можно деактивировать коллайдер чекпоинта, чтобы он не срабатывал повторно визуально/логически
                    // Collider2D col = GetComponent<Collider2D>();
                    // if (col != null) col.enabled = false;
                    // Или даже весь GameObject: gameObject.SetActive(false); (но тогда эффекты не проиграются, если они на нем)
                }
            }
        }
    }

    void PerformActivationFeedback()
    {
        Debug.Log($"Чекпоинт {checkpointID} ({gameObject.name}) активирован!");

        if (activationEffectPrefab != null)
        {
            Instantiate(activationEffectPrefab, transform.position, Quaternion.identity);
        }

        if (activationSound != null && Camera.main != null) // Проигрываем звук на камере для 2D
        {
            AudioSource.PlayClipAtPoint(activationSound, Camera.main.transform.position);
        }
        // Дополнительно: можно изменить спрайт чекпоинта, запустить анимацию и т.д.
    }

    // Для визуализации в редакторе
    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // Сам чекпоинт
        if (respawnTargetPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(respawnTargetPoint.position, 0.3f); // Точка респауна
            Gizmos.DrawLine(transform.position, respawnTargetPoint.position);
        }
    }
}