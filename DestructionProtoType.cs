using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon;

public class DestructionProtoType : DistructibleEntity, IPunObservable
{
    PhotonView pv = null;

    [SerializeField]
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Rigidbody[] debrisRigidbody;

    [SerializeField]
    private GameObject destructdObject;

    private AudioSource audioSource;
    private AudioClip destructionClip;

    private float currentHealth = 20f;

    private void Awake() // Start전에 OnPhotonSerializeView 실행되면 에러
    {
        pv = GetComponent<PhotonView>();
        health = 20f;
        meshCollider = GetComponent<MeshCollider>();
        debrisRigidbody = GetComponentsInChildren<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        destructionClip = Resources.Load<AudioClip>("StoneFalling");

        // 먼저 들어간 플레이어가 부순 경우가 있기 때문에 나중에 들어간 플레이어는 먼저들어간 유저가 보낸 기둥의 체력 송신값을 기반으로 기둥의 파괴 상태 설정
        if (pv.IsMine)
        {

        }
        else
        {
            currentHealth = health; // 원격 기둥의 체력 설정
          
            // Awake 메서드 안에서 아래 코드는 작동하지 않음, Update 메서드로 옮김
            // if (currentHealth <= 0f)
            // {
            //     Detruct();
            // }
        }
    }

    private void Update()
    {
        if (currentHealth <= 0f)
        {
            audioSource.volume = 0f;
            Detruct();
        }
    }

    [PunRPC]
    public override void OnDamage(float damage)
    {
        base.OnDamage(damage);
    }

    public override void Detruct()
    {
        // 체력이 0이 되는 경우
        meshRenderer.enabled = false;
        meshCollider.enabled = false;
        destructdObject.SetActive(true);
        foreach (Rigidbody rigidbody in debrisRigidbody)
        {
            rigidbody.AddExplosionForce(1000f, this.gameObject.transform.position, 1000f);
        }
        audioSource.PlayOneShot(destructionClip, 1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 탱크와 충돌하는 경우
        if (collision.gameObject.CompareTag("Tank"))
        {
            OnDamage(20f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
/*        if (other.CompareTag("Tank"))
        {
            OnDamage(20f);
            audioSource.PlayOneShot(destructionClip);
        }
*/    }

    // 정보 송수신
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(health);
        }
        else
        {
            currentHealth = (float)stream.ReceiveNext();
        }
    }
}
