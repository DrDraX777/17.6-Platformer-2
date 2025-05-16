using UnityEngine;

[RequireComponent(typeof(SliderJoint2D))] // Гарантирует наличие SliderJoint2D
public class PlatformMotorControl : MonoBehaviour
{
    private SliderJoint2D sliderJoint;
    private JointMotor2D motor;

    [Tooltip("Начальная скорость мотора. Положительное значение для движения к Upper Limit, отрицательное - к Lower Limit.")]
    public float initialMotorSpeed = 2f;

    void Awake()
    {
        sliderJoint = GetComponent<SliderJoint2D>();
        if (sliderJoint == null)
        {
            Debug.LogError("SliderJoint2D не найден на объекте! Скрипт не будет работать.", this);
            enabled = false;
            return;
        }

        // Сохраняем текущие настройки мотора
        motor = sliderJoint.motor;
        // Устанавливаем начальную скорость
        motor.motorSpeed = initialMotorSpeed;
        sliderJoint.motor = motor; // Применяем измененный мотор обратно к джоинту
    }

    void Update() // Можно использовать FixedUpdate, если есть проблемы с точностью определения пределов
    {
        if (sliderJoint == null || !sliderJoint.useMotor || !sliderJoint.useLimits)
        {
            return; // Ничего не делаем, если джоинт настроен неправильно
        }

        // Получаем текущее смещение джоинта
        float currentTranslation = sliderJoint.jointTranslation;

        // Проверяем, достигли ли мы одного из пределов
        // Используем небольшую погрешность (epsilon), чтобы избежать проблем с точностью float

        // Если мотор движет платформу к верхнему пределу (скорость положительная)
        // и мы достигли или превысили верхний предел
        if (motor.motorSpeed > 0 && currentTranslation >= sliderJoint.limits.max - 0.01f)
        {
            motor.motorSpeed = -Mathf.Abs(initialMotorSpeed); // Меняем направление на противоположное (к нижнему пределу)
            sliderJoint.motor = motor;
            // Debug.Log("Достигнут верхний предел, меняем направление вниз.");
        }
        // Если мотор движет платформу к нижнему пределу (скорость отрицательная)
        // и мы достигли или опустились ниже нижнего предела
        else if (motor.motorSpeed < 0 && currentTranslation <= sliderJoint.limits.min + 0.01f)
        {
            motor.motorSpeed = Mathf.Abs(initialMotorSpeed); // Меняем направление на противоположное (к верхнему пределу)
            sliderJoint.motor = motor;
            // Debug.Log("Достигнут нижний предел, меняем направление вверх.");
        }
    }
}