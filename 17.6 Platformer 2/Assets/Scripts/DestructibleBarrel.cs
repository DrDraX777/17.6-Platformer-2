using UnityEngine;

public class DestructibleBarrel : MonoBehaviour
{
    [Header("Эффекты")]
    [Tooltip("Префаб эффекта/анимации взрыва и префаб эффектора взрыва, который появится на месте бочки.")]
    public GameObject explosionPrefab; // <--- СЮДА ТЕПЕРЬ ПЕРЕТАЩИШЬ BarrelExplosionAnimPrefab
    public GameObject explosioneffectorPrefab;
    [Header("Взаимодействие")]
    [Tooltip("Тег объекта, который может разрушить эту бочку (например, \"Arrow\").")]
    public string zerstorerTag = "Arrow";

    private bool isDestroyed = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleDestruction(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleDestruction(other.gameObject);
    }

    void HandleDestruction(GameObject otherObject)
    {
        if (isDestroyed) return;

        if (otherObject.CompareTag(zerstorerTag))
        {
            isDestroyed = true;

            if (explosionPrefab != null)
            {
                // Создаем экземпляр префаба анимации взрыва
                Instantiate(explosionPrefab, transform.position, transform.rotation);
                Instantiate(explosioneffectorPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject); // Уничтожаем саму бочку
        }
    }
}