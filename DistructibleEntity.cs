using System;
using Photon.Pun;
using UnityEngine;

// �ı� ���� ��ü�μ� ������ ���� ������Ʈ���� ���� ���븦 ����
// ü��, ������ �޾Ƶ��̱�, ��� ���, ��� �̺�Ʈ�� ����
public class DistructibleEntity : MonoBehaviourPun, IDamageable
{
    public float startingHealth = 20f; // ���� ü��
    public float health { get; protected set; } // ���� ü��
    public bool isDestucted { get; protected set; } // ��� ����
    public event Action onDestruct; // �ı��� �ߵ��� �̺�Ʈ


    // ȣ��Ʈ->��� Ŭ���̾�Ʈ �������� ü�°� ��� ���¸� ����ȭ �ϴ� �޼���
    [PunRPC]
    public void ApplyUpdatedHealth(float newHealth, bool newDead)
    {
        health = newHealth;
        isDestucted = newDead;
    }

    protected virtual void Awake()
    {
        // ������� ���� ���·� ����
        isDestucted = false;

        // ü���� ���� ü������ �ʱ�ȭ
        health = startingHealth;
    }

    // ������ ó��
    // ȣ��Ʈ���� ���� �ܵ� ����ǰ�, ȣ��Ʈ�� ���� �ٸ� Ŭ���̾�Ʈ�鿡�� �ϰ� �����
    [PunRPC]
    public virtual void OnDamage(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // ��������ŭ ü�� ����
            health -= damage;

            // ȣ��Ʈ���� Ŭ���̾�Ʈ�� ����ȭ
            photonView.RPC("ApplyUpdatedHealth", RpcTarget.Others, health, isDestucted);

            // �ٸ� Ŭ���̾�Ʈ�鵵 OnDamage�� �����ϵ��� ��
            photonView.RPC("OnDamage", RpcTarget.Others, damage);
        }

        // ü���� 0 ���� && ���� ���� �ʾҴٸ� �ı� ó�� ����
        if (health <= 0 && !isDestucted)
        {
            Detruct();
        }
    }


    public virtual void Detruct()
    {
        // onDeath �̺�Ʈ�� ��ϵ� �޼��尡 �ִٸ� ����
        if (onDestruct != null)
        {
            onDestruct();
        }

        // ��� ���¸� ������ ����
        isDestucted = true;
    }
}