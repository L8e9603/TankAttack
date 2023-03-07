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
            // 만약 싱글톤 변수에 아직 오브젝트가 할당되지 않았다면
            if (m_instance == null)
            {
                // 씬에서 GameManager 오브젝트를 찾아 할당
                m_instance = FindObjectOfType<GameManager>();
            }

            // 싱글톤 오브젝트를 반환
            return m_instance;
        }
    }

    private static GameManager m_instance; // 싱글톤이 할당될 static 변수

    [HideInInspector]
    public bool isActiveLeaveGameUI;

    public Transform[] spawnPoint;

    private int selectedSpawnPointIndex;

    void Awake()
    {
        CreateTank();
        PhotonNetwork.IsMessageQueueRunning = true; // 포톤 클라우드에서 네트워크 메시지 수신을 다시 연결
    }

    void CreateTank()
    {
        //float pos = Random.Range(-60f, -50f);
        //PhotonNetwork.Instantiate("Tank v2.1", new Vector3(pos, 5.0f, pos), Quaternion.identity, 0);

        selectedSpawnPointIndex = Random.RandomRange(0, spawnPoint.Length);
        
        // 데스매치모드 탱크 랜덤생성 테스트용 코드
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
        // 게임 중 Escape 키 다운이 감지되면
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // LeaveGameUI가 비활성화 상태였다면
            if (UIManager.instance.leaveGameUI.activeSelf == false)
            {
                UIManager.instance.leaveGameUI.SetActive(true); // UI 활성화
                isActiveLeaveGameUI = true; // LeaveGameUI의 활성화 상태를 true로 변경, 해당 변수가 true일 때 탱크 조작 관련 스크립트들에서 탱크 조작 입력을 받지 않음
                Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
                Cursor.visible = true;
            }
            // LeaveGameUI가 활성화 상태였다면
            else if (UIManager.instance.leaveGameUI.activeSelf == true)
            {
                UIManager.instance.leaveGameUI.SetActive(false); // UI 비활성화
                isActiveLeaveGameUI = false; // 상태를 false로 변경시켜 탱크 조작 입력을 받음
                Cursor.lockState = CursorLockMode.Locked; // 커서 잠금
                Cursor.visible = false;
            }
        }

        // 게임 중 Tab 키가 감지되면
        if (Input.GetKey(KeyCode.Tab))
        {
            Debug.Log(PhotonNetwork.PlayerList);
            UIManager.instance.textScoreBoardPlayerInfo.text = "";

            // 현재 하이어라키에 존재하는 모든 탱크의 정보 가져오기
            // 모든 플레이어들 킬 수 비교하여 순위 계산
            // 순위 순서대로 스코어보드 텍스트에 저장
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
        Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
        Cursor.visible = true;
    }
    public void OnButtonClickCancel()
    {
        UIManager.instance.leaveGameUI.SetActive(false);
        isActiveLeaveGameUI = false;
        Cursor.lockState = CursorLockMode.Locked; // 커서 잠금
        Cursor.visible = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        
    }
}
