using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("���� ��������")]
    [Tooltip("������, �� ������� ������ ����� ��������� (������ �����).")]
    public Transform target;

    [Header("��������� ��������")]
    [Tooltip("��������� ������ ������ ����� ��������� �� �����. ������ �������� = ����� ������ ��������, ������ = ����� �������.")]
    public float smoothSpeed = 0.125f;
    [Tooltip("�������� ������ ������������ ���� �� ��� X � Y.")]
    public Vector3 offset = new Vector3(0f, 2f, -10f); // Z = -10 ���������� ��� 2D ������

    [Header("����������� �������� ������")]
    [Tooltip("�������� ����������� �� ����������� X ����������.")]
    public bool enableMinXLimit = false;
    [Tooltip("����������� X ����������, �� ������� ������ �� ������ ��������.")]
    public float minX = -5f;

    [Tooltip("�������� ����������� �� ������������ X ����������.")]
    public bool enableMaxXLimit = false;
    [Tooltip("������������ X ����������, �� ������� ������ �� ������ ��������.")]
    public float maxX = 100f;

    [Tooltip("�������� ����������� �� ����������� Y ����������.")]
    public bool enableMinYLimit = false;
    [Tooltip("����������� Y ����������, �� ������� ������ �� ������ ��������.")]
    public float minY = 0f;

    [Tooltip("�������� ����������� �� ������������ Y ����������.")]
    public bool enableMaxYLimit = false;
    [Tooltip("������������ Y ����������, �� ������� ������ �� ������ ��������.")]
    public float maxY = 20f;


    private Vector3 velocity = Vector3.zero; // ������������ ��� SmoothDamp

    void LateUpdate() // ���������� LateUpdate ��� �������� �������, ����� �������� ��������
    {
        if (target == null)
        {
            Debug.LogWarning("���� ��� �������� ������ (Target) �� ���������!");
            return;
        }

        // �������� ������� ������ = ������� ���� + ��������
        Vector3 desiredPosition = target.position + offset;

        // ������� ����������� ������ � �������� �������
        // Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime); // ������� � Lerp
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed); // ������� � SmoothDamp

        // ��������� ����������� �� X
        if (enableMinXLimit && smoothedPosition.x < minX)
        {
            smoothedPosition.x = minX;
        }
        if (enableMaxXLimit && smoothedPosition.x > maxX)
        {
            smoothedPosition.x = maxX;
        }

        // ��������� ����������� �� Y
        if (enableMinYLimit && smoothedPosition.y < minY)
        {
            smoothedPosition.y = minY;
        }
        if (enableMaxYLimit && smoothedPosition.y > maxY)
        {
            smoothedPosition.y = maxY;
        }

        // ������������� ������� ������
        // �����: ��������� ������� Z ���������� ������, ���� offset.z �� ������������ ��� �����
        // ��� ���� offset.z ��� �������� ���������� �������� Z ��� ������.
        // � ����� ������ offset.z ��� ������ ���� ���������� (��������, -10).
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, offset.z);
        // ��� transform.position.z, ���� offset.z = 0
    }

    // �����������: ������ ����� ��� ������ ������ � ���������
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // ��� ����� �� ����� �������� ���������� ������� ������� ������,
        // ��� ��� ��� ������� �� ������� ��������������� ������ ��� FOV �������������.
        // �� ��� ������� ��������������� X/Y �������.

        // ������������, ��� ������ ��������������� ��� �������� �����
        Camera cam = GetComponent<Camera>();
        float cameraHeight = cam != null && cam.orthographic ? cam.orthographicSize * 2 : 10f; // ��������� ������
        float cameraWidth = cameraHeight * (cam != null ? cam.aspect : 1.77f); // ��������� ������

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