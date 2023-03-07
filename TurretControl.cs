using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;

public class TurretControl : MonoBehaviourPun, IPunObservable
{
    [SerializeField]
    private Transform tpsCameraPosition; // Tank v2 프리팹 안에 만들어 둔 tpsCamera가 올 자리위 위치를 저장 할 변수

    [SerializeField]
    private Transform fpsCameraPosition; // Tank v2 프리팹 안에 만들어 둔 fpsCamera가 올 자리위 위치를 저장 할 변수

    [SerializeField]
    private Transform deathCameraPosition; // Tank v2 프리팹 안에 만들어 둔 deathCamera가 올 자리위 위치를 저장 할 변수

    [SerializeField]
    private Transform tr; // 터렛의 회전값을 가져오기 위한 트랜스폼

    [SerializeField]
    private Transform barrelTransform; // 배럴의 회전값을 가져오기 위한 트랜스폼

    private RaycastHit hit; // 광선이 지면에 맞은 위치를 저장할 변수

    public float rotSpeed = 1.0f; // 터렛 회전 속도

    private bool isAimimg = false; // 현재 조준 상태인지 아닌지 저장할 변수

    private PhotonView pv = null;

    // 원격 탱크의 터렛 회전값을 저장할 변수
    private Quaternion currRot = Quaternion.identity;

    private float stabilizeSpeed; // 터렛 안정화를 위한 변수

    private AudioSource audioSource; // 터렛의 오디오 소스

    [SerializeField]
    private AudioClip aimingInClip; // 1인칭 조준 활성화 시 재생할 오디오 클립

    [SerializeField]
    private AudioClip aimingOutClip; // 1인칭 조준 비활성화 시 재생할 오디오 클립

    // 실제 피탄지점 크로스헤어 구현에 필요한 변수
    [SerializeField]
    private Transform firePosition;
    private RaycastHit hit2;

    // 레티클 이미지 동적 할당
    public Sprite spriteReticle;

    void Awake()
    {
        tr = GetComponent<Transform>(); 
        pv = GetComponent<PhotonView>();
        pv.Synchronization = ViewSynchronization.Unreliable;
        pv.ObservedComponents[0] = this; // 이 스크립트를 포톤뷰 컴포넌트의 옵저브드 컴포넌트에 필드에 박아줌
        currRot = tr.localRotation; // 초기 회전값 설정
        audioSource = GetComponent<AudioSource>();
        stabilizeSpeed = GetComponentInParent<TankMove>().rotSpeed; // 차체 회전속도 만큼 역방향으로 돌려 터렛 안정화를 시키기 위해 부모에 있는 TankMove의 rotSpeed참조
        
        if (spriteReticle != null)
        {
            UIManager.instance.ImageReticle.sprite = spriteReticle;
        }

        if (pv.IsMine)
        {
            Camera.main.GetComponent<CameraMovement>().tpsCamera.GetComponent<CinemachineVirtualCamera>().Follow = tpsCameraPosition; 
            Camera.main.GetComponent<CameraMovement>().fpsCamera.GetComponent<CinemachineVirtualCamera>().Follow = fpsCameraPosition;
            Camera.main.GetComponent<CameraMovement>().deathCamera.GetComponent<CinemachineVirtualCamera>().Follow = deathCameraPosition;
            GameObject.FindGameObjectWithTag("TankOrientationCamera").GetComponent<TankOrientationCopy>().target = this.transform; // 탱크 차체 - 포탑 정렬 상태 UI
        }
    }

