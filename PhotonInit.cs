using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class PhotonInit : MonoBehaviourPunCallbacks // 포톤이 가진 콜백함수 사용을 위해서는 MonoBehaviourPunCallbacks 클래스를 상속받아야함
{
    public string version = "v1.0";
    public Text logText; // 접속 로그를 표시할 텍스트
    public InputField userId; // 사용자 이름을 입력하는 UI 항목 연결

    public InputField roomName; // 룸 이름을 입력받을 UI 항목 연결 변수

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
            PhotonNetwork.ConnectUsingSettings(); // 포톤 클라우드에 접속
        }

        roomName.text = "ROOM _" + Random.Range(0, 999).ToString(); // 방 이름을 무작위로 설정
    }

    // 마스터 서버에 접속 성공했을 때 자동으로 호출되는 콜백 함수
    public override void OnConnectedToMaster()
    {
        Debug.Log("Enter Lobby");
        // PhotonNetwork.JoinRandomRoom(); // 서버에 접속 성공하는 순간 방에 입장 시도
        PhotonNetwork.JoinLobby();
        userId.text = GetUserId();

    }

    string GetUserId()
    {
        string userId = PlayerPrefs.GetString("USER_ID");
        if (string.IsNullOrEmpty(userId))
        {
            userId = "USER_" + Random.Range(0, 999).ToString(); // ID에 아무 입력 없이 조인 했으면 자동으로 생성해줌
        }
        return userId;
    }

    // 랜덤으로 방 접속을 시도하였으나 실패 했을 때
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No Rooms");
        // 정원 20명 짜리 방을 생성
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

    public void OnClickJoinRandomRoom() //  Join Random Room 버튼 누르면 호출 10.31
    {
        PhotonNetwork.NickName = userId.text; // 로컬 플레이어 이름 지정
        PlayerPrefs.SetString("USER_ID", userId.text); // 플레이어 이름 저장
        PhotonNetwork.JoinRandomRoom(); // 무작위로 추출된 룸으로 입장
    }

    // Create Room 버튼과 연결할 메서드
    public void OnClickCreateRoom()
    {
        // 사용자가 입력한 방이름으로 방제목 설정
        string _roomName = roomName.text;
        
        if (string.IsNullOrEmpty(roomName.text))
        {
            // 사용자가 방이름을 입력하지 않았다면 무작위로 설정
            _roomName = "Room_" + Random.Range(0, 999).ToString();
        }
        
        PhotonNetwork.NickName = userId.text; // 사용자 이름 설정
        PlayerPrefs.SetString("USER_ID", userId.text);
        
        // 생성할 룸의 조건을 설정
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
        PhotonNetwork.IsMessageQueueRunning = false; // 씬을 이동하는동안 포톤 클라우드 서버로부터 네트워크 메시지 수진 중단
        AsyncOperation ao = Application.LoadLevelAsync(1); // 백그라운드 씬 로딩, 좀비게임에선 포톤네트워크 내부 기능 씬 전환 썻지만 여기선 수동으로 그 작업 해준거
        yield return ao;
    }

    void Update()
    {
        logText.text = PhotonNetwork.NetworkClientState.ToString();
    }

    // 룸 목록을 받아오는 함수 // 생성된 룸 목록이 "변경"되었을 때 호출되는 콜백함수, 그래서 방을 나가면 방이 추가가 되는 버그 있음, 방을 만들면 자료구조인 리스트에
    // 로비에 있을때만 호출되는 함수
    //
    // 호출되는 경우
    // -로비에 접속 시
    // -새로운 룸이 만들어질 경우
    // -룸이 삭제되는 경우
    // -룸의 IsOpen 값이 변화할 경우(아예 RoomInfo 내 데이터가 바뀌는 경우 전체일 수도 있습니다)
    //
    // !!!! 로비에 첫 접속 시 모든 방의 정보를 roomList 리스트에 담아옴
    // !!!! 로비에 접속한 상태인 경우 변경이 일어난 룸 하나의 정보만 roomList 리스트 안에 RoomInfo 정보가 담겨져에 담겨져 옴
    // !!!!
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        /*        // 수업 원본 코드
                foreach (RoomInfo _room in roomList)
                {
                    Debug.Log(_room.Name);
                    GameObject room = Instantiate(roomItem);
                    // 생성한 RoomItem 프리팹의 Parent를 지정
                    room.transform.SetParent(scrollContent.transform, false);

                    RoomData roomData = room.GetComponent<RoomData>();
                    roomData.roomName = _room.Name;
                    roomData.connectPlayer = _room.PlayerCount;
                    roomData.maxPlayers = _room.MaxPlayers;
                    // 텍스트 정보를 표시
                    roomData.DisplayRoomData();
                }
        */


        // 플레이어가 0인 방의 GameObject는 삭제시키는 알고리즘으로 개선
        // 변화가 감지된 하나 방의 정보들만 roomList 리스트에 담겨져 옴
        // 방 이름이 같으면 버그 발생?, RemovedFromList 필드 응용하여 해야할 듯, RemovedFromList는 풀방이어도 리스트에서 지워버려서 true 리턴
        // 방 정보에 변화 발생시 roomList 인덱스를 알아내서 인덱스로만 처리?
        // 하나의 방 정보만 변화 발생시 가져오면 인덱스 무조건 1 아닌가?
        foreach (RoomInfo _room in roomList)
        {
            // 변화가 감지된 방에 사람이 있으면
            if (_room.PlayerCount >= 1)
            {
                Debug.Log(_room.Name + " PlayerCount : " + _room.PlayerCount);
                
                // 방 제목 리스트에서 검색 시도
                int roomIndex = -1;
                roomIndex = roomNameList.FindIndex(name => name.Contains(_room.Name));

                // 방 제목 리스트에서 검색이 되면 인덱스 참조해서 텍스트만 갱신
                if (roomIndex >= 0)
                {
                    RoomData roomData = roomItemGameObjectList[roomIndex].GetComponent<RoomData>();
                    roomData.roomName = _room.Name;
                    roomData.connectPlayer = _room.PlayerCount;
                    roomData.maxPlayers = _room.MaxPlayers;

                    // 텍스트 정보를 반영시켜주기
                    roomData.DisplayRoomData();
                }
                // 방 제목 리스트에서 검색이 안되면
                else
                {
                    // scrollContent의 자식으로 인스턴스화시켜 방 목록에 표시시켜 줌
                    GameObject room = Instantiate(roomItem, scrollContent.transform);

                    // 오브젝트리스트에 Add
                    roomItemGameObjectList.Add(room); // 인스턴스화 시킨 roomItem을 게임오브젝트 리스트에 Add

                    // 방금 리스트에 넣은 roomItem 오브젝트에서 RoomData 컴포넌트 가져오고, 방 정보 넣어주기
                    RoomData roomData = room.GetComponent<RoomData>();
                    roomData.roomName = _room.Name;
                    roomNameList.Add(_room.Name); // 검색으로 쓸 방 이름 리스트에 Add
                    roomData.connectPlayer = _room.PlayerCount;
                    roomData.maxPlayers = _room.MaxPlayers;

                    // 텍스트 정보를 반영시켜주기
                    roomData.DisplayRoomData();

                    // RoomItem의 Button 컴포넌트에 클릭 이벤트를 동적으로 연결
                    roomData.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { OnClickRoomItem(roomData.roomName); }); // 익명 메서드 이용
                }
            }

            // 변화가 감지된 방이 0명인 방이면 (로비에서 대기중 인원이 0인 방을 찾아내면 리스트에서 제거)
            if (_room.PlayerCount == 0)
            {
                Debug.Log(_room.Name + " PlayerCount : 0");

                // 방이름 가지고 리스트에서 검색 후 인덱스를 찾아내고 인덱스를 기반으로 오브젝트 Destroy, 오브젝트리스트에서 Remove
                int roomIndex = roomNameList.FindIndex(name => name.Contains(_room.Name));
                if(roomItemGameObjectList.Count > 0) // 나 혼자인 방에서 로비로 돌아갔을 때 에러 방지 예외처리용 조건
                {
                    Destroy(roomItemGameObjectList[roomIndex]); // 하이어라키에서 삭제

                    // 리스트에서 삭제
                    roomItemGameObjectList.Remove(roomItemGameObjectList[roomIndex]);
                    roomNameList.Remove(roomNameList[roomIndex]);
                }
            }
        }
    }

    // RoomItem 생성시 동적 연결할 함수
    private void OnClickRoomItem(string roomName)
    {
        PhotonNetwork.NickName = userId.text; // 유저 아이디 설정
        PlayerPrefs.SetString("USER_ID", userId.text);
        PhotonNetwork.JoinRoom(roomName);
    }
}
