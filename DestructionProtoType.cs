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

    private void Awake() // Start���� OnPhotonSerializeView ����Ǹ� ����
    {
        pv = GetComponent<PhotonView>();
        health = 20f;
        meshCollider = GetComponent<MeshCollider>();
        debrisRigidbody = GetComponentsInChildren<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        destructionClip = Resources.Load<AudioClip>("StoneFalling");

        // ���� �� �÷��̾ �μ� ��찡 �ֱ� ������ ���߿� �� �÷��̾�� ������ ������ ���� ����� ü�� �۽Ű��� ������� ����� �ı� ���� ����
        if (pv.IsMine)
        {

        }
        else
        {
            currentHealth = health; // ���� ����� ü�� ����
          
            // Awake �޼��� �ȿ��� �Ʒ� �ڵ�� �۵����� ����, Update �޼���� �ű�
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
        // ü���� 0�� �Ǵ� ���
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
        // ��ũ�� �浹�ϴ� ���
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

    // ���� �ۼ���
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
