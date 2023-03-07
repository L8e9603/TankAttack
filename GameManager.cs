using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager instance
    {
        get
        {
            // ���� �̱��� ������ ���� ������Ʈ�� �Ҵ���� �ʾҴٸ�
            if (m_instance == null)
            {
                // ������ GameManager ������Ʈ�� ã�� �Ҵ�
                m_instance = FindObjectOfType<GameManager>();
            }

            // �̱��� ������Ʈ�� ��ȯ
            return m_instance;
        }
    }

    private static GameManager m_instance; // �̱����� �Ҵ�� static ����

    [HideInInspector]
    public bool isActiveLeaveGameUI;

    public Transform[] spawnPoint;

    private int selectedSpawnPointIndex;

    void Awake()
    {
        CreateTank();
        PhotonNetwork.IsMessageQueueRunning = true; // ���� Ŭ���忡�� ��Ʈ��ũ �޽��� ������ �ٽ� ����
    }

    void CreateTank()
    {
        //float pos = Random.Range(-60f, -50f);
        //PhotonNetwork.Instantiate("Tank v2.1", new Vector3(pos, 5.0f, pos), Quaternion.identity, 0);

        selectedSpawnPointIndex = Random.RandomRange(0, spawnPoint.Length);
        
        // ������ġ��� ��ũ �������� �׽�Ʈ�� �ڵ�
        if(selectedSpawnPointIndex/2 == 0)
        {
            PhotonNetwork.Instantiate("Tank v2.1", spawnPoint[selectedSpawnPointIndex].position, Quaternion.Euler(0, Camera.main.GetComponent<CameraMovement>().rotY, 0), 0);
        }
        else
        {
            PhotonNetwork.Instantiate("Sherman", spawnPoint[selectedSpawnPointIndex].position, Quaternion.Euler(0, Camera.main.GetComponent<CameraMovement>().rotY, 0), 0);
        }
    }

    void Update()
    {
        // ���� �� Escape Ű �ٿ��� �����Ǹ�
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // LeaveGameUI�� ��Ȱ��ȭ ���¿��ٸ�
            if (UIManager.instance.leaveGameUI.activeSelf == false)
            {
                UIManager.instance.leaveGameUI.SetActive(true); // UI Ȱ��ȭ
                isActiveLeaveGameUI = true; // LeaveGameUI�� Ȱ��ȭ ���¸� true�� ����, �ش� ������ true�� �� ��ũ ���� ���� ��ũ��Ʈ�鿡�� ��ũ ���� �Է��� ���� ����
                Cursor.lockState = CursorLockMode.None; // Ŀ�� ��� ����
                Cursor.visible = true;
            }
            // LeaveGameUI�� Ȱ��ȭ ���¿��ٸ�
            else if (UIManager.instance.leaveGameUI.activeSelf == true)
            {
                UIManager.instance.leaveGameUI.SetActive(false); // UI ��Ȱ��ȭ
                isActiveLeaveGameUI = false; // ���¸� false�� ������� ��ũ ���� �Է��� ����
                Cursor.lockState = CursorLockMode.Locked; // Ŀ�� ���
                Cursor.visible = false;
            }
        }

        // ���� �� Tab Ű�� �����Ǹ�
        if (Input.GetKey(KeyCode.Tab))
        {
            Debug.Log(PhotonNetwork.PlayerList);
            UIManager.instance.textScoreBoardPlayerInfo.text = "";

            // ���� ���̾��Ű�� �����ϴ� ��� ��ũ�� ���� ��������
            // ��� �÷��̾�� ų �� ���Ͽ� ���� ���
            // ���� ������� ���ھ�� �ؽ�Ʈ�� ����
            UIManager.instance.SetActiveScoreBoardUI(true);
        }
        else
        {
            UIManager.instance.SetActiveScoreBoardUI(false);
        }
    }


    public void OnButtonClickLeave()
    {
        PhotonNetwork.LeaveRoom();
        Application.LoadLevel(0);
        Cursor.lockState = CursorLockMode.None; // Ŀ�� ��� ����
        Cursor.visible = true;
    }
    public void OnButtonClickCancel()
    {
        UIManager.instance.leaveGameUI.SetActive(false);
        isActiveLeaveGameUI = false;
        Cursor.lockState = CursorLockMode.Locked; // Ŀ�� ���
        Cursor.visible = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        
    }
}
