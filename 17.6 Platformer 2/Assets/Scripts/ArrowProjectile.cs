// ArrowProjectile.cs (Упрощенная версия)
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Взаимодействие")]
    [Tooltip("Тег объекта (например, бочки), при попадании в который стрела должна уничтожиться.")]
    public string targetTag = "Barrel"; // Тег цели, например, бочки

    // Метод OnCollisionEnter2D будет вызван, если стрела имеет Rigidbody2D и Collider2D (не триггер),
    // и она сталкивается с другим объектом, у которого есть Collider2D.
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, попали ли мы в цель с заданным тегом
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Debug.Log("Стрела попала в объект с тегом: " + targetTag + " (" + collision.gameObject.name + ")");

            // Опционально: здесь можно создать эффект попадания стрелы, если нужно
            // if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject); // Уничтожаем стрелу
        }
        // Больше никакой логики здесь нет, стрела не будет уничтожаться от других столкновений,
        // если только у этих других объектов нет своей логики для уничтожения стрел.
    }

    // Если стрела сама по себе является триггером (что менее вероятно, но возможно),
    // и должна уничтожаться при входе в триггер-объект с тегом targetTag.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            // Debug.Log("Стрела вошла в триггер объекта с тегом: " + targetTag + " (" + other.gameObject.name + ")");
            // if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject); // Уничтожаем стрелу
        }
    }
}