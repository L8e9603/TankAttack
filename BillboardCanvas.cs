using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    private Transform tr;
    private Transform mainCameraTr;

    // Start is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
        mainCameraTr = Camera.main.transform; // �������� �ȿ� �ִ� ���� ī�޶� Ʈ������ ������Ʈ ����
    }

    // Update is called once per frame
    void Update()
    {
        tr.LookAt(mainCameraTr); // UI�� ���� ī�޶� ��������
    }
}
