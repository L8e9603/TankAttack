using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CannonControl : MonoBehaviour, IPunObservable
{
    private Transform tr; // 포신의 회전값 사용을 위한 트랜스폼
    public float rotSpeed = 2.0f; // 포신의 회전 속도
    private PhotonView pv = null; // 포톤뷰 컴포넌트
    private Quaternion currRot = Quaternion.identity; // 원격 탱크 포신의 회전값을 저장

    public float clampAngle = 7.0f; // 배럴의 회전값을 제한하기 위한 변수
    private RaycastHit hit; // 광선이 지면에 맞은 위치를 저장할 변수

    [SerializeField]
    private Transform barrelMidTransform; // 중간 배럴 트랜스폼

    [SerializeField]
    private Transform barrelEndTransform; // 가장 끝 배럴 트랜스폼

/*    private Vector3 barrelMidRemotePosition = Vector3.zero; // 원격탱크 중간배럴의 위치가 담길 변수
    private Vector3 barrelEndRemotePosition = Vector3.zero; // 원격탱크 끝배럴의 위치가 담길 변수

    private Quaternion barrelMidRemoteRotation = Quaternion.identity; // 원격탱크 중간배럴의 회전값이 담길 변수
    private Quaternion barrelEndRemoteRotation = Quaternion.identity; // 원격탱크 끝배럴의 회전값이 담길 변수
*/
    private

    void Awake()
    {
        tr = GetComponent<Transform>();
        pv = GetComponent<PhotonView>();
        pv.ObservedComponents[0] = this; // 이 스크립트를 포톤뷰 컴포넌트의 옵저브드 컴포넌트 필드에 박아줌
        pv.Synchronization = ViewSynchronization.Unreliable;
        currRot = tr.localRotation; // 초기 회전값 설정
/*        barrelMidRemotePosition = barrelMidTransform.position; // 초기 위치값 설정 
        barrelEndRemotePosition = barrelEndTransform.position; // 초기 위치값 설정 
        barrelMidRemoteRotation = barrelMidTransform.localRotation; // 초기 회전값 설정 
        barrelEndRemoteRotation = barrelEndTransform.localRotation; // 초기 회전값 설정 
*/    }

    void Update()
    {
        if (pv.IsMine)
        {
            if (GameManager.instance.isActiveLeaveGameUI)
            {
                return;
            }

            // 로컬 탱크의 포신만 마우스 스크롤로 상하회전
            /*            float angle = -Input.GetAxis("Mouse ScrollWheel") * rotSpeed * Time.deltaTime;
                        tr.Rotate(angle, 0, 0);
            */

            // 배럴 회전
            if (Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled)
            {
                // 3인칭 상태일 때 배럴 회전 방법
                // 메인 카메라에서 마우스 커서의 위치로 캐스팅 되는 Ray를 생성
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(Camera.main.transform.position + ray.direction, ray.direction, out hit, Mathf.Infinity)) // 카메라 중앙에서 레이 방향으로 15f 떨어진 곳에서 레이 발사
                {
                    // Ray에 맞은 위치를 로컬좌표로 변환, 상대좌표
                    Vector3 relative = tr.InverseTransformPoint(hit.point);
                    // 역 탄젠트 함수인 Atan2로 두 점의 각도를 계산
                    float angle = Mathf.Atan2(relative.y, relative.z) * Mathf.Rad2Deg;
                    // rotSpeed 변수에 저장된 속도로 배럴의 X축 회전
                    tr.Rotate(-angle * Time.deltaTime * rotSpeed, 0, 0);
                }
            }
            else
            {
                // 조준 상태일 때 배럴 회전 방법, 입력이 감지됨에 따라 일정 속도로 회전
                if (Input.GetAxis("Mouse Y") > 0)
                {
                    tr.Rotate(-5f * Time.deltaTime, 0, 0);
                }
                else if (Input.GetAxis("Mouse Y") < 0)
                {
                    tr.Rotate(5f * Time.deltaTime,0, 0);
                }
            }

            // 배럴 각도 제한
            tr.localEulerAngles = new Vector3(ClampAngle(tr.localEulerAngles.x, -clampAngle, clampAngle), 0, 0);
        }
        else // 수신받은 원격탱크의 포신 회전값으로 보간처리하여 부드럽게 회전
        {
            tr.localRotation = Quaternion.Slerp(tr.localRotation, currRot, Time.deltaTime * 3.0f);
/*            barrelMidTransform.position = Vector3.Lerp(barrelMidTransform.position, barrelMidRemotePosition, Time.deltaTime * 3.0f);
            barrelEndTransform.position = Vector3.Lerp(barrelEndTransform.position, barrelEndRemotePosition, Time.deltaTime * 3.0f);
            barrelMidTransform.localRotation = Quaternion.Slerp(barrelMidTransform.localRotation, barrelMidRemoteRotation, Time.deltaTime * 3.0f);
            barrelEndTransform.localRotation = Quaternion.Slerp(barrelEndTransform.localRotation, barrelEndRemoteRotation, Time.deltaTime * 3.0f);
*/        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 로컬 탱크 터렛의 회전값을 송신
        {
            stream.SendNext(tr.localRotation); // 터렛과 이어진 첫번째 포신의 회전 송신
/*            stream.SendNext(barrelMidTransform.position); // 중간 배럴 위치 송신
            stream.SendNext(barrelEndTransform.position); // 마지막 배럴 위치 송신
            stream.SendNext(barrelMidTransform.localRotation); // 중간 배럴 회전 송신
            stream.SendNext(barrelEndTransform.localRotation); // 마지막 배럴 회전 송신
*/        }
        else // 원격 탱크 터렛의 회전값을 수신
        {
            currRot = (Quaternion)stream.ReceiveNext();
/*            barrelMidRemotePosition = (Vector3)stream.ReceiveNext(); // 중간 배럴 위치 수신
            barrelEndRemotePosition = (Vector3)stream.ReceiveNext(); // 마지막 배럴 위치 수신
            barrelMidRemoteRotation = (Quaternion)stream.ReceiveNext(); // 중간 배럴 회전 수신
            barrelEndRemoteRotation = (Quaternion)stream.ReceiveNext(); // 마지막 배럴 회전 수신
*/        }

    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180)
        {
            angle -= 360;
        }
        else if (angle < -180)
        {
            angle += 360;
        }

        return Mathf.Clamp(angle, min, max);
    }
}