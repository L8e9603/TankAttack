using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 6000.0f;
    [SerializeField]
    private GameObject explosionEffect; // �������� ��ź �� ����� ȿ�� ������
    [SerializeField]
    private GameObject tankHitEffect; // ��ũ ��ź �� ����� ȿ�� ������
    private CapsuleCollider _collider;
    private Rigidbody _rigidbody;
    private AudioSource _audioSource;
    [SerializeField]
    private AudioClip stoneFallingClip;
    [SerializeField]
    private AudioClip tankHitClip;

    [HideInInspector]
    public Vector3 hitPosition;
    [HideInInspector]
    public Vector3 hitNormal;
    [HideInInspector]
    public IDamageable hitObject;
    [HideInInspector]
    public IDamageable hitObject2;

    public float cannonShellDamage = 20f;

    [SerializeField]
    private GameObject cannonShellFx;

    public AudioSource audioSourceShellWhistling;

    void Start()
    {
        _collider = GetComponent<CapsuleCollider>(); // ��ź(CannonShell)�� �ݶ��̴�
        _rigidbody = GetComponent<Rigidbody>(); // ��ź(CannonShell)�� ������ٵ�
        _audioSource = GetComponent<AudioSource>();
        GetComponent<Rigidbody>().AddForce(transform.forward * speed);
        StartCoroutine(ExplosionCannonShell(3.0f)); // ��Ʈ���� ���� ��ź 3�� �� ����
    }
    private void FixedUpdate()
    {
/*        RaycastHit hit; // �浹������ ��� ���� ����

        if (Physics.SphereCast(transform.position, 0.2f, transform.forward, out hit, Mathf.Infinity))
        {
            Debug.DrawRay(transform.position, transform.forward, Color.red ,Mathf.Infinity);
            hitPosition = hit.point;
            hitObject2 = hit.collider.GetComponent<IDamageable>();
        }*/
    }

    private void OnCollisionEnter(Collision collision)
    {
        audioSourceShellWhistling.Stop();

        cannonShellFx.SetActive(false);

        hitObject = collision.other.GetComponent<IDamageable>();

        if (hitObject != null)
        {
            //Debug.Log(collision.gameObject.name + "�� IDamageable �������� ����");
            _collider.enabled = false;
            if (collision.gameObject.CompareTag("Tank"))
            {
                GameObject obj = Instantiate(tankHitEffect, collision.contacts[0].point, explosionEffect.transform.localRotation);
            }
            else if (collision.gameObject.CompareTag("Untagged"))
            {
                GameObject obj = Instantiate(explosionEffect, collision.contacts[0].point, explosionEffect.transform.localRotation);
                _audioSource.PlayOneShot(stoneFallingClip);
            }
            _audioSource.volume = Mathf.Lerp(_audioSource.volume, 0f, Time.deltaTime * 2f);
            hitObject.OnDamage(cannonShellDamage);
            //Debug.Log("OnDamage �����");
        }
        // IDamageable�� ��ӹ��� ��ü�� ��Ʈ���� ���� ���
        else
        {
            _collider.enabled = false;
            GameObject obj = Instantiate(explosionEffect, collision.contacts[0].point, explosionEffect.transform.localRotation);
            _audioSource.PlayOneShot(stoneFallingClip);
            //_audioSource.volume = Mathf.Lerp(_audioSource.volume, 0f, Time.deltaTime * 2f);
            Destroy(this.gameObject, 5.0f);
            Destroy(obj, 5.0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
/*        cannonShellFx.SetActive(false); // ��ź�� �ٸ� �ݶ��̴��� �浹�ϸ� ��ź ������ ��Ȱ��ȭ

        hitObject = other.GetComponent<IDamageable>();

        // IDamageable �������̽��� ��ӹ��� ������Ʈ���Լ� IDmageable ������Ʈ�� �������µ� �����ߴٸ�
        if (hitObject != null)
        {
            // ����� ����, VehicleEntity ��ũ��Ʈ�� RPC �Լ� OnDamage�� �����Ϳ��� ���� �� �ٸ� Ŭ���̾�Ʈ���Ե� ü���� ����ȭ ���� ��
            hitObject.OnDamage(cannonShellDamage, hitPosition, hitNormal);
            Debug.Log("IDamageable �������� ����, OnDamage �����");
        }
        else
        {
            // �ݶ��̴��� ���� �Ǵ� �� ��ũ�� �浹�ϴ� ��� ��� ���� ��ƼŬ ����ϵ��� �ڷ�ƾ ����
            StartCoroutine(ExplosionCannonShell(0.0f));
            Debug.Log("IDamageable �������� ����");
        }
*/

        // �±׳� ���̾ ���� �����, ��ƼŬ ����ȭ ���Ѻ���
    }

    IEnumerator ExplosionCannonShell(float tm)
    {
        yield return new WaitForSeconds(tm);
        _audioSource.volume = Mathf.Lerp(_audioSource.volume, 0f, Time.deltaTime * 2f); // ��ź �ֽ��Ҹ� ������������ ���� // ��������
        _collider.enabled = false; // �浹 �ݹ��Լ��� �߻����� �ʵ��� Collider�� ��Ȱ��ȭ
        //_rigidbody.isKinematic = true; // �������� ������ ���� �ʿ� ����
        // GameObject obj = Instantiate(expEffect, hitPosition, Quaternion.identity); // ��źȿ�� ���, ������ �浹�� ������ ���
        //GameObject obj = Instantiate(explosionEffect, hitPosition, explosionEffect.transform.localRotation); // ��źȿ�� ���, ������ �浹�� ������ ��� -> ���� �浹������ ��źȿ�� �����Ű�� �ݶ��̴� �浹������ ���� �߻� ���ɼ� ����
        //Destroy(obj, 5.0f);
        Destroy(this.gameObject, 1.0f); // Ʈ���� �������� �Ҹ�� ������ ��� ��� �� ���� ó��
    }
}
