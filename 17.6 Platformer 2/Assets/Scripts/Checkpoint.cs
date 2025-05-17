// Checkpoint.cs
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("���������� ����� ����� ���������. ��������� ������ �������������� ������ �� ����������� ����� ������.")]
    public int checkpointID; // ���������� ID ��� �������

    [Tooltip("�����, �� ������� ����� ���������� ������� ������ ��� ��������� ����� ���������. �������� ������, ���� ���� �������� ������ ��� ������ �������.")]
    public Transform respawnTargetPoint;

    [Tooltip("������, ������� ����������� ��� ��������� ��������� (�����������).")]
    public GameObject activationEffectPrefab;

    [Tooltip("����, ������� ����������� ��� ��������� ��������� (�����������).")]
    public AudioClip activationSound;

    private bool isActivated = false; // ����, ��� ���� �������� ��� ��� �����������

    // �������� ������ ��� ������, ����� ������, ����������� �� ��������
    public bool IsActivated => isActivated;
    public int ID => checkpointID; // �������� ��� ������� � ID

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return; // ���� ��� �����������, ������ �� ������

        if (other.CompareTag("Player")) // �������, ��� � ������ ���� ��� "Player"
        {
            PlayerInteractions playerInteractions = other.GetComponent<PlayerInteractions>();
            if (playerInteractions != null)
            {
                // �������� ������������ ���� �������� ����� PlayerInteractions
                // PlayerInteractions ��� �����, ����� �� ��� ������������ (�� ������ ID)
                bool activatedSuccessfully = playerInteractions.TryActivateCheckpoint(this);

                if (activatedSuccessfully)
                {
                    isActivated = true; // �������� ���� �������� ��� ��������������
                    PerformActivationFeedback();

                    // �����������: ����� �������������� ��������� ���������, ����� �� �� ���������� �������� ���������/���������
                    // Collider2D col = GetComponent<Collider2D>();
                    // if (col != null) col.enabled = false;
                    // ��� ���� ���� GameObject: gameObject.SetActive(false); (�� ����� ������� �� �����������, ���� ��� �� ���)
                }
            }
        }
    }

    void PerformActivationFeedback()
    {
        Debug.Log($"�������� {checkpointID} ({gameObject.name}) �����������!");

        if (activationEffectPrefab != null)
        {
            Instantiate(activationEffectPrefab, transform.position, Quaternion.identity);
        }

        if (activationSound != null && Camera.main != null) // ����������� ���� �� ������ ��� 2D
        {
            AudioSource.PlayClipAtPoint(activationSound, Camera.main.transform.position);
        }
        // �������������: ����� �������� ������ ���������, ��������� �������� � �.�.
    }

    // ��� ������������ � ���������
    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f); // ��� ��������
        if (respawnTargetPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(respawnTargetPoint.position, 0.3f); // ����� ��������
            Gizmos.DrawLine(transform.position, respawnTargetPoint.position);
        }
    }
}