using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;

public class CameraMovement : MonoBehaviour //, IPunObservable
{
    public GameObject tpsCamera; // TurretControl.cs ���� ���ӿ�����Ʈ�� Ȱ��ȭ, ��Ȱ��ȭ ���� 1, 3��Ī ��ȯ
    public CinemachineVirtualCamera tpsCinemachineVirtualCamera;

    public GameObject fpsCamera;
    public CinemachineVirtualCamera fpsCinemachineVirtualCamera;

    public GameObject deathCamera;
    public CinemachineVirtualCamera deathCinemachineVirtualCamera;

    public GameObject respawnCamera;
    public CinemachineVirtualCamera respawnCinemachineVirtualCamera;
    public Transform respawnCameraTransform;


    //public Transform objectToFollow; // ī�޶� ���� ������Ʈ

    public float followSpeed = 10f; // ī�޶� ���� �ӵ�
    public float sensitivity = 100f; // ���콺 ����
    private float clampAngle = 15f; // ī�޶� ���� ����

    public float rotX; // ���콺 �Է��� ���� ���� (rotationX)
    public float rotY;

    public Transform realCamera;
    public Vector3 dirNormalized;
    public Vector3 finalDir;
    public float minDistance; // ī�޶�� �÷��̾� ���̿� ��ֹ��� ���� ��� ī�޶� �������� ī�޶� �ּҰŸ�
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
        /*        // ������ ���̶�� (�켱���� ���� ����)
                if (isRespawn)
                {
                    // �̸� ������ ������ ī�޶��� ȸ�������� ��������
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
            rotX += -Input.GetAxisRaw("Mouse Y") * (sensitivity - 80f) * Time.deltaTime; // ���콺�� Y��, �� ���Ʒ��� �����̸� X�� ȸ��
            rotY += Input.GetAxisRaw("Mouse X") * (sensitivity - 80f) * Time.deltaTime;

            rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
            Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
            transform.rotation = rot;
        }
        else
        {
            rotX += -Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime; // ���콺�� Y��, �� ���Ʒ��� �����̸� X�� ȸ��
            rotY += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

            rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);
            Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
            transform.rotation = rot;
        }
    }
}
