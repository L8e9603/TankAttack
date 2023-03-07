using System;
using Photon.Pun;
using UnityEngine;

// 파괴 가능 물체로서 동작할 게임 오브젝트들을 위한 뼈대를 제공
// 체력, 데미지 받아들이기, 사망 기능, 사망 이벤트를 제공
public class DistructibleEntity : MonoBehaviourPun, IDamageable
{
    public float startingHealth = 20f; // 시작 체력
    public float health { get; protected set; } // 현재 체력
    public bool isDestucted { get; protected set; } // 사망 상태
    public event Action onDestruct; // 파괴시 발동할 이벤트


    // 호스트->모든 클라이언트 방향으로 체력과 사망 상태를 동기화 하는 메서드
    [PunRPC]
    public void ApplyUpdatedHealth(float newHealth, bool newDead)
    {
        health = newHealth;
        isDestucted = newDead;
    }

    protected virtual void Awake()
    {
        // 사망하지 않은 상태로 시작
        isDestucted = false;

        // 체력을 시작 체력으로 초기화
        health = startingHealth;
    }

    // 데미지 처리
    // 호스트에서 먼저 단독 실행되고, 호스트를 통해 다른 클라이언트들에서 일괄 실행됨
    [PunRPC]
    public virtual void OnDamage(float damage)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 데미지만큼 체력 감소
            health -= damage;

            // 호스트에서 클라이언트로 동기화
            photonView.RPC("ApplyUpdatedHealth", RpcTarget.Others, health, isDestucted);

            // 다른 클라이언트들도 OnDamage를 실행하도록 함
            photonView.RPC("OnDamage", RpcTarget.Others, damage);
        }

        // 체력이 0 이하 && 아직 죽지 않았다면 파괴 처리 실행
        if (health <= 0 && !isDestucted)
        {
            Detruct();
        }
    }


    public virtual void Detruct()
    {
        // onDeath 이벤트에 등록된 메서드가 있다면 실행
        if (onDestruct != null)
        {
            onDestruct();
        }

        // 사망 상태를 참으로 변경
        isDestucted = true;
    }
}