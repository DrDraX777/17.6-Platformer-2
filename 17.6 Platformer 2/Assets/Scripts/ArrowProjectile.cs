// ArrowProjectile.cs (���������� ������)
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("��������������")]
    [Tooltip("��� ������� (��������, �����), ��� ��������� � ������� ������ ������ ������������.")]
    public string targetTag = "Barrel"; // ��� ����, ��������, �����

    // ����� OnCollisionEnter2D ����� ������, ���� ������ ����� Rigidbody2D � Collider2D (�� �������),
    // � ��� ������������ � ������ ��������, � �������� ���� Collider2D.
    void OnCollisionEnter2D(Collision2D collision)
    {
        // ���������, ������ �� �� � ���� � �������� �����
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Debug.Log("������ ������ � ������ � �����: " + targetTag + " (" + collision.gameObject.name + ")");

            // �����������: ����� ����� ������� ������ ��������� ������, ���� �����
            // if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject); // ���������� ������
        }
        // ������ ������� ������ ����� ���, ������ �� ����� ������������ �� ������ ������������,
        // ���� ������ � ���� ������ �������� ��� ����� ������ ��� ����������� �����.
    }

    // ���� ������ ���� �� ���� �������� ��������� (��� ����� ��������, �� ��������),
    // � ������ ������������ ��� ����� � �������-������ � ����� targetTag.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            // Debug.Log("������ ����� � ������� ������� � �����: " + targetTag + " (" + other.gameObject.name + ")");
            // if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject); // ���������� ������
        }
    }
}