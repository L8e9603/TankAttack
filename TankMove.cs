using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
// using UnityStandardAssets.Utility; // 옛날 유니티 기본 애셋이었음, Camera.main.GetComponent<SmoothFollow>().target = camPivot; 쓰려고 선언한 네임스페이스
using Cinemachine;

public class TankMove : MonoBehaviourPun, IPunObservable
{
    [HideInInspector]
    public bool isDie = false; // true면 전차를 천천히 멈춤

    [SerializeField]
    private float defaultMoveSpeed = 3.5f;
    [SerializeField]
    private float accelerationMoveSpeed = 5f;
    [SerializeField]
    private float currentMoveSpeed;
    
    public float rotSpeed = 40.0f; // 터렛 stabilizeSpeed 속도를 차체 회전속도에 종속 시키기 위해 public 선언
    
    private Rigidbody rbody;
    private Transform tr;
    public float h, v; // 사용자 키 입력을 받기 위한 변수
    private PhotonView pv = null; // 포톤 뷰 컴포넌트

    // 원격탱크의 정보를 송수신할 때 사용할 변수 선언 및 초기화
    private Vector3 bodyCurrentPosition = Vector3.zero; // 원격 탱크 차체의 좌표
    private Quaternion bodyCurrentRotation = Quaternion.identity; // 원격 탱크 차체의 회전 값

    private Vector2 rTrackOffset = Vector2.zero; // 원격 탱크의 우측 무한궤도 Offset 값
    private Vector2 lTrackOffset = Vector2.zero;
    private Quaternion[] rWheelCurrentRotation = new Quaternion[15];
    private Quaternion[] lWheelCurrentRotation = new Quaternion[15];

    // 무한궤도 관련 변수
    [SerializeField]
    private float trackScrollSpeed = 0.5f; // 무한궤도 텍스처 Offset 스크롤 속도

    [SerializeField]
    private Renderer _rendererR;

    [SerializeField]
    private Renderer _rendererL;

    // 휠 관련 변수, localRotation 값이 증가하면 전진 연출
    [SerializeField]
    private Transform[] wheelR;

    [SerializeField]
    private Transform[] wheelL;

    [SerializeField]
    private float wheelRotSpeed = 100f;
    private float wheelRotAddSpeed = 300f;

    private bool isCollision; // 탱크의 충돌 상태를 저장하는 변수, 충돌상태이면 쉬프트 키로 가속되지 않음

    [SerializeField]
    private Transform turretTransform; // 터렛 - 바디 정렬에 쓸 변수
    private RaycastHit hit; // 광선이 지면에 맞은 위치를 저장할 변수

    [SerializeField]
    private AudioSource engineAudioSource; // 엔진음 무한루핑 오디오소스

    // 전투지역 복귀 관련 변수
    private float countdown = 11f;
    private bool isInCombatArea = true;
    public bool isDeserted = false;
    public bool isCoroutineStarted = false;

    // 엔진 연기
    [SerializeField]
    private ParticleSystem engineSmokeParticleSystem_L;
    [SerializeField]
    private ParticleSystem engineSmokeParticleSystem_R;
    private float originMaxParticles = 50f;
    private float originGravityModifire = -0.02f;

