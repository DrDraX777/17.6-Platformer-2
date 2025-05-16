using UnityEngine;

[RequireComponent(typeof(SliderJoint2D))] // ����������� ������� SliderJoint2D
public class PlatformMotorControl : MonoBehaviour
{
    private SliderJoint2D sliderJoint;
    private JointMotor2D motor;

    [Tooltip("��������� �������� ������. ������������� �������� ��� �������� � Upper Limit, ������������� - � Lower Limit.")]
    public float initialMotorSpeed = 2f;

    void Awake()
    {
        sliderJoint = GetComponent<SliderJoint2D>();
        if (sliderJoint == null)
        {
            Debug.LogError("SliderJoint2D �� ������ �� �������! ������ �� ����� ��������.", this);
            enabled = false;
            return;
        }

        // ��������� ������� ��������� ������
        motor = sliderJoint.motor;
        // ������������� ��������� ��������
        motor.motorSpeed = initialMotorSpeed;
        sliderJoint.motor = motor; // ��������� ���������� ����� ������� � �������
    }

    void Update() // ����� ������������ FixedUpdate, ���� ���� �������� � ��������� ����������� ��������
    {
        if (sliderJoint == null || !sliderJoint.useMotor || !sliderJoint.useLimits)
        {
            return; // ������ �� ������, ���� ������ �������� �����������
        }

        // �������� ������� �������� �������
        float currentTranslation = sliderJoint.jointTranslation;

        // ���������, �������� �� �� ������ �� ��������
        // ���������� ��������� ����������� (epsilon), ����� �������� ������� � ��������� float

        // ���� ����� ������ ��������� � �������� ������� (�������� �������������)
        // � �� �������� ��� ��������� ������� ������
        if (motor.motorSpeed > 0 && currentTranslation >= sliderJoint.limits.max - 0.01f)
        {
            motor.motorSpeed = -Mathf.Abs(initialMotorSpeed); // ������ ����������� �� ��������������� (� ������� �������)
            sliderJoint.motor = motor;
            // Debug.Log("��������� ������� ������, ������ ����������� ����.");
        }
        // ���� ����� ������ ��������� � ������� ������� (�������� �������������)
        // � �� �������� ��� ���������� ���� ������� �������
        else if (motor.motorSpeed < 0 && currentTranslation <= sliderJoint.limits.min + 0.01f)
        {
            motor.motorSpeed = Mathf.Abs(initialMotorSpeed); // ������ ����������� �� ��������������� (� �������� �������)
            sliderJoint.motor = motor;
            // Debug.Log("��������� ������ ������, ������ ����������� �����.");
        }
    }
}