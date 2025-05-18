// AutoDestroyEffect.cs (�������� ��� ���������)
using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    [Tooltip("����� ����� ������� ������� � ��������.")]
    public float lifetime = 1.0f; // ������� � ����������!

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}