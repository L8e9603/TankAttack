using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;

public class CameraMovement : MonoBehaviour //, IPunObservable
{
    public GameObject tpsCamera; // TurretControl.cs 에서 게임오브젝트를 활성화, 비활성화 시켜 1, 3인칭 전환
    public CinemachineVirtualCamera tpsCinemachineVirtualCamera;

    public GameObject fpsCamera;
    public CinemachineVirtualCamera fpsCinemachineVirtualCamera;

    public GameObject deathCamera;
    public CinemachineVirtualCamera deathCinemachineVirtualCamera;

    public GameObject respawnCamera;
    public CinemachineVirtualCamera respawnCinemachineVirtualCamera;
    public Transform respawnCameraTransform;


    //public Transform objectToFollow; // 카메라가 따라갈 오브젝트

    public float followSpeed = 10f; // 카메라가 따라갈 속도
    public float sensitivity = 100f; // 마우스 감도
    private float clampAngle = 15f; // 카메라 제한 각도

    public float rotX; // 마우스 입력을 받을 변수 (rotationX)
    public float rotY;

    public Transform realCamera;
    public Vector3 dirNormalized;
    public Vector3 finalDir;
    public float minDistance; // 카메라와 플레이어 사이에 장애물이 있을 경우 카메라가 좁혀지는 카메라 최소거리
    public float maxDistance;
    public float finalDistance;
    public float smoothness = 10f;

    public bool isRespawn = false;

    public bool getInput = true;

    void Awake()
    {
        realCamera = Camera.main.GetComponent<Transform>();

        rotX = transform.localRotation.eulerAngles.x;
        rotY = transform.localRotation.eulerAngles.y;

        dirNormalized = realCamera.localPosition.normalized;
        finalDistance = realCamera.localPosition.magnitude;

        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;

        tpsCinemachineVirtualCamera = tpsCamera.GetComponent<CinemachineVirtualCamera>();
        fpsCinemachineVirtualCamera = fpsCamera.GetComponent<CinemachineVirtualCamera>();
    }

    void Update()
    {
        /*        // 리스폰 중이라면 (우선순위 가장 높음)
                if (isRespawn)
                {
                    // 미리 설정한 리스폰 카메라의 회전값으로 선형보간
                    this.gameObject.transform.rotation = Quaternion.Lerp(this.gameObject.transform.rotation, respawnCameraTransform.rotation, Time.deltaTime * smoothness);
                    return;
                }
        */
        if (!getInput)
        {
            return;
        }

        if (UIManager.instance.leaveGameUI.activeSelf)
        {
            return;
        }

        if (fpsCinemachineVirtualCamera.enabled)
        {
            Debug.Log(Input.GetAxis("Mouse X"));
            rotX += -Input.GetAxisRaw("Mouse Y") * (sensitivity - 80f) * Time.deltaTime; // 마우스를 Y축, 즉 위아래로 움직이면 X축 회전
            rotY += Input.GetAxisRaw("Mouse X") * (sensitivity - 80f) * Time.deltaTime;

            rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
            Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
            transform.rotation = rot;
        }
        else
        {
            rotX += -Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime; // 마우스를 Y축, 즉 위아래로 움직이면 X축 회전
            rotY += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

            rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
            Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
            transform.rotation = rot;
        }
    }
}