    void Update()
    {
        if (pv.IsMine) // 로컬 탱크인 경우만 터렛 회전
        {
            if (UIManager.instance.leaveGameUI.activeSelf == true)
            {
                return; // 게임 나가기 UI가 활성화 중이라면 메서드 종료
            }

            // 포탑 회전
            if (!isAimimg)
            {
                // 3인칭, 즉 정조준 상태가 아닐때 포탑 회전 방법
                if (Physics.Raycast((Camera.main.transform.position + Camera.main.transform.forward * 15f), Camera.main.transform.forward, out hit, Mathf.Infinity)) // 카메라 중앙에서 레이 방향으로 15f 떨어진 곳에서 레이 발사
                                                                                                                                                                     // if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) // 9번 레이어만 무시
                                                                                                                                                                     // if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    // Ray에 맞은 위치를 로컬좌표로 변환, 상대좌표
                    Vector3 relative = tr.InverseTransformPoint(hit.point);
                    // 역 탄젠트 함수인 Atan2로 두 점의 각도를 계산
                    float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                    // rotSpeed 변수에 저장된 속도로 포탑의 Y축 회전
                    tr.Rotate(0, angle * Time.deltaTime * rotSpeed, 0);
                }
            }
            else
            {
                // 1인칭, 즉 조준 상태일 때 포탑 회전 방법, 입력이 감지됨에 따라 일정 속도로 회전
                if (Input.GetAxis("Mouse X") > 0)
                {
                    tr.Rotate(0, 15f * Time.deltaTime, 0);
                }
                else if (Input.GetAxis("Mouse X") < 0)
                {
                    tr.Rotate(0, -15f * Time.deltaTime, 0);
                }
            }

            // 마우스 오른쪽 버튼 다운이 감지되면 카메라를 변경해 정조준 실행, 메서드화 고려
            if (Input.GetMouseButtonDown(1))
            {
                // 정조준 상태가 아니라면 FPS카메라로 전환하여 정조준 모드로 변경
                if (!isAimimg)
                {
                    isAimimg = true;
                    // Camera.main.GetComponent<CameraMovement>().fpsCamera.SetActive(true); // 메서드화 시킬 것, 메인카메라의 TPSCameraMovement 스크립트 얻어오기, public 선언한 fpsCamera 게임오브젝트를 활성화
                    // Camera.main.GetComponent<CameraMovement>().tpsCamera.SetActive(false);
                    Camera.main.GetComponent<CameraMovement>().tpsCamera.GetComponent<CinemachineVirtualCamera>().enabled = false;
                    Camera.main.GetComponent<CameraMovement>().fpsCamera.GetComponent<CinemachineVirtualCamera>().enabled = true;
                    UIManager.instance.tpsUI.SetActive(false); // 3인칭 UI 비활성화
                    UIManager.instance.fpsUI.SetActive(true); // 1인칭 UI 활성화
                    UIManager.instance.StartCoroutineFadeout(); // 레티클을 가리고 있던 검은 판넬 페이드아웃
                    audioSource.PlayOneShot(aimingInClip);
                }
                // 정조준 상태였다면 TPS 카메라로 전환
                else if (isAimimg)
                {
                    isAimimg = false;
                    // Camera.main.GetComponent<CameraMovement>().fpsCamera.SetActive(false);
                    // Camera.main.GetComponent<CameraMovement>().tpsCamera.SetActive(true);
                    Camera.main.GetComponent<CameraMovement>().fpsCamera.GetComponent<CinemachineVirtualCamera>().enabled = false;
                    Camera.main.GetComponent<CameraMovement>().tpsCamera.GetComponent<CinemachineVirtualCamera>().enabled = true;
                    UIManager.instance.fpsUI.SetActive(false);
                    UIManager.instance.tpsUI.SetActive(true);
                    audioSource.PlayOneShot(aimingOutClip);
                }
            }
            TurretStabilize(); // 회전 시 주포 조준 안정화 메서드
            HitPositionCrosshair(); // 실제 피탄 지점 표시 크로스헤어
            Zoom(); // 마우스 휠 입력이 감지되면 확대 / 축소
        }
        else // 원격 네트워크 플레이어 탱크인 경우
        {
            tr.localRotation = Quaternion.Slerp(tr.localRotation, currRot, Time.deltaTime * 3.0f);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 로컬 탱크 터렛의 회전값을 송신
        {
            stream.SendNext(tr.localRotation); 
        }
        else // 원격 탱크 터렛의 회전값을 수신
        {
            currRot = (Quaternion)stream.ReceiveNext();
        }
    }

    // 터렛이 차체의 자식이기 때문에 차체 회전 시 터렛이 차체 방향으로 딸려오는 현상 방지
    private void TurretStabilize()
    {
/*        // 스틱형 탱크
        // 회전이 감지되면
        if (Input.GetAxis("Horizontal") != 0)
        {
            // 터렛을 Horizontal 회전 값의 역방향으로 회전시켜 스태빌라이징, Time.deltaTime을 반드시 곱해줘야 어느 컴퓨터를 가더라도 터렛 안정화가 정상작동 함
            tr.Rotate(0, -Input.GetAxis("Horizontal") * Time.deltaTime * stabilizeSpeed, 0);
        }
*/

        // 핸들형 탱크
        if (Input.GetAxis("Horizontal") != 0)
        {
            if (Input.GetAxis("Vertical") >= 0) // 정지 상태거나 전진상태면
            {
                tr.Rotate(0, -Input.GetAxis("Horizontal") * Time.deltaTime * stabilizeSpeed, 0); // 터렛을 Horizontal 회전 값의 역방향으로 회전시켜 스태빌라이징, Time.deltaTime을 반드시 곱해줘야 어느 컴퓨터를 가더라도 터렛 안정화가 정상작동 함
            }
            else if (Input.GetAxis("Vertical") < 0) // 후진 상태면
            {
                tr.Rotate(0, Input.GetAxis("Horizontal") * Time.deltaTime * stabilizeSpeed, 0);
            }
        }

    }

    private void Zoom()
    {
        // 스크롤 다운 중이면 확대
        if (Input.GetKey(KeyCode.Q))
        {
            if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
            {
                Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.m_Lens.FieldOfView
                    = Mathf.Lerp(Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.m_Lens.FieldOfView, 10f, Time.deltaTime * 5f);
            }
        }
        // 스크롭 업 중이면 축소
        if (Input.GetKey(KeyCode.E))
        {
            if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
            {
                Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.m_Lens.FieldOfView
                    = Mathf.Lerp(Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.m_Lens.FieldOfView, 25f, Time.deltaTime * 5f);
            }
        }
    }

    private void HitPositionCrosshair()
    {
        if(Physics.Raycast(firePosition.position, firePosition.forward, out hit2, Mathf.Infinity))
        {
            Vector3 hitPositionInScreen = Camera.main.WorldToScreenPoint(hit2.point);
            UIManager.instance.hitPositionCrosshair.transform.position = 
                Vector3.Lerp(UIManager.instance.hitPositionCrosshair.transform.position, hitPositionInScreen, Time.deltaTime * 10f);
        }
    }

/*    private IEnumerator ChangeCameraTPStoFPS()
    {

    }
*/}
