using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon;
using Cinemachine;

public class TankDamage : LivingEntity, IPunObservable
{
    private MeshRenderer[] renderers; // 탱크의 투명처리를 위한 렌더러 오브젝트
    private GameObject destroyEffect = null; // 탱크가 체력이 0이 되어 폭발할 때 이펙트
    /*    private int initHp = 100; // 최초 체력
        private int currHp = 0; // 현재 체력
    */

    private PhotonView pv;
    public Canvas hudCanvas;
    public Image hpBar;

    [SerializeField]
    private GameObject destoyedTank;

    private TankMove tankMove; // 체력이 0일 때 탱크 조작 제어를 막기 위한 변수
    private FireCannon fireCannon; // 체력이 0일 때 캐논 발사를 막기 위한 변수

    private AudioSource audioSource;
    [SerializeField]
    private AudioClip[] hitClip; // 히트 사운드 다양화, 히트당한 오브젝트의 AudioSource 컴포넌트로 재생시킴
    private AudioClip[] ricochetClip; // 히트 판정음으로 쓸 도탄효과음, 히트시킨 오브젝트의 AudioSource 컴포넌트로 재생시킴
    private int selectedHitClipIndex; // 난수 발생 후 해당하는 인덱스 번호의 hitClip 재생

    // 리스폰 관련 변수
    private int selectedSpawnPointIndex;


    private float cameraBlendTime = 2.5f;

    [SerializeField]
    private Transform turretTransform;

    private CameraMovement cameraMovement;

    private float respawnCountdown = 6f;

    public bool isReadyToSpawn = false; // 로컬 리스폰 준비 여부
    public bool IsReadyToSpawnCurrent = false; // 원격 탱크 리스폰 준비 여부

    private bool isDeathCamCoroutineEnd = false;
    private bool isRespawnCamCoroutineEnd = false;

