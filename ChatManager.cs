using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class ChatManager : MonoBehaviourPunCallbacks
{
    public GameObject m_Content;
    public TMP_InputField m_inputField;

    PhotonView pV;

    GameObject m_ContentText;

    string m_strUserName;


    void Awake()
    {
        // Screen.SetResolution(960, 600, false);
        // PhotonNetwork.ConnectUsingSettings(); // 0번 씬에서 포톤 접속 했으니 생략한다
        m_ContentText = m_Content.transform.GetChild(0).gameObject;
        pV = GetComponent<PhotonView>();
        AddChatMessage("User Connected : " + PhotonNetwork.LocalPlayer.NickName); // 접속한 유저 닉네임 채팅창에 표시
    }

    void Update()
    {
        if (GameManager.instance.isActiveLeaveGameUI)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.T) && m_inputField.isFocused == false)
        {
            m_inputField.enabled = true;

            m_inputField.ActivateInputField();
        }
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Success To Enter Chat Lobby");

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 20;

        int nRandomKey = Random.Range(0, 100);

        //m_strUserName = "user" + nRandomKey;

        PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("USER_ID");
        PhotonNetwork.JoinOrCreateRoom("Room1", options, null);
    }

    public override void OnJoinedRoom()
    {
        AddChatMessage("connect user : " + PhotonNetwork.LocalPlayer.NickName);
    }

    // 메세지 입력 후 엔터를 누르면 메세지를 보냄
    public void OnEndEditEvent()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // 인풋 필드가 비어있으면 공백 메세지를 보내지 않고 함수 종료
            if (m_inputField.text == "")
            {
                m_inputField.enabled = false;
                return;
            }

            string strMessage = PhotonNetwork.LocalPlayer.NickName + " : " + m_inputField.text;

            pV.RPC("RPC_Chat", RpcTarget.All, strMessage);
            m_inputField.text = ""; // 인풋필드를 비워줌
            m_inputField.enabled = false;
        }
    }

    void AddChatMessage(string message)
    {
        GameObject goText = Instantiate(m_ContentText, m_Content.transform);

        goText.GetComponent<TextMeshProUGUI>().text = message;
        m_Content.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

    }

    [PunRPC]
    void RPC_Chat(string message)
    {
        AddChatMessage(message);
    }

}

