using UnityEngine;
using System.Collections.Generic; // Все еще нужен, если ты хочешь сбрасывать скорость колес

public class CarriageRespawner : MonoBehaviour // Этот скрипт по-прежнему на объекте КАРЕТЫ
{
    [Header("Объекты для Респауна")]
    [Tooltip("Тело кареты (этот самый GameObject, если скрипт на нем). Можно оставить пустым.")]
    public Transform carriageBodyTransform; // Transform кареты

    // Список Rigidbody2D колес НУЖЕН, если мы хотим сбрасывать их скорость.
    // Если сброс скорости колес не важен, этот список можно убрать.
    [Tooltip("Список Rigidbody2D колес (если нужно сбрасывать их скорость). Колеса должны быть ДОЧЕРНИМИ карете.")]
    public List<Rigidbody2D> childWheelsRbs;

    [Header("Точка Респауна")]
    [Tooltip("Пустой GameObject, определяющий базовую точку и ориентацию для респауна кареты.")]
    public Transform respawnPoint;

    private Rigidbody2D carriageRb;

    // Переменные для хранения начальных относительных позиций колес БОЛЬШЕ НЕ НУЖНЫ,
    // так как иерархия сама их сохраняет.
    // private Vector3 initialCarriageOffsetFromRespawn; // Это все еще может быть полезно, если respawnPoint - это только точка, а не точная копия начального состояния кареты
    // private Quaternion initialCarriageRotationOffsetFromRespawn;

    void Awake()
    {
        if (carriageBodyTransform == null)
        {
            carriageBodyTransform = transform;
        }
        carriageRb = carriageBodyTransform.GetComponent<Rigidbody2D>();

        if (respawnPoint == null)
        {
            Debug.LogError("Точка респауна (Respawn Point) не назначена! Респаун не будет работать.", this);
            enabled = false;
            return;
        }

        if (childWheelsRbs == null || childWheelsRbs.Count == 0)
        {
            Debug.LogWarning("Rigidbody2D колес не назначены в списке childWheelsRbs. Скорость колес не будет сбрасываться при респауне.", this);
        }

        // Запоминать начальные смещения кареты относительно respawnPoint НЕ ОБЯЗАТЕЛЬНО,
        // если ты будешь просто устанавливать transform.position и transform.rotation кареты равными respawnPoint.
        // Но если respawnPoint - это лишь "якорь", а карета должна иметь смещение относительно него, то это полезно.
        // Для простоты предположим, что respawnPoint - это точное место и ориентация кареты.
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeathZone"))
        {
            RespawnCarriage();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("DeathZone"))
        {
            RespawnCarriage();
        }
    }

    void RespawnCarriage()
    {
        Debug.Log(carriageBodyTransform.name + " вошла в DeathZone. Респаун...");

        if (respawnPoint == null) return;

        // 1. Респаун Кареты (родительского объекта)
        // Просто устанавливаем позицию и вращение кареты равными respawnPoint
        carriageBodyTransform.position = respawnPoint.position;
        carriageBodyTransform.rotation = respawnPoint.rotation;

        if (carriageRb != null)
        {
            carriageRb.linearVelocity = Vector2.zero;
            carriageRb.angularVelocity = 0f;
        }

        // 2. Сброс Скорости Колес (если они назначены в списке)
        // Колеса уже переместились и повернулись вместе с каретой, так как они дочерние.
        // Нам нужно только сбросить их физические скорости.
        if (childWheelsRbs != null)
        {
            foreach (Rigidbody2D wheelRb in childWheelsRbs)
            {
                if (wheelRb != null)
                {
                    wheelRb.linearVelocity = Vector2.zero;
                    wheelRb.angularVelocity = 0f;
                }
            }
        }
        Debug.Log("Карета (и ее дочерние колеса) респаунены.");
    }
}