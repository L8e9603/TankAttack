using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class PhotonInit : MonoBehaviourPunCallbacks // ������ ���� �ݹ��Լ� ����� ���ؼ��� MonoBehaviourPunCallbacks Ŭ������ ��ӹ޾ƾ���
{
    public string version = "v1.0";
    public Text logText; // ���� �α׸� ǥ���� �ؽ�Ʈ
    public InputField userId; // ����� �̸��� �Է��ϴ� UI �׸� ����

    public InputField roomName; // �� �̸��� �Է¹��� UI �׸� ���� ����

    public GameObject scrollContent;
    public GameObject roomItem;

    public List<GameObject> roomItemGameObjectList;
    public List<string> roomNameList;
    public List<int> connectPlayerList;
    public List<int> maxPlayersList;

    void Awake()
    {
        PhotonNetwork.GameVersion = version;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings(); // ���� Ŭ���忡 ����
        }

        roomName.text = "ROOM _" + Random.Range(0, 999).ToString(); // �� �̸��� �������� ����
    }

    // ������ ������ ���� �������� �� �ڵ����� ȣ��Ǵ� �ݹ� �Լ�
    public override void OnConnectedToMaster()
    {
        Debug.Log("Enter Lobby");
        // PhotonNetwork.JoinRandomRoom(); // ������ ���� �����ϴ� ���� �濡 ���� �õ�
        PhotonNetwork.JoinLobby();
        userId.text = GetUserId();

    }

    string GetUserId()
    {
        string userId = PlayerPrefs.GetString("USER_ID");
        if (string.IsNullOrEmpty(userId))
        {
            userId = "USER_" + Random.Range(0, 999).ToString(); // ID�� �ƹ� �Է� ���� ���� ������ �ڵ����� ��������
        }
        return userId;
    }

    // �������� �� ������ �õ��Ͽ����� ���� ���� ��
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No Rooms");
        // ���� 20�� ¥�� ���� ����
        PhotonNetwork.CreateRoom("My Room", new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Enter Room");
        // CreateTank();
        StartCoroutine(LoadBattlefield());
    }

