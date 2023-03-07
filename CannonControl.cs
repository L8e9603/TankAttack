using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CannonControl : MonoBehaviour, IPunObservable
{
    private Transform tr; // ������ ȸ���� ����� ���� Ʈ������
    public float rotSpeed = 2.0f; // ������ ȸ�� �ӵ�
    private PhotonView pv = null; // ����� ������Ʈ
    private Quaternion currRot = Quaternion.identity; // ���� ��ũ ������ ȸ������ ����

    public float clampAngle = 7.0f; // �跲�� ȸ������ �����ϱ� ���� ����
    private RaycastHit hit; // ������ ���鿡 ���� ��ġ�� ������ ����

    [SerializeField]
    private Transform barrelMidTransform; // �߰� �跲 Ʈ������

    [SerializeField]
    private Transform barrelEndTransform; // ���� �� �跲 Ʈ������

/*    private Vector3 barrelMidRemotePosition = Vector3.zero; // ������ũ �߰��跲�� ��ġ�� ��� ����
    private Vector3 barrelEndRemotePosition = Vector3.zero; // ������ũ ���跲�� ��ġ�� ��� ����

    private Quaternion barrelMidRemoteRotation = Quaternion.identity; // ������ũ �߰��跲�� ȸ������ ��� ����
    private Quaternion barrelEndRemoteRotation = Quaternion.identity; // ������ũ ���跲�� ȸ������ ��� ����
*/
    private

    void Awake()
    {
        tr = GetComponent<Transform>();
        pv = GetComponent<PhotonView>();
        pv.ObservedComponents[0] = this; // �� ��ũ��Ʈ�� ����� ������Ʈ�� ������� ������Ʈ �ʵ忡 �ھ���
        pv.Synchronization = ViewSynchronization.Unreliable;
        currRot = tr.localRotation; // �ʱ� ȸ���� ����
/*        barrelMidRemotePosition = barrelMidTransform.position; // �ʱ� ��ġ�� ���� 
        barrelEndRemotePosition = barrelEndTransform.position; // �ʱ� ��ġ�� ���� 
        barrelMidRemoteRotation = barrelMidTransform.localRotation; // �ʱ� ȸ���� ���� 
        barrelEndRemoteRotation = barrelEndTransform.localRotation; // �ʱ� ȸ���� ���� 
*/    }

    void Update()
    {
        if (pv.IsMine)
        {
            if (GameManager.instance.isActiveLeaveGameUI)
            {
                return;
            }

            // ���� ��ũ�� ���Ÿ� ���콺 ��ũ�ѷ� ����ȸ��
            /*            float angle = -Input.GetAxis("Mouse ScrollWheel") * rotSpeed * Time.deltaTime;
                        tr.Rotate(angle, 0, 0);
            */

            // �跲 ȸ��
            if (Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled)
            {
                // 3��Ī ������ �� �跲 ȸ�� ���
                // ���� ī�޶󿡼� ���콺 Ŀ���� ��ġ�� ĳ���� �Ǵ� Ray�� ����
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(Camera.main.transform.position + ray.direction, ray.direction, out hit, Mathf.Infinity)) // ī�޶� �߾ӿ��� ���� �������� 15f ������ ������ ���� �߻�
                {
                    // Ray�� ���� ��ġ�� ������ǥ�� ��ȯ, �����ǥ
                    Vector3 relative = tr.InverseTransformPoint(hit.point);
                    // �� ź��Ʈ �Լ��� Atan2�� �� ���� ������ ���
                    float angle = Mathf.Atan2(relative.y, relative.z) * Mathf.Rad2Deg;
                    // rotSpeed ������ ����� �ӵ��� �跲�� X�� ȸ��
                    tr.Rotate(-angle * Time.deltaTime * rotSpeed, 0, 0);
                }
            }
            else
            {
                // ���� ������ �� �跲 ȸ�� ���, �Է��� �����ʿ� ���� ���� �ӵ��� ȸ��
                if (Input.GetAxis("Mouse Y") > 0)
                {
                    tr.Rotate(-5f * Time.deltaTime, 0, 0);
                }
                else if (Input.GetAxis("Mouse Y") < 0)
                {
                    tr.Rotate(5f * Time.deltaTime,0, 0);
                }
            }

            // �跲 ���� ����
            tr.localEulerAngles = new Vector3(ClampAngle(tr.localEulerAngles.x, -clampAngle, clampAngle), 0, 0);
        }
        else // ���Ź��� ������ũ�� ���� ȸ�������� ����ó���Ͽ� �ε巴�� ȸ��
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
        if (stream.IsWriting) // ���� ��ũ �ͷ��� ȸ������ �۽�
        {
            stream.SendNext(tr.localRotation); // �ͷ��� �̾��� ù��° ������ ȸ�� �۽�
/*            stream.SendNext(barrelMidTransform.position); // �߰� �跲 ��ġ �۽�
            stream.SendNext(barrelEndTransform.position); // ������ �跲 ��ġ �۽�
            stream.SendNext(barrelMidTransform.localRotation); // �߰� �跲 ȸ�� �۽�
            stream.SendNext(barrelEndTransform.localRotation); // ������ �跲 ȸ�� �۽�
*/        }
        else // ���� ��ũ �ͷ��� ȸ������ ����
        {
            currRot = (Quaternion)stream.ReceiveNext();
/*            barrelMidRemotePosition = (Vector3)stream.ReceiveNext(); // �߰� �跲 ��ġ ����
            barrelEndRemotePosition = (Vector3)stream.ReceiveNext(); // ������ �跲 ��ġ ����
            barrelMidRemoteRotation = (Quaternion)stream.ReceiveNext(); // �߰� �跲 ȸ�� ����
            barrelEndRemoteRotation = (Quaternion)stream.ReceiveNext(); // ������ �跲 ȸ�� ����
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