using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Cinemachine;
using System; // 무한루프 방지용
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FireCannon : MonoBehaviourPun
{
    // 마우스 좌클릭 시 리소스 폴더의 Canon 프리팹을 불러오고, 발사 하는 스크립트
    public GameObject cannonShell = null; // 포탄 프리팹
    public GameObject fireSFX = null; // 포탄 발사 효과 파티클
    public Transform firePos; // 포탄이 발사될 좌표

    private AudioSource sfx = null;
    private AudioClip fireSFXClip = null; // 포격음
    private AudioClip fireInsideSFXClip = null; // 탱크 내부 주포 후퇴음
    private AudioClip shellDropSFXClip = null; // 탄피소리
    private AudioClip reloadSFXClip = null; // 재장전

    private PhotonView pv = null; // RPC 함수를 쓰기 위한 포톤뷰 컴포넌트 

    private float shakeIntensityTPS = 8f; // 포탄 발사시 카메라를 흔드는 정도의 세기
    private float shakeIntensityFPS = 1f; // 포탄 발사시 카메라를 흔드는 정도의 세기

    // 포탄 발사시 주포 후퇴 구현에 필요한 변수
    private bool isReadyToFire = true;
    private float accumTime = 0f;
    private float blowBackTime = 0.05f;
    private float recoilTime = 2f;

    [SerializeField]
    private Transform barrelMidTransform; // 중간 배럴
    [SerializeField]
    private Transform barrelMidOriginTransform; // 배럴의 원래 위치를 저장할 변수
    [SerializeField]
    private Transform barrelMidTargetTransform; // 배럴이 후퇴하고 정지하는 위치를 저장할 변수

    [SerializeField]
    private Transform barrelEndTransform; // 가장 끝 배럴
    [SerializeField]
    private Transform barrelEndOriginTransform;
    [SerializeField]
    private Transform barrelEndTargetTransform;

    // 레티클 크기 동적 제어에 메서드에 필요한 변수
    private RectTransform reticleImageRectTransform;
    private Vector2 reticleOriginalSize;
    private Vector2 reticleDecreasedSize;
    private Vector2 reticleIncreasedSize;
    private float decreaseSizeTime = 0.1f;
    private float increaseSizeTime = 0.2f;
    private float originSizeTime = 1f;

    // 시네머신 FOV 동적 제어에 필요한 변수
    private CinemachineVirtualCamera fpsCinemachineVirtualCamera; // fps 카메라의 시네머신버추얼카메라 컴포넌트를 담을 변수

    // Vignette Intensity 필드 동적 제어에 필요한 변수
    private Vignette vignette;
    private float vignetteOriginIntensity;

    void Awake()
    {
        // Resources 폴더에서 포탄 프리팹을 불러옴, Load 함수의 파라미터는 path지만 Resources 폴더에 있다면 프리팹 이름으로도 가능
        cannonShell = (GameObject)Resources.Load("Projectile");// == cannon = Resources.Load<GameObject>("Cannon");
        fireSFX = (GameObject)Resources.Load("Cannon_Shot_FX"); // 

        sfx = GetComponent<AudioSource>();
        fireSFXClip = Resources.Load<AudioClip>("Fire2");
        pv = GetComponent<PhotonView>();

        shellDropSFXClip = Resources.Load<AudioClip>("MetalPipeFalling"); 
        reloadSFXClip = Resources.Load<AudioClip>("Reload");
        fireInsideSFXClip = Resources.Load<AudioClip>("CannonFireInside");

        reticleImageRectTransform = UIManager.instance.reticleRectTransform;
        reticleOriginalSize = reticleImageRectTransform.sizeDelta; // 레티클의 원래 사이즈 저장
        reticleDecreasedSize = reticleImageRectTransform.sizeDelta - new Vector2(80, 80); // 주퇴 즉시 레티클 크기
        reticleIncreasedSize = reticleImageRectTransform.sizeDelta + new Vector2(50, 50); // 주퇴 후 레티클 크기

        fpsCinemachineVirtualCamera = Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera; // FOV 동적 제어

        UIManager.instance.fpsVolume.profile.TryGet<Vignette>(out vignette);
        vignetteOriginIntensity = vignette.intensity.value;
    }

    void Update()
    {
        // 마우스 왼쪽 버튼 입력
        if (pv.IsMine && Input.GetMouseButtonDown(0))
        {
            if (GameManager.instance.isActiveLeaveGameUI)
            {
                return;
            }

            // 격발 준비 완료 상태일때만 발사 실행
            if (isReadyToFire == true)
            {
                Fire();
                ShakeCamera(); // 카메라 흔들기
                StartRecoilBuffer(); // 주퇴복좌
                StartControlReticleSize(); // 레티클 크기 동적 제어
                StartControlCameraFOV(); // 카메라 FOV 동적 제어
                pv.RPC("Fire", RpcTarget.Others, null);
            }
        }
    }

    [PunRPC]
    void Fire()
    {
        isReadyToFire = false;
        Instantiate(cannonShell, firePos.position, firePos.rotation);
        Instantiate(fireSFX, firePos.position, firePos.rotation);
        sfx.PlayOneShot(fireSFXClip, 1.0f);
        sfx.PlayOneShot(fireInsideSFXClip, 1.0f);
        sfx.PlayOneShot(shellDropSFXClip, 1.0f);
    }

    // 주퇴-복좌 코루틴 실행 메서드
    private void StartRecoilBuffer()
    {
        StartCoroutine(RecoilBuffer());
    }

    private IEnumerator RecoilBuffer() // Wave 알림 UI 응용하여 주퇴복좌 구현
    {
        // 주퇴
        accumTime = 0f;

        while (accumTime < blowBackTime)
        {
            barrelMidTransform.position = Vector3.Lerp(barrelMidTransform.position, barrelMidTargetTransform.position, accumTime / blowBackTime); // 중간 배럴 후퇴
            barrelEndTransform.position = Vector3.Lerp(barrelEndTransform.position, barrelEndTargetTransform.position, accumTime / blowBackTime); // 끝 배럴 후퇴
            yield return 0;
            accumTime += Time.deltaTime;
        }
        barrelMidTransform.position = barrelMidTargetTransform.position;
        barrelEndTransform.position = barrelEndTargetTransform.position;
        //Debug.Log("Barrel Back");

        yield return new WaitForSeconds(0.02f);

        // 복좌
        accumTime = 0f;

        while (accumTime < recoilTime + 2f)
        {
            barrelMidTransform.position = Vector3.Lerp(barrelMidTransform.position, barrelMidOriginTransform.position, Time.deltaTime * recoilTime); // Awake메서드에서 저장한 중간 배럴의 원래 위치로 복귀
            barrelEndTransform.position = Vector3.Lerp(barrelEndTransform.position, barrelEndOriginTransform.position, Time.deltaTime * recoilTime); // Awake메서드에서 저장한 끝 배럴의 원래 위치로 복귀
            yield return 0;
            accumTime += Time.deltaTime;
        }

        barrelMidTransform.position = barrelMidOriginTransform.position;
        barrelEndTransform.position = barrelEndOriginTransform.position;
        //Debug.Log("Recoil Done");

        //yield return new WaitForSeconds(1.0f);

        sfx.PlayOneShot(reloadSFXClip);
        yield return new WaitForSeconds(1.2f);

        isReadyToFire = true; // 주포가 원래 위치로 돌아오고, 재장전 효과음이 끝나면 격발 준비 완료로 변경
    }

    // 카메라 흔들기 메서드
    private void ShakeCamera()
    {
        if (Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled)
        {
            CinemachineShakeTPS.Instance.ShakeCamera(shakeIntensityTPS, 0.1f); // TPS 카메라를 shakeIntensityTPS 만큼의 강도로 흔들기
        }
        else if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
        {
            CinemachineShakeFPS.Instance.ShakeCamera(shakeIntensityFPS, 0.1f); //FPS 카메라를 shakeIntensityFPS 만큼의 강도로 흔들기
        }
    }

    private void StartControlReticleSize()
    {
        StartCoroutine(ControlReticleSize());
    }

    private IEnumerator ControlReticleSize()
    {
        if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
        {
            // 주퇴 즉시 레티클 축소
            accumTime = 0f;
            while (accumTime < decreaseSizeTime)
            {
                reticleImageRectTransform.sizeDelta = Vector2.Lerp(reticleImageRectTransform.sizeDelta, reticleDecreasedSize, accumTime / decreaseSizeTime);
                //Debug.Log("reticleDecreasedSize");
                yield return 0;
                accumTime += Time.deltaTime;
            }
            reticleImageRectTransform.sizeDelta = reticleDecreasedSize;

            // 레티클 축소 후 즉시 확대
            accumTime = 0f;
            while (accumTime < increaseSizeTime)
            {
                reticleImageRectTransform.sizeDelta = Vector2.Lerp(reticleImageRectTransform.sizeDelta, reticleIncreasedSize, accumTime / increaseSizeTime);
                //Debug.Log("reticleIncreasedSize");
                yield return 0;
                accumTime += Time.deltaTime;
            }
            reticleImageRectTransform.sizeDelta = reticleIncreasedSize;

            // 레티클 확대가 끝나면 천천히 원래 크기로
            accumTime = 0f;
            while (accumTime < originSizeTime)
            {
                reticleImageRectTransform.sizeDelta = Vector2.Lerp(reticleImageRectTransform.sizeDelta, reticleOriginalSize, accumTime / originSizeTime);
                // Debug.Log("reticleOriginalSize");
                yield return 0;
                accumTime += Time.deltaTime;
            }
            reticleImageRectTransform.sizeDelta = reticleOriginalSize;

            yield return null;
        }
    }

    private void StartControlCameraFOV()
    {
        StartCoroutine(ControlCameraFOV());
    }

    private IEnumerator ControlCameraFOV()
    {
        if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
        {
            float originFOV = fpsCinemachineVirtualCamera.m_Lens.FieldOfView;
            float increaseFOV = originFOV * 35f / 25f;
            float decreaseFOV = originFOV * 20f / 25f;

            // 25원래 35멀어졌다 20가까워졌다 25원래대로
            // 주퇴 즉시 FOV 값 올리기
            accumTime = 0f;
            while (accumTime < decreaseSizeTime)
            {
                fpsCinemachineVirtualCamera.m_Lens.FieldOfView 
                    = Mathf.Lerp(fpsCinemachineVirtualCamera.m_Lens.FieldOfView, increaseFOV, accumTime / decreaseSizeTime); //25+15
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, (vignetteOriginIntensity + 0.15f), accumTime / decreaseSizeTime); // Vignette

                yield return 0;
                accumTime += Time.deltaTime;
            }
            fpsCinemachineVirtualCamera.m_Lens.FieldOfView = increaseFOV;
            vignette.intensity.value = (vignetteOriginIntensity + 0.15f);

            // 즉시 FOV 줄이기
            accumTime = 0f;
            while (accumTime < increaseSizeTime)
            {
                fpsCinemachineVirtualCamera.m_Lens.FieldOfView 
                    = Mathf.Lerp(fpsCinemachineVirtualCamera.m_Lens.FieldOfView, decreaseFOV, accumTime / increaseSizeTime); // 40-10
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, (vignetteOriginIntensity - 0.05f), accumTime / increaseSizeTime); // Vignette

                yield return 0;
                accumTime += Time.deltaTime;
            }
            fpsCinemachineVirtualCamera.m_Lens.FieldOfView = decreaseFOV;
            vignette.intensity.value = (vignetteOriginIntensity - 0.05f);

            // 천천히 원래 크기로
            accumTime = 0f;
            while (accumTime < originSizeTime + 2.5f)
            {
                fpsCinemachineVirtualCamera.m_Lens.FieldOfView 
                    = Mathf.Lerp(fpsCinemachineVirtualCamera.m_Lens.FieldOfView, originFOV, Time.deltaTime * 2.5f); // 20+5
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, vignetteOriginIntensity, Time.deltaTime * 2.5f); // Vignette

                yield return 0;
                accumTime += Time.deltaTime;
            }

            yield return null;
        }
    }    
}