    public int kill = 0;
    public int death = 0;


    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        health = startingHealth; // 현재 체력을 초기화, LivingEntity로부터 상속받은 변수
        hpBar.color = Color.green; // Filled 이미지 색상을 녹색으로 설정
        destroyEffect = Resources.Load<GameObject>("Explosion_TankDestroy_FX"); // 탱크 폭발 파티클 시스템 프리팹 로드
        renderers = GetComponentsInChildren<MeshRenderer>(); // Tank 오브젝트의 자식인 BodyMesh의 메쉬렌더러를 이용하여 투명처리
        tankMove = GetComponent<TankMove>();
        fireCannon = GetComponent<FireCannon>();
        audioSource = GetComponent<AudioSource>();
        cameraMovement = Camera.main.GetComponent<CameraMovement>();
    }

    [PunRPC]
    public override void OnDamage(float damage)
    {
        //Debug.Log(PhotonNetwork.NickName + "hit, left health = " + health);

        // 피격 효과음 클립 랜덤 재생
        selectedHitClipIndex = Random.Range(0, hitClip.Length);
        audioSource.PlayOneShot(hitClip[selectedHitClipIndex], 1f);

        // LivingEntity의 OnDamage() 실행, 대미지 적용
        base.OnDamage(damage);

        if (health / startingHealth <= 0.4f)
        {
            hpBar.color = Color.red; // 40% 이하는 빨간색
        }
        else if (health / startingHealth <= 0.6f)
        {
            hpBar.color = Color.yellow; // 60% 이하는 노란색
        }
        
    }

    public override void Die()
    {
        base.Die();

        GetComponentInChildren<TurretControl>().enabled = false; // 터렛 조작 비활성화
        GetComponentInChildren<CannonControl>().enabled = false; // 포신 각도 조작 비활성화
        fireCannon.enabled = false; // 캐논 발사 비활성화
        tankMove.isDie = true; // 탱크가 죽을 때 뚝 멈추지 않고 관성이 적용된 것 처럼 천천히 멈춤
        audioSource.PlayOneShot(hitClip[selectedHitClipIndex]); // LivingEntity에서 OnDamage 메서드 안에 Die함수 있어서 가능한 구문?
        StartCoroutine(ExplosionTank()); // 탱크 폭파 코루틴 함수 실행

        StartCoroutine(RespawnCoroutine());
    }

    // 탱크 체력이 0이 되면 폭발을 연출하는 코루틴
    private IEnumerator ExplosionTank() // 폭파 효과 생성 밎 리스폰 코루틴 함수
    {
        isReadyToSpawn = false;
        IsReadyToSpawnCurrent = false;

        hudCanvas.enabled = false; // HUD 비활성화
        //Debug.Log("HUD비활성화");

        SetTankVisible(false); // 파괴되지 않은 탱크 모델링 렌더링 끄기
        //Debug.Log("파괴되지 않은 탱크 모델링 렌더링 끄기");

        destoyedTank.SetActive(true); // 파괴된 탱크 모델링 활성화
        //Debug.Log("파괴된 탱크 모델링 활성화");

        GameObject effect = GameObject.Instantiate(destroyEffect, transform.position + new Vector3(0,0.5f,0), Quaternion.identity); // 탱크 폭발 이펙트 생성
        Destroy(effect, 3.0f); // 이펙트 3초 뒤 파괴

        yield return null;
    }

    public IEnumerator RespawnCoroutine()
    {
        Debug.Log("StartCoroutine(DeathCamAction());");
        StartCoroutine(DeathCamAction());

        while (!isDeathCamCoroutineEnd)
        {
            yield return null;
        }

        Debug.Log("StartCoroutine(RespawnCamAction());");
        StartCoroutine(RespawnCamAction());


        while (!isRespawnCamCoroutineEnd)
        {
            yield return null;
        }

        Debug.Log("Respawn();");
        Respawn();
    }

    // 데스캠 연출 코루틴
    private IEnumerator DeathCamAction()
    {
        isDeathCamCoroutineEnd = false;

        // 데스캠 연출
        if (photonView.IsMine)
        {
            Debug.Log("데스캠 연출 시작");

            UIManager.instance.tpsUI.SetActive(false); // TPSUI인 크로스헤어와 실제피탄지점 크로스헤어 비활성화

            Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, cameraBlendTime); // 카메라 전환을 EaseOut으로

            // fps캠이 활성화 되어 있었다면 카메라와 UI 비활성화
            if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
            {
                UIManager.instance.fpsUI.SetActive(false);
                Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = false;
            }

            Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = false; // tps캠 비활성화
            Camera.main.GetComponent<CameraMovement>().deathCinemachineVirtualCamera.enabled = true; // 데스캠 활성화
        }

        yield return new WaitForSeconds(3f); // 3초간 데스캠 보여주기

        isDeathCamCoroutineEnd = true;
    }

    // 리스폰캠 연출 코루틴
    private IEnumerator RespawnCamAction()
    {
        isRespawnCamCoroutineEnd = false;

        if (pv.IsMine)
        {
            Debug.Log("리스폰캠 연출 시작");

            cameraMovement.getInput = false; // 리스폰중인 사람만 카메라 회전 입력 정지 

            // 리스폰캠으로 전환
            Camera.main.GetComponent<CameraMovement>().enabled = true;
            Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, cameraBlendTime); // 카메라 전환을 EaseOut으로
            Camera.main.GetComponent<CameraMovement>().deathCinemachineVirtualCamera.enabled = false; // 데스캠 비활성화
            Camera.main.GetComponent<CameraMovement>().respawnCinemachineVirtualCamera.enabled = true; // 리스폰캠 활성화

            float time = 0f;
            float rotateTime = 2f;

            // 리스폰캠이 맵을 바라보도록 설정
            while (time < rotateTime)
            {
                Camera.main.transform.rotation
                    = Quaternion.Lerp(Camera.main.transform.rotation, Camera.main.GetComponent<CameraMovement>().respawnCameraTransform.rotation, Time.deltaTime * 3f);
                yield return null;
                time += Time.deltaTime;
            }

            // 리스폰 카운트다운 시작
            respawnCountdown = 6f; // 리스폰 시간 초기화
            UIManager.instance.respawnUI.GetComponent<Text>().enabled = true; // 리스폰 UI 활성화, 리스폰중인 사람만 보임

            // 리스폰 남은시간 갱신
            while (respawnCountdown >= 0)
            {
                UIManager.instance.respawnUI.GetComponent<Text>().text = "RESPAWN IN\n" + Mathf.Floor(respawnCountdown);
                yield return null;
                respawnCountdown -= Time.deltaTime;
            }

            while (!Input.GetKeyDown(KeyCode.Space))
            {
                UIManager.instance.respawnUI.GetComponent<Text>().text = "SPACE TO RESPAWN";
                isReadyToSpawn = true; // 동기화되는 변수임
                yield return null;
            }

            Debug.Log("while 빠져나옴");
            UIManager.instance.respawnUI.GetComponent<Text>().enabled = false;            
        }

        while (!isReadyToSpawn) // 업데이트 메서드에서 동기화 갱신되면 빠져나감
        {
            yield return null;
        }

        // 리스폰 캠에서 TPS캠으로 전환
        if (pv.IsMine)
        {
            Camera.main.GetComponent<CameraMovement>().respawnCinemachineVirtualCamera.enabled = false; // 리스폰캠 비활성화
            Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = true; // tps캠 활성화, EaseOut으로 tps캠으로 이동

            Quaternion rot2 = Quaternion.Euler(Camera.main.GetComponent<CameraMovement>().rotX, Camera.main.GetComponent<CameraMovement>().rotY, 0); // 죽었을 때 카메라 회전값 가져오기

            float time = 0f;
            float rotateTime = 2f;

            while (time < rotateTime)
            {
                //Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, rot2, time / rotateTime);
                Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, rot2, Time.deltaTime * 3f);
                yield return null;
                time += Time.deltaTime;
            }

            Camera.main.transform.rotation = rot2; // 죽었을 때 마지막 시점과 동일하게 카메라 회전

            // UI 재설정
            UIManager.instance.tpsUI.SetActive(true); // TPSUI인 크로스헤어와 실제피탄지점 크로스헤어 활성화

            cameraMovement.getInput = true; // 카메라 조작 가능

            Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f); // 카메라 전환을 Cut으로
        }

        isRespawnCamCoroutineEnd = true;
    }

    private void Respawn()
    {
        gameObject.SetActive(false);

        // 탱크를 리스폰 지점으로 이동
        if (pv.IsMine)
        {
            selectedSpawnPointIndex = Random.Range(0, GameManager.instance.spawnPoint.Length); // 리스폰 지점 랜덤 선택
            this.transform.position = GameManager.instance.spawnPoint[selectedSpawnPointIndex].position; // 탱크를 리스폰 지점으로 이동
        }

        Quaternion rot = Quaternion.Euler(0, Camera.main.GetComponent<CameraMovement>().rotY, 0); // 카메라의 Y 회전값 가져오기
        this.gameObject.transform.rotation = rot; // 차체를 카메라 Y 회전값으로 회전
        turretTransform.localRotation = Quaternion.Euler(0, 0, 0); // 터렛이 차체와 나란히 되도록 회전 설정
        Debug.Log("탱크를 스폰지점으로 이동");

        // 조작 재설정
        GetComponentInChildren<TurretControl>().enabled = true; // Deserted에서 막았던 조작 재설정
        GetComponentInChildren<CannonControl>().enabled = true;
        fireCannon.enabled = true;

        // 탱크 스탯 재설정, 다른 클라이언트에 존재하는 나의 탱크에도 적용됨
        destoyedTank.SetActive(false); // 파괴된 탱크 모델링 비활성화
        Debug.Log("파괴된 탱크 모델링 비활성화");

        SetTankVisible(true); // 탱크 렌더링 활성화
        Debug.Log("탱크 렌더링 활성화");

        hudCanvas.enabled = true; // HUD 활성화
        Debug.Log("HUD 활성화");

        tankMove.isDie = false;
        Debug.Log("isDie = false");

        hpBar.fillAmount = 1.0f; // hpBar를 다시 채움
        Debug.Log("hpBar를 다시 채움");

        hpBar.color = Color.green; // 다시 녹색으로
        Debug.Log("다시 녹색으로");

        dead = false;

        health = startingHealth; // 체력을 다시 100으로
        Debug.Log("체력을 다시 100으로");

        tankMove.isDeserted = false;

        photonView.RPC("ApplyUpdatedHealth", RpcTarget.Others, health, dead);

        gameObject.SetActive(true);

    }

    private void SetTankVisible(bool isVisible)
    {
        foreach (MeshRenderer _renderer in renderers)
        {
            _renderer.enabled = isVisible;
        }
    }

    void Update()
    {
        // 체력바 선형보간
        hpBar.fillAmount = Mathf.Lerp(hpBar.fillAmount, health / startingHealth, Time.deltaTime * 10f);

        // 내 원격 탱크의 리스폰 타이밍 동기화
        if (!pv.IsMine)
        {
            isReadyToSpawn = IsReadyToSpawnCurrent;
        }

        if (health > 0 && !tankMove.isDeserted)
        {
            hpBar.color = Color.green; // 다시 녹색으로
            destoyedTank.SetActive(false); // 파괴된 탱크 모델링 비활성화
            SetTankVisible(true); // 탱크 렌더링 활성화
            hudCanvas.enabled = true; // HUD 활성화

            GetComponentInChildren<TurretControl>().enabled = true; // Deserted에서 막았던 조작 또한 재설정
            GetComponentInChildren<CannonControl>().enabled = true;
            fireCannon.enabled = true;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // 리스폰 준비 값을 송신
        {
            stream.SendNext(isReadyToSpawn);
        }
        else // 원격 탱크의 리스폰 준비 여부 수신
        {
            IsReadyToSpawnCurrent = (bool)stream.ReceiveNext();
        }
    }
}
