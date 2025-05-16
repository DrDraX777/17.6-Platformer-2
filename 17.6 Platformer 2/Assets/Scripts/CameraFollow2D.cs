using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Цель Слежения")]
    [Tooltip("Объект, за которым камера будет следовать (обычно игрок).")]
    public Transform target;

    [Header("Параметры Слежения")]
    [Tooltip("Насколько плавно камера будет следовать за целью. Меньше значение = более резкое движение, больше = более плавное.")]
    public float smoothSpeed = 0.125f;
    [Tooltip("Смещение камеры относительно цели по оси X и Y.")]
    public Vector3 offset = new Vector3(0f, 2f, -10f); // Z = -10 стандартно для 2D камеры

    [Header("Ограничения Движения Камеры")]
    [Tooltip("Включить ограничение по минимальной X координате.")]
    public bool enableMinXLimit = false;
    [Tooltip("Минимальная X координата, за которую камера не должна заходить.")]
    public float minX = -5f;

    [Tooltip("Включить ограничение по максимальной X координате.")]
    public bool enableMaxXLimit = false;
    [Tooltip("Максимальная X координата, за которую камера не должна заходить.")]
    public float maxX = 100f;

    [Tooltip("Включить ограничение по минимальной Y координате.")]
    public bool enableMinYLimit = false;
    [Tooltip("Минимальная Y координата, за которую камера не должна заходить.")]
    public float minY = 0f;

    [Tooltip("Включить ограничение по максимальной Y координате.")]
    public bool enableMaxYLimit = false;
    [Tooltip("Максимальная Y координата, за которую камера не должна заходить.")]
    public float maxY = 20f;


    private Vector3 velocity = Vector3.zero; // Используется для SmoothDamp

    void LateUpdate() // Используем LateUpdate для слежения камерой, чтобы избежать дрожания
    {
        if (target == null)
        {
            Debug.LogWarning("Цель для слежения камеры (Target) не назначена!");
            return;
        }

        // Желаемая позиция камеры = позиция цели + смещение
        Vector3 desiredPosition = target.position + offset;

        // Плавное перемещение камеры к желаемой позиции
        // Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime); // Вариант с Lerp
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed); // Вариант с SmoothDamp

        // Применяем ограничения по X
        if (enableMinXLimit && smoothedPosition.x < minX)
        {
            smoothedPosition.x = minX;
        }
        if (enableMaxXLimit && smoothedPosition.x > maxX)
        {
            smoothedPosition.x = maxX;
        }

        // Применяем ограничения по Y
        if (enableMinYLimit && smoothedPosition.y < minY)
        {
            smoothedPosition.y = minY;
        }
        if (enableMaxYLimit && smoothedPosition.y > maxY)
        {
            smoothedPosition.y = maxY;
        }

        // Устанавливаем позицию камеры
        // Важно: сохраняем текущую Z координату камеры, если offset.z не используется для этого
        // или если offset.z уже содержит правильное значение Z для камеры.
        // В нашем случае offset.z уже должен быть правильным (например, -10).
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, offset.z);
        // или transform.position.z, если offset.z = 0
    }

    // Опционально: Рисуем гизмо для границ камеры в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // Эти гизмо не будут идеально отображать видимую область камеры,
        // так как это зависит от размера ортографической камеры или FOV перспективной.
        // Но они помогут визуализировать X/Y границы.

        // Предполагаем, что камера ортографическая для простоты гизмо
        Camera cam = GetComponent<Camera>();
        float cameraHeight = cam != null && cam.orthographic ? cam.orthographicSize * 2 : 10f; // Примерная высота
        float cameraWidth = cameraHeight * (cam != null ? cam.aspect : 1.77f); // Примерная ширина

        Vector3 minBounds = new Vector3(minX, minY, 0);
        Vector3 maxBounds = new Vector3(maxX, maxY, 0);

        if (enableMinXLimit)
            Gizmos.DrawLine(new Vector3(minX, minY - cameraHeight / 2, 0), new Vector3(minX, maxY + cameraHeight / 2, 0));
        if (enableMaxXLimit)
            Gizmos.DrawLine(new Vector3(maxX, minY - cameraHeight / 2, 0), new Vector3(maxX, maxY + cameraHeight / 2, 0));
        if (enableMinYLimit)
            Gizmos.DrawLine(new Vector3(minX - cameraWidth / 2, minY, 0), new Vector3(maxX + cameraWidth / 2, minY, 0));
        if (enableMaxYLimit)
            Gizmos.DrawLine(new Vector3(minX - cameraWidth / 2, maxY, 0), new Vector3(maxX + cameraWidth / 2, maxY, 0));
    }
}