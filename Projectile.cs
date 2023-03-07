using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 6000.0f;
    [SerializeField]
    private GameObject explosionEffect; // 지형지물 피탄 시 재생할 효과 프리팹
    [SerializeField]
    private GameObject tankHitEffect; // 탱크 피탄 시 재생할 효과 프리팹
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
        _collider = GetComponent<CapsuleCollider>(); // 포탄(CannonShell)의 콜라이더
        _rigidbody = GetComponent<Rigidbody>(); // 포탄(CannonShell)의 리지드바디
        _audioSource = GetComponent<AudioSource>();
        GetComponent<Rigidbody>().AddForce(transform.forward * speed);
        StartCoroutine(ExplosionCannonShell(3.0f)); // 히트되지 않은 포탄 3초 뒤 폭파
    }
    private void FixedUpdate()
    {
/*        RaycastHit hit; // 충돌지점을 담기 위한 변수

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
            //Debug.Log(collision.gameObject.name + "의 IDamageable 가져오기 성공");
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
            //Debug.Log("OnDamage 실행됨");
        }
        // IDamageable을 상속받은 물체에 히트되지 않은 경우
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
/*        cannonShellFx.SetActive(false); // 포탄이 다른 콜라이더와 충돌하면 포탄 프리팹 비활성화

        hitObject = other.GetComponent<IDamageable>();

        // IDamageable 인터페이스를 상속받은 오브젝트에게서 IDmageable 컴포넌트를 가져오는데 성공했다면
        if (hitObject != null)
        {
            // 대미지 적용, VehicleEntity 스크립트의 RPC 함수 OnDamage는 마스터에서 연산 후 다른 클라이언트에게도 체력을 동기화 시켜 줌
            hitObject.OnDamage(cannonShellDamage, hitPosition, hitNormal);
            Debug.Log("IDamageable 가져오기 성공, OnDamage 실행됨");
        }
        else
        {
            // 콜라이더가 지면 또는 적 탱크에 충돌하는 경우 즉시 폭발 파티클 재생하도록 코루틴 실행
            StartCoroutine(ExplosionCannonShell(0.0f));
            Debug.Log("IDamageable 가져오기 실패");
        }
*/

        // 태그나 레이어에 따라서 오디오, 파티클 차별화 시켜보자
    }

    IEnumerator ExplosionCannonShell(float tm)
    {
        yield return new WaitForSeconds(tm);
        _audioSource.volume = Mathf.Lerp(_audioSource.volume, 0f, Time.deltaTime * 2f); // 포탄 휘슬소리 선형보간으로 정지 // 에러구문
        _collider.enabled = false; // 충돌 콜백함수가 발생하지 않도록 Collider를 비활성화
        //_rigidbody.isKinematic = true; // 물리엔진 영향을 받을 필요 없음
        // GameObject obj = Instantiate(expEffect, hitPosition, Quaternion.identity); // 피탄효과 재생, 광선이 충돌한 지점에 재생
        //GameObject obj = Instantiate(explosionEffect, hitPosition, explosionEffect.transform.localRotation); // 피탄효과 재생, 광선이 충돌한 지점에 재생 -> 레이 충돌지점에 피탄효과 재생시키면 콜라이더 충돌지점과 차이 발생 가능성 있음
        //Destroy(obj, 5.0f);
        Destroy(this.gameObject, 1.0f); // 트레일 렌더러가 소멸될 때까지 잠시 대기 후 삭제 처리
    }
}