/*    void CreateTank()
    {
        float pos = Random.Range(-100.0f, 100.0f);
        PhotonNetwork.Instantiate("Tank", new Vector3(pos, 20.0f, pos), Quaternion.identity);
    }
*/

    public void OnClickJoinRandomRoom() //  Join Random Room ��ư ������ ȣ�� 10.31
    {
        PhotonNetwork.NickName = userId.text; // ���� �÷��̾� �̸� ����
        PlayerPrefs.SetString("USER_ID", userId.text); // �÷��̾� �̸� ����
        PhotonNetwork.JoinRandomRoom(); // �������� ����� ������ ����
    }

    // Create Room ��ư�� ������ �޼���
    public void OnClickCreateRoom()
    {
        // ����ڰ� �Է��� ���̸����� ������ ����
        string _roomName = roomName.text;
        
        if (string.IsNullOrEmpty(roomName.text))
        {
            // ����ڰ� ���̸��� �Է����� �ʾҴٸ� �������� ����
            _roomName = "Room_" + Random.Range(0, 999).ToString();
        }
        
        PhotonNetwork.NickName = userId.text; // ����� �̸� ����
        PlayerPrefs.SetString("USER_ID", userId.text);
        
        // ������ ���� ������ ����
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 20;
        PhotonNetwork.CreateRoom(_roomName, roomOptions, TypedLobby.Default);
    }

    public void OnClickQuitRomm()
    {
/*        RoomOptions roomOptions = 
        roomOptions.IsOpen = false;
        roomOptions.IsVisible = false;
*/    }

    void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        Debug.Log("Create Room Failed" + codeAndMsg);
    }

    IEnumerator LoadBattlefield()
    {
        PhotonNetwork.IsMessageQueueRunning = false; // ���� �̵��ϴµ��� ���� Ŭ���� �����κ��� ��Ʈ��ũ �޽��� ���� �ߴ�
        AsyncOperation ao = Application.LoadLevelAsync(1); // ��׶��� �� �ε�, ������ӿ��� �����Ʈ��ũ ���� ��� �� ��ȯ ������ ���⼱ �������� �� �۾� ���ذ�
        yield return ao;
    }

    void Update()
    {
        logText.text = PhotonNetwork.NetworkClientState.ToString();
    }

    // �� ����� �޾ƿ��� �Լ� // ������ �� ����� "����"�Ǿ��� �� ȣ��Ǵ� �ݹ��Լ�, �׷��� ���� ������ ���� �߰��� �Ǵ� ���� ����, ���� ����� �ڷᱸ���� ����Ʈ��
    // �κ� �������� ȣ��Ǵ� �Լ�
    //
    // ȣ��Ǵ� ���
    // -�κ� ���� ��
    // -���ο� ���� ������� ���
    // -���� �����Ǵ� ���
    // -���� IsOpen ���� ��ȭ�� ���(�ƿ� RoomInfo �� �����Ͱ� �ٲ�� ��� ��ü�� ���� �ֽ��ϴ�)
    //
    // !!!! �κ� ù ���� �� ��� ���� ������ roomList ����Ʈ�� ��ƿ�
    // !!!! �κ� ������ ������ ��� ������ �Ͼ �� �ϳ��� ������ roomList ����Ʈ �ȿ� RoomInfo ������ ������� ����� ��
    // !!!!
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        /*        // ���� ���� �ڵ�
                foreach (RoomInfo _room in roomList)
                {
                    Debug.Log(_room.Name);
                    GameObject room = Instantiate(roomItem);
                    // ������ RoomItem �������� Parent�� ����
                    room.transform.SetParent(scrollContent.transform, false);

                    RoomData roomData = room.GetComponent<RoomData>();
                    roomData.roomName = _room.Name;
                    roomData.connectPlayer = _room.PlayerCount;
                    roomData.maxPlayers = _room.MaxPlayers;
                    // �ؽ�Ʈ ������ ǥ��
                    roomData.DisplayRoomData();
                }
        */


        // �÷��̾ 0�� ���� GameObject�� ������Ű�� �˰������� ����
        // ��ȭ�� ������ �ϳ� ���� �����鸸 roomList ����Ʈ�� ����� ��
        // �� �̸��� ������ ���� �߻�?, RemovedFromList �ʵ� �����Ͽ� �ؾ��� ��, RemovedFromList�� Ǯ���̾ ����Ʈ���� ���������� true ����
        // �� ������ ��ȭ �߻��� roomList �ε����� �˾Ƴ��� �ε����θ� ó��?
        // �ϳ��� �� ������ ��ȭ �߻��� �������� �ε��� ������ 1 �ƴѰ�?
        foreach (RoomInfo _room in roomList)
        {
            // ��ȭ�� ������ �濡 ����� ������
            if (_room.PlayerCount >= 1)
            {
                Debug.Log(_room.Name + " PlayerCount : " + _room.PlayerCount);
                
                // �� ���� ����Ʈ���� �˻� �õ�
                int roomIndex = -1;
                roomIndex = roomNameList.FindIndex(name => name.Contains(_room.Name));

                // �� ���� ����Ʈ���� �˻��� �Ǹ� �ε��� �����ؼ� �ؽ�Ʈ�� ����
                if (roomIndex >= 0)
                {
                    RoomData roomData = roomItemGameObjectList[roomIndex].GetComponent<RoomData>();
                    roomData.roomName = _room.Name;
                    roomData.connectPlayer = _room.PlayerCount;
                    roomData.maxPlayers = _room.MaxPlayers;

                    // �ؽ�Ʈ ������ �ݿ������ֱ�
                    roomData.DisplayRoomData();
                }
                // �� ���� ����Ʈ���� �˻��� �ȵǸ�
                else
                {
                    // scrollContent�� �ڽ����� �ν��Ͻ�ȭ���� �� ��Ͽ� ǥ�ý��� ��
                    GameObject room = Instantiate(roomItem, scrollContent.transform);

                    // ������Ʈ����Ʈ�� Add
                    roomItemGameObjectList.Add(room); // �ν��Ͻ�ȭ ��Ų roomItem�� ���ӿ�����Ʈ ����Ʈ�� Add

                    // ��� ����Ʈ�� ���� roomItem ������Ʈ���� RoomData ������Ʈ ��������, �� ���� �־��ֱ�
                    RoomData roomData = room.GetComponent<RoomData>();
                    roomData.roomName = _room.Name;
                    roomNameList.Add(_room.Name); // �˻����� �� �� �̸� ����Ʈ�� Add
                    roomData.connectPlayer = _room.PlayerCount;
                    roomData.maxPlayers = _room.MaxPlayers;

                    // �ؽ�Ʈ ������ �ݿ������ֱ�
                    roomData.DisplayRoomData();

                    // RoomItem�� Button ������Ʈ�� Ŭ�� �̺�Ʈ�� �������� ����
                    roomData.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnClickRoomItem(roomData.roomName); }); // �͸� �޼��� �̿�
                }
            }

            // ��ȭ�� ������ ���� 0���� ���̸� (�κ񿡼� ����� �ο��� 0�� ���� ã�Ƴ��� ����Ʈ���� ����)
            if (_room.PlayerCount == 0)
            {
                Debug.Log(_room.Name + " PlayerCount : 0");

                // ���̸� ������ ����Ʈ���� �˻� �� �ε����� ã�Ƴ��� �ε����� ������� ������Ʈ Destroy, ������Ʈ����Ʈ���� Remove
                int roomIndex = roomNameList.FindIndex(name => name.Contains(_room.Name));
                if(roomItemGameObjectList.Count > 0) // �� ȥ���� �濡�� �κ�� ���ư��� �� ���� ���� ����ó���� ����
                {
                    Destroy(roomItemGameObjectList[roomIndex]); // ���̾��Ű���� ����

                    // ����Ʈ���� ����
                    roomItemGameObjectList.Remove(roomItemGameObjectList[roomIndex]);
                    roomNameList.Remove(roomNameList[roomIndex]);
                }
            }
        }
    }

    // RoomItem ������ ���� ������ �Լ�
    private void OnClickRoomItem(string roomName)
    {
        PhotonNetwork.NickName = userId.text; // ���� ���̵� ����
        PlayerPrefs.SetString("USER_ID", userId.text);
        PhotonNetwork.JoinRoom(roomName);
    }
}
