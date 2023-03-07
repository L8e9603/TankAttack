using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class DisplayUserID : MonoBehaviourPun
{
    public Text userID;
    private PhotonView pv = null;

    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();
        userID.text = pv.Owner.NickName; // ����� �̸��� HUD UI�� ǥ��
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
