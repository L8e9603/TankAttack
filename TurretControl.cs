using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Cinemachine;

public class TurretControl : MonoBehaviourPun, IPunObservable
{
    [SerializeField]
    private Transform tpsCameraPosition; // Tank v2 ������ �ȿ� ����� �� tpsCamera�� �� �ڸ��� ��ġ�� ���� �� ����

    [SerializeField]
    private Transform fpsCameraPosition; // Tank v2 ������ �ȿ� ����� �� fpsCamera�� �� �ڸ��� ��ġ�� ���� �� ����

    [SerializeField]
    private Transform deathCameraPosition; // Tank v2 ������ �ȿ� ����� �� deathCamera�� �� �ڸ��� ��ġ�� ���� �� ����

    [SerializeField]
    private Transform tr; // �ͷ��� ȸ������ �������� ���� Ʈ������

    [SerializeField]
    private Transform barrelTransform; // �跲�� ȸ������ �������� ���� Ʈ������

    private RaycastHit hit; // ������ ���鿡 ���� ��ġ�� ������ ����

    public float rotSpeed = 1.0f; // �ͷ� ȸ�� �ӵ�

    private bool isAimimg = false; // ���� ���� �������� �ƴ��� ������ ����

    private PhotonView pv = null;

    // ���� ��ũ�� �ͷ� ȸ������ ������ ����
    private Quaternion currRot = Quaternion.identity;

    private float stabilizeSpeed; // �ͷ� ����ȭ�� ���� ����

    private AudioSource audioSource; // �ͷ��� ����� �ҽ�

    [SerializeField]
    private AudioClip aimingInClip; // 1��Ī ���� Ȱ��ȭ �� ����� ����� Ŭ��

    [SerializeField]
    private AudioClip aimingOutClip; // 1��Ī ���� ��Ȱ��ȭ �� ����� ����� Ŭ��

    // ���� ��ź���� ũ�ν���� ������ �ʿ��� ����
    [SerializeField]
    private Transform firePosition;
    private RaycastHit hit2;

    // ��ƼŬ �̹��� ���� �Ҵ�
    public Sprite spriteReticle;

    void Awake()
    {
        tr = GetComponent<Transform>(); 
        pv = GetComponent<PhotonView>();
        pv.Synchronization = ViewSynchronization.Unreliable;
        pv.ObservedComponents[0] = this; // �� ��ũ��Ʈ�� ����� ������Ʈ�� ������� ������Ʈ�� �ʵ忡 �ھ���
        currRot = tr.localRotation; // �ʱ� ȸ���� ����
        audioSource = GetComponent<AudioSource>();
        stabilizeSpeed = GetComponentInParent<TankMove>().rotSpeed; // ��ü ȸ���ӵ� ��ŭ ���������� ���� �ͷ� ����ȭ�� ��Ű�� ���� �θ� �ִ� TankMove�� rotSpeed����
        
        if (spriteReticle != null)
        {
            UIManager.instance.ImageReticle.sprite = spriteReticle;
        }

        if (pv.IsMine)
        {
            Camera.main.GetComponent<CameraMovement>().tpsCamera.GetComponent<CinemachineVirtualCamera>().Follow = tpsCameraPosition; 
            Camera.main.GetComponent<CameraMovement>().fpsCamera.GetComponent<CinemachineVirtualCamera>().Follow = fpsCameraPosition;
            Camera.main.GetComponent<CameraMovement>().deathCamera.GetComponent<CinemachineVirtualCamera>().Follow = deathCameraPosition;
            GameObject.FindGameObjectWithTag("TankOrientationCamera").GetComponent<TankOrientationCopy>().target = this.transform; // ��ũ ��ü - ��ž ���� ���� UI
        }
    }