    void Awake() // start->Awake로 바꿈, 스타트가 실행되기 전에 OnStartPhotonSer~~ 실행되면 에러 발생함
    {
        currentMoveSpeed = defaultMoveSpeed;

        rbody = GetComponent<Rigidbody>();
        tr = GetComponent<Transform>();
        pv = GetComponent<PhotonView>();
        // 데이터 전송 타입을 설정
        pv.Synchronization = ViewSynchronization.Unreliable; // 포톤뷰 컴포넌트가 가진 필드, (드롭다운 메뉴 위부터 순서대로 Off(RPC로 데이터 송수신 할 때), TCP, UDP, 변화가 생길때만)
        //ObservedComponents 속성에 TankMove 스크립트를 연결함
        pv.ObservedComponents[0] = this;

        if (pv.IsMine)// 로컬인지 아닌지 검사 후 메인 카메라가 로컬만 따라다니도록 함
        {
            GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<CopyPosition>().target = this.transform; // 미니맵 카메라가 로컬을 따라다니도록 설정

            //Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().Follow = tpsCameraPosition;

            /*            Camera.main.GetComponent<SmoothFollow>().target = camPivot;
                        // 무게중심을 낮게 설정
                        rbody.centerOfMass = new Vector3(0.0f, -0.5f, 0.0f);
            */
        }
        else
        {
            // 원격 탱크는 물리력을 이용하지 않음
            //rbody.isKinematic = true;
        }
        // 원격 탱크의 위치 및 회전 값을 처리할 변수의 초기값 설정
        bodyCurrentPosition = tr.position; // 원격 탱크(나의 분신)의 좌표가 될 변수에 현재 내 탱크의 좌표를 복사해줌
        bodyCurrentRotation = tr.rotation; // 뿐만 아니라 나중에 들어온 플레이어는 내 위치를 모르기 때문에 내 위치를 서버에 송신해주고 나중에 들어온 플레이어는 서버에서 내 위치값을 수신받아 원격의 내 위치를 알아냄

        rTrackOffset = _rendererR.material.mainTextureOffset;
        lTrackOffset = _rendererL.material.mainTextureOffset;

        for (int i = 0; i < wheelR.Length; i++) // 로컬 탱크의 오른쪽 바퀴 수 만큼 반복 수행
        {
            rWheelCurrentRotation[i] = wheelR[i].localRotation; // 원격 탱크의 회전값이 될 변수에 로컬 탱크 바퀴의 회전값 복사
        }
        for (int i = 0; i < wheelL.Length; i++) // 로컬 탱크의 왼쪽 바퀴 수 만큼 반복 수행
        {
            lWheelCurrentRotation[i] = wheelL[i].localRotation; // 원격 탱크의 회전값이 될 변수에 로컬 탱크 바퀴의 회전값 복사
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(countdown);

        // 로컬이면
        if (pv.IsMine)
        {
            if (!isDie)
            {
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
            else if (isDie)
            {
                // 관성에 의해 서서히 멈추는 효과 연출
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * 0.5f);
                h = Mathf.Lerp(h, 0f, Time.deltaTime * 1f);
                v = Mathf.Lerp(v, 0f, Time.deltaTime * 1f);

            }

            // 탱크의 속도에 따른 엔진음 변화
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, 1f + (currentMoveSpeed * 1f / accelerationMoveSpeed * (Mathf.Abs(v) + Mathf.Abs(h) * 1f / 2f)), Time.deltaTime * 5f); // pitch를 최대 2까지 올림, currentMoveSpeed * 1f / accelerationMoveSpeed의 최대값은 1, 축의 값은 절대값으로 변환

            // 탱크의 속도에 따른 엔진 연기 변화
            engineSmokeParticleSystem_L.maxParticles = (int)Mathf.Lerp(engineSmokeParticleSystem_L.maxParticles, originMaxParticles + 1000f * (currentMoveSpeed / accelerationMoveSpeed), Time.deltaTime * 10f); // 기본 파티클수 40에 가속하면 1000까지 증가
            engineSmokeParticleSystem_R.maxParticles = (int)Mathf.Lerp(engineSmokeParticleSystem_R.maxParticles, originMaxParticles + 1000f * (currentMoveSpeed / accelerationMoveSpeed), Time.deltaTime * 10f); // 기본 파티클수 40에 가속하면 1000까지 증가
            engineSmokeParticleSystem_L.gravityModifier = Mathf.Lerp(engineSmokeParticleSystem_L.gravityModifier, originGravityModifire - 0.1f * (currentMoveSpeed / accelerationMoveSpeed) , Time.deltaTime * 5f);
            engineSmokeParticleSystem_R.gravityModifier = Mathf.Lerp(engineSmokeParticleSystem_L.gravityModifier, originGravityModifire - 0.1f * (currentMoveSpeed / accelerationMoveSpeed) , Time.deltaTime * 5f);
            // Debug.Log("MaxParticles : "+ engineSmokeParticleSystem_L.maxParticles + " / " + engineSmokeParticleSystem_L.gravityModifier);

            // 축 입력이 감지되면 가속 시작
            if (h != 0 || v != 0)
            {
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, defaultMoveSpeed, Time.deltaTime * defaultMoveSpeed / 2.0f);
            }
            else
            {
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * defaultMoveSpeed / 2.0f);
            }

