// AutoDestroyEffect.cs (остается без изменений)
using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    [Tooltip("Время жизни объекта эффекта в секундах.")]
    public float lifetime = 1.0f; // Настрой в инспекторе!

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}