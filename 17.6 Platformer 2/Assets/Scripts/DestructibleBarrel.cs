using UnityEngine;

public class DestructibleBarrel : MonoBehaviour
{
    [Header("�������")]
    [Tooltip("������ �������/�������� ������ � ������ ��������� ������, ������� �������� �� ����� �����.")]
    public GameObject explosionPrefab; // <--- ���� ������ ���������� BarrelExplosionAnimPrefab
    public GameObject explosioneffectorPrefab;
    [Header("��������������")]
    [Tooltip("��� �������, ������� ����� ��������� ��� ����� (��������, \"Arrow\").")]
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
                // ������� ��������� ������� �������� ������
                Instantiate(explosionPrefab, transform.position, transform.rotation);
                Instantiate(explosioneffectorPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject); // ���������� ���� �����
        }
    }
}