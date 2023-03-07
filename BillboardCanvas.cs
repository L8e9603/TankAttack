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
        mainCameraTr = Camera.main.transform; // 스테이지 안에 있는 메인 카메라 트랜스폼 컴포넌트 추출
    }

    // Update is called once per frame
    void Update()
    {
        tr.LookAt(mainCameraTr); // UI를 메인 카메라 방향으로
    }
}