    void Update()
    {
        if (pv.IsMine) // ���� ��ũ�� ��츸 �ͷ� ȸ��
        {
            if (UIManager.instance.leaveGameUI.activeSelf == true)
            {
                return; // ���� ������ UI�� Ȱ��ȭ ���̶�� �޼��� ����
            }

            // ��ž ȸ��
            if (!isAimimg)
            {
                // 3��Ī, �� ������ ���°� �ƴҶ� ��ž ȸ�� ���
                if (Physics.Raycast((Camera.main.transform.position + Camera.main.transform.forward * 15f), Camera.main.transform.forward, out hit, Mathf.Infinity)) // ī�޶� �߾ӿ��� ���� �������� 15f ������ ������ ���� �߻�
                                                                                                                                                                     // if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) // 9�� ���̾ ����
                                                                                                                                                                     // if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    // Ray�� ���� ��ġ�� ������ǥ�� ��ȯ, �����ǥ
                    Vector3 relative = tr.InverseTransformPoint(hit.point);
                    // �� ź��Ʈ �Լ��� Atan2�� �� ���� ������ ���
                    float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                    // rotSpeed ������ ����� �ӵ��� ��ž�� Y�� ȸ��
                    tr.Rotate(0, angle * Time.deltaTime * rotSpeed, 0);
                }
            }
            else
            {
                // 1��Ī, �� ���� ������ �� ��ž ȸ�� ���, �Է��� �����ʿ� ���� ���� �ӵ��� ȸ��
                if (Input.GetAxis("Mouse X") > 0)
                {
                    tr.Rotate(0, 15f * Time.deltaTime, 0);
                }
                else if (Input.GetAxis("Mouse X") < 0)
                {
                    tr.Rotate(0, -15f * Time.deltaTime, 0);
                }
            }

            // ���콺 ������ ��ư �ٿ��� �����Ǹ� ī�޶� ������ ������ ����, �޼���ȭ ���
            if (Input.GetMouseButtonDown(1))
            {
                // ������ ���°� �ƴ϶�� FPSī�޶�� ��ȯ�Ͽ� ������ ���� ����
                if (!isAimimg)
                {
                    isAimimg = true;
                    // Camera.main.GetComponent<CameraMovement>().fpsCamera.SetActive(true); // �޼���ȭ ��ų ��, ����ī�޶��� TPSCameraMovement ��ũ��Ʈ ������, public ������ fpsCamera ���ӿ�����Ʈ�� Ȱ��ȭ
                    // Camera.main.GetComponent<CameraMovement>().tpsCamera.SetActive(false);
                    Camera.main.GetComponent<CameraMovement>().tpsCamera.GetComponent<CinemachineVirtualCamera>().enabled = false;
                    Camera.main.GetComponent<CameraMovement>().fpsCamera.GetComponent<CinemachineVirtualCamera>().enabled = true;
                    UIManager.instance.tpsUI.SetActive(false); // 3��Ī UI ��Ȱ��ȭ
                    UIManager.instance.fpsUI.SetActive(true); // 1��Ī UI Ȱ��ȭ
                    UIManager.instance.StartCoroutineFadeout(); // ��ƼŬ�� ������ �ִ� ���� �ǳ� ���̵�ƿ�
                    audioSource.PlayOneShot(aimingInClip);
                }
                // ������ ���¿��ٸ� TPS ī�޶�� ��ȯ
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
            TurretStabilize(); // ȸ�� �� ���� ���� ����ȭ �޼���
            HitPositionCrosshair(); // ���� ��ź ���� ǥ�� ũ�ν����
            Zoom(); // ���콺 �� �Է��� �����Ǹ� Ȯ�� / ���
        }
        else // ���� ��Ʈ��ũ �÷��̾� ��ũ�� ���
        {
            tr.localRotation = Quaternion.Slerp(tr.localRotation, currRot, Time.deltaTime * 3.0f);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // ���� ��ũ �ͷ��� ȸ������ �۽�
        {
            stream.SendNext(tr.localRotation); 
        }
        else // ���� ��ũ �ͷ��� ȸ������ ����
        {
            currRot = (Quaternion)stream.ReceiveNext();
        }
    }

    // �ͷ��� ��ü�� �ڽ��̱� ������ ��ü ȸ�� �� �ͷ��� ��ü �������� �������� ���� ����
    private void TurretStabilize()
    {
/*        // ��ƽ�� ��ũ
        // ȸ���� �����Ǹ�
        if (Input.GetAxis("Horizontal") != 0)
        {
            // �ͷ��� Horizontal ȸ�� ���� ���������� ȸ������ ���º�����¡, Time.deltaTime�� �ݵ�� ������� ��� ��ǻ�͸� ������ �ͷ� ����ȭ�� �����۵� ��
            tr.Rotate(0, -Input.GetAxis("Horizontal") * Time.deltaTime * stabilizeSpeed, 0);
        }
*/

        // �ڵ��� ��ũ
        if (Input.GetAxis("Horizontal") != 0)
        {
            if (Input.GetAxis("Vertical") >= 0) // ���� ���°ų� �������¸�
            {
                tr.Rotate(0, -Input.GetAxis("Horizontal") * Time.deltaTime * stabilizeSpeed, 0); // �ͷ��� Horizontal ȸ�� ���� ���������� ȸ������ ���º�����¡, Time.deltaTime�� �ݵ�� ������� ��� ��ǻ�͸� ������ �ͷ� ����ȭ�� �����۵� ��
            }
            else if (Input.GetAxis("Vertical") < 0) // ���� ���¸�
            {
                tr.Rotate(0, Input.GetAxis("Horizontal") * Time.deltaTime * stabilizeSpeed, 0);
            }
        }

    }

    private void Zoom()
    {
        // ��ũ�� �ٿ� ���̸� Ȯ��
        if (Input.GetKey(KeyCode.Q))
        {
            if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
            {
                Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.m_Lens.FieldOfView
                    = Mathf.Lerp(Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.m_Lens.FieldOfView, 10f, Time.deltaTime * 5f);
            }
        }
        // ��ũ�� �� ���̸� ���
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