            // 전차의 이동과 회전
            Acceleration(); // 쉬프트 키가 감지되면 currentMoveSpeed = moveSpeed * float

            tr.Translate(Vector3.forward * v * currentMoveSpeed * Time.deltaTime); // 전진후진

/*            // 스틱형 탱크의 회전
            tr.Rotate(Vector3.up * rotSpeed * h * Time.deltaTime); // 회전
*/
            // 핸들형 탱크의 회전
            if (v >= 0)
            {
                tr.Rotate(Vector3.up * rotSpeed * h * Time.deltaTime); // 전진중 회전 또는 제자리 회전
            }
            else if (v < 0)
            {
                tr.Rotate(Vector3.up * rotSpeed * -h * Time.deltaTime); // 후진중이면 회전 반대로하여 자동차 핸들처럼 조작
            }

            // 무한궤도 애니메이션
            TracksAndWheelsAnimation();
            
            // 전투지역을 벗어나면 카운트다운 시작
            ReturnToCombatAreaCountdown();
        }

        else // 원격 탱크(나의 분신)의 이동과 회전
        {
            tr.position = Vector3.Lerp(tr.position, bodyCurrentPosition, Time.deltaTime * 3.0f);
            tr.rotation = Quaternion.Slerp(tr.rotation, bodyCurrentRotation, Time.deltaTime * 3.0f);

            // 원격 탱크의 무한궤도 회전
            _rendererR.material.mainTextureOffset = Vector2.Lerp(_rendererR.material.mainTextureOffset, rTrackOffset, Time.deltaTime * 3.0f);
            _rendererL.material.mainTextureOffset = Vector2.Lerp(_rendererL.material.mainTextureOffset, lTrackOffset, Time.deltaTime * 3.0f);

            // 원격 탱크의 오른쪽 휠 회전
            for (int i = 0; i < wheelR.Length; i++)
            {
                wheelR[i].localRotation = Quaternion.Slerp(wheelR[i].localRotation, rWheelCurrentRotation[i], Time.deltaTime * 3.0f);
            }
            for (int i = 0; i < wheelL.Length; i++)
            {
                wheelL[i].localRotation = Quaternion.Slerp(wheelL[i].localRotation, lWheelCurrentRotation[i], Time.deltaTime * 3.0f);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 로컬 플레이어의 위치 정보 송신
        {
            stream.SendNext(tr.position);
            stream.SendNext(tr.rotation);

            stream.SendNext(_rendererR.material.mainTextureOffset);
            stream.SendNext(_rendererL.material.mainTextureOffset);

            // 바퀴의 회전값 전송
            for (int i = 0; i < wheelR.Length; i++)
            {
                stream.SendNext(wheelR[i].localRotation);
            }
            for (int i = 0; i < wheelL.Length; i++)
            {
                stream.SendNext(wheelL[i].localRotation);
            }
        }
        else // 원격 플레이어의 위치 정보 수신
        {
            bodyCurrentPosition = (Vector3)stream.ReceiveNext();
            bodyCurrentRotation = (Quaternion)stream.ReceiveNext();

            // 좌, 우 트랙의 Vector2 머티리얼 Offset 정보 수신
            rTrackOffset = (Vector2)stream.ReceiveNext();
            lTrackOffset = (Vector2)stream.ReceiveNext();

            // 좌,우 바퀴의 회전 쿼터니언 정보 수신
            for (int i = 0; i < wheelR.Length; i++)
            {
                rWheelCurrentRotation[i] = (Quaternion)stream.ReceiveNext();
            }
            for (int i = 0; i < wheelL.Length; i++)
            {
                lWheelCurrentRotation[i] = (Quaternion)stream.ReceiveNext();
            }
        }
    }

    // 키 입력별 무한궤도, 휠 회전 애니메이션 처리
    private void TracksAndWheelsAnimation()
    {
        //Debug.Log("Vertical : " + Input.GetAxis("Vertical"));
        //Debug.Log("Horizontal : " + Input.GetAxis("Horizontal"));

        // 서로 반대 방향인 방향키를 동시에 입력하였고, 감지된 값의 결과가 0인 경우 애니메이션 연출 하지 않음
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S) && Input.GetAxis("Vertical") == 0)
        {
            return; 
        }
        // 서로 반대 방향인 방향키를 동시에 입력하였고, 감지된 값의 결과가 0인 경우 애니메이션 연출 하지 않음
        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D) && Input.GetAxis("Horizontal") == 0)
        {
            return;
        }


        // 제자리 좌, 우회전 시 무한궤도 회전
        //if (Input.GetKey(KeyCode.A)) // 좌회전
        if (Input.GetAxis("Horizontal") < 0) // 좌회전
        {
            MoveForwardTrackAnim(_rendererR); // 우측 무한궤도 전진 회전
            MoveBackwardTrackAnim(_rendererL); // 좌측 무한궤도 후진 회전
            MoveForwardAllOfWheel(wheelR); // 우측 모든 휠 전진 회전
            MoveBackwardAllOfWheel(wheelL); // 좌측 모든 휠 후진 회전
        }
        //else if (Input.GetKey(KeyCode.D)) // 우회전
        else if (Input.GetAxis("Horizontal") > 0) // 우회전
        {
            MoveBackwardTrackAnim(_rendererR); // 우측 무한궤도 후진 회전
            MoveForwardTrackAnim(_rendererL); // 좌측 무한궤도 전진 회전
            MoveBackwardAllOfWheel(wheelR); // 우측 모든 휠 후진 회전
            MoveForwardAllOfWheel(wheelL); // 좌측 모든 휠 전진 회전
        }

        // 전진, 후진시 무한궤도와 휠 애니메이션
        // 전진시 무한궤도와 휠 애니메이션
        //if (Input.GetKey(KeyCode.W))
        if (Input.GetAxis("Vertical") > 0)
        {
            //if (Input.GetKey(KeyCode.A)) // 전진하면서 좌회전
            if (Input.GetAxis("Horizontal") < 0) // 전진하면서 좌회전
            {
                // 우측 궤도 빠른 회전

                // 좌측 궤도 느린 회전
                MoveForwardTrackAnim(_rendererR); // 우측 무한궤도 전진 회전
                MoveForwardTrackAnim(_rendererL); // 좌측 무한궤도 전진 회전
                MoveForwardAllOfWheel(wheelR); // 우측 모든 휠 전진 회전
                MoveForwardAllOfWheel(wheelL); // 좌측 모든 휠 전진 회전
            }
            //else if (Input.GetKey(KeyCode.D)) // 전진하면서 우회전
            else if (Input.GetAxis("Horizontal") > 0) // 전진하면서 우회전
            {
                // 좌측 궤도 빠른 회전

                // 우측 궤도 느린 회전
                MoveForwardTrackAnim(_rendererR); // 우측 무한궤도 전진 회전
                MoveForwardTrackAnim(_rendererL); // 좌측 무한궤도 전진 회전
                MoveForwardAllOfWheel(wheelR); // 우측 모든 휠 전진 회전
                MoveForwardAllOfWheel(wheelL); // 좌측 모든 휠 전진 회전
            }
            else // 그냥 전진
            {
                MoveForwardTrackAnim(_rendererR); // 우측 무한궤도 전진 회전
                MoveForwardTrackAnim(_rendererL); // 좌측 무한궤도 전진 회전
                MoveForwardAllOfWheel(wheelR); // 우측 모든 휠 전진 회전
                MoveForwardAllOfWheel(wheelL); // 좌측 모든 휠 전진 회전
            }
        }
        // 후진 시 무한궤도와 휠 애니메이션
        //else if (Input.GetKey(KeyCode.S))
        else if (Input.GetAxis("Vertical") < 0)
        {
            //if (Input.GetKey(KeyCode.A)) // 후진하면서 좌회전
            if (Input.GetAxis("Horizontal") < 0) // 후진하면서 좌회전
            {
                // 좌우 궤도 둘다 후진, 우측을 더 빨리 회전
                MoveBackwardTrackAnim(_rendererR); // 우측 무한궤도 후진 회전
                MoveBackwardTrackAnim(_rendererL); // 좌측 무한궤도 후진 회전
                MoveBackwardAllOfWheel(wheelR); // 우측 모든 휠 후진 회전
                MoveBackwardAllOfWheel(wheelL); // 좌측 모든 휠 후진 회전
            }
            //else if (Input.GetKey(KeyCode.D)) // 후진하면서 우회전
            else if (Input.GetAxis("Horizontal") > 0) // 후진하면서 우회전
            {
                // 좌우 궤도 둘다 후진, 좌측을 더 빨리 회전
                MoveBackwardTrackAnim(_rendererR); // 우측 무한궤도 후진 회전
                MoveBackwardTrackAnim(_rendererL); // 좌측 무한궤도 후진 회전
                MoveBackwardAllOfWheel(wheelR); // 우측 모든 휠 후진 회전
                MoveBackwardAllOfWheel(wheelL); // 좌측 모든 휠 후진 회전
            }
            else // 그냥 후진
            {
                MoveBackwardTrackAnim(_rendererR); // 우측 무한궤도 후진 회전
                MoveBackwardTrackAnim(_rendererL); // 좌측 무한궤도 후진 회전
                MoveBackwardAllOfWheel(wheelR); // 우측 모든 휠 후진 회전
                MoveBackwardAllOfWheel(wheelL); // 좌측 모든 휠 후진 회전
            }
        }
    }


    // Track //
    // 무한궤도 회전 메서드 //
    // 무한궤도 전진 회전
    private void MoveForwardTrackAnim(Renderer renderer_RorL)
    {
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1) ; // 전진값만 가져오기
        float offset = vertical * Time.deltaTime * trackScrollSpeed * -1.0f; // Tank v2의 트랙은 Y값이 음수일 때 전진하는 연출이기 때문에 -1을 곱해줌
        renderer_RorL.material.mainTextureOffset = new Vector2(0, offset - Time.time);
    }

    // 무한궤도 후진 회전
    private void MoveBackwardTrackAnim(Renderer renderer_RorL)
    {
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0); // 후진값만 가져오기
        float offset = vertical * Time.deltaTime * trackScrollSpeed * -1.0f; // Tank v2의 트랙은 Y값이 음수일 때 후진하는 연출이기 때문에 -1을 곱해줌
        renderer_RorL.material.mainTextureOffset = new Vector2(0, offset + Time.time);
    }

    // Wheel //
    // 휠 회전 메서드 //
    // 휠 1개만 전진 회전, localEulerAngles.x 값이 증가하면 전진회전
    private void MoveForwardWheelRotation(Transform wheelTransform)
    {
        // 휠 1개를 전진 회전
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1); // 전진값만 가져오기
        float eulerX = vertical * Time.deltaTime * wheelRotSpeed;
        wheelTransform.localEulerAngles = new Vector3(eulerX + Time.time * wheelRotAddSpeed, 0, 0);
    }
    // 좌, 우 중 한 쪽에 있는 모든 휠 전진 회전
    private void MoveForwardAllOfWheel(Transform[] wheelTransform)
    {
        foreach(Transform transform in wheelTransform)
        {
            MoveForwardWheelRotation(transform);
        }
    }

    // 휠 1개만 후진 회전, localEulerAngles.x 값이 감소하면 후진회전
    private void MoveBackwardWheelRotation(Transform wheelTransform)
    {
        // 휠 1개를 후진 회전
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0); // 후진값만 가져오기
        float eulerX = vertical * Time.deltaTime * wheelRotSpeed;
        wheelTransform.localEulerAngles = new Vector3(eulerX - Time.time * wheelRotAddSpeed, 0, 0);
    }
    // 좌, 우 중 한 쪽에 있는 모든 휠 전진 회전
    private void MoveBackwardAllOfWheel(Transform[] wheelTransform)
    {
        foreach (Transform transform in wheelTransform)
        {
            MoveBackwardWheelRotation(transform);
        }
    }

    // 쉬프트 키가 감지되는동안 전차를 가속하는 메서드
    private void Acceleration()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (isCollision) // 무언가와 충돌한 상태라면 가속하지 않음
            {
                return;
            }
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, defaultMoveSpeed * 1.7f, Time.deltaTime * defaultMoveSpeed / 2.0f) ;
        }
    }


    // 충돌 검사 메서드
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ReturnToCombatArea"))
        {
            if (!isDeserted)
            {
                isInCombatArea = false;
            }
            // 탈영처리 되었다면
/*            else if (isDeserted)
            {
                isInCombatArea = true;
            }*/
        }

        if (other.CompareTag("Building"))
        {
            isCollision = true;
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0.5f, Time.deltaTime * defaultMoveSpeed * 10.0f);
            //Debug.Log(other.tag);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ReturnToCombatArea"))
        {
            isInCombatArea = true;
        }

        // Exit 된 콜라이더의 태그가 빌딩이면 다시 가속 할 수 있도록 isCollision을 false로 변경
        if (other.CompareTag("Building"))
        {
            isCollision = false;
        }
    }

    // 전투지역 복귀 카운트다운 메서드
    public void ReturnToCombatAreaCountdown()
    {
        // 전투지역에 있지 않고
        if (!isInCombatArea)
        {
            // 카운트 시간이 0 초과면
            if (countdown > 0)
            {
                countdown -= Time.deltaTime;
                //Mathf.Clamp(countdown, 0f, 11f);
                UIManager.instance.SetActiveReturnToCombatAreaUI(true);
                UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "RETURN TO COMBAT AREA\n" + Mathf.Floor(countdown);
            }
            // 카운트 시간이 0 이하면
            else if (countdown <= 0)
            {
                // 텍스트를 변경해 탈영 알림
                isDeserted = true;
                UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "RETURN TO COMBAT AREA\n0";

                if (isDeserted)
                {
                    StartDeserted();
                }
            }
        }
        // 전투지역 내에 있다면
        else if (isInCombatArea)
        {
            UIManager.instance.SetActiveReturnToCombatAreaUI(false);
            countdown = 11f;
        }
    }

    private void StartDeserted()
    {
        if (isCoroutineStarted)
        {
            return;
        }

        StartCoroutine(Deserted());
    }

    private IEnumerator Deserted()
    {
        Debug.Log("전투지역 벗어남, Deserted 코루틴 실행");

        isCoroutineStarted = true; // 코루틴 한번만 실행되게 함

        UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "DESERTED";

        //카메라 조작 비활성화
        Camera.main.GetComponent<CameraMovement>().enabled = false;

        // 탱크 조작 비활성화
        isDie = true;
        GetComponentInChildren<TurretControl>().enabled = false;
        GetComponentInChildren<CannonControl>().enabled = false;
        GetComponentInChildren<FireCannon>().enabled = false;
        UIManager.instance.tpsUI.SetActive(false); // TPSUI인 크로스헤어와 실제피탄지점 크로스헤어 비활성화

        // 1인칭 상태였다면 1인칭 UI를 끄고, 3인칭 카메라로 전환
        if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = true)
        {
            UIManager.instance.fpsUI.SetActive(false);
            Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = false;
            Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = true;
        }

        // 카메라 전환 방법은 EaseIn으로, 카메라 전환 소요시간 설정
        Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseIn, 3f);

        // 데스캠으로 전환
        Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = false;
        Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = false;
        Camera.main.GetComponent<CameraMovement>().deathCinemachineVirtualCamera.enabled = true;

        isInCombatArea = true;
        UIManager.instance.returnToCombatAreaUI.SetActive(false); // 전투지역 벗어남 UI 비활성화

        StartCoroutine(GetComponent<TankDamage>().RespawnCoroutine()); // 리스폰 진행
        
        yield return null;
    }
}

