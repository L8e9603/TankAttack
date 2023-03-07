using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Cinemachine;
using System; // ���ѷ��� ������
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FireCannon : MonoBehaviourPun
{
    // ���콺 ��Ŭ�� �� ���ҽ� ������ Canon �������� �ҷ�����, �߻� �ϴ� ��ũ��Ʈ
    public GameObject cannonShell = null; // ��ź ������
    public GameObject fireSFX = null; // ��ź �߻� ȿ�� ��ƼŬ
    public Transform firePos; // ��ź�� �߻�� ��ǥ

    private AudioSource sfx = null;
    private AudioClip fireSFXClip = null; // ������
    private AudioClip fireInsideSFXClip = null; // ��ũ ���� ���� ������
    private AudioClip shellDropSFXClip = null; // ź�ǼҸ�
    private AudioClip reloadSFXClip = null; // ������

    private PhotonView pv = null; // RPC �Լ��� ���� ���� ����� ������Ʈ 

    private float shakeIntensityTPS = 8f; // ��ź �߻�� ī�޶� ���� ������ ����
    private float shakeIntensityFPS = 1f; // ��ź �߻�� ī�޶� ���� ������ ����

    // ��ź �߻�� ���� ���� ������ �ʿ��� ����
    private bool isReadyToFire = true;
    private float accumTime = 0f;
    private float blowBackTime = 0.05f;
    private float recoilTime = 2f;

    [SerializeField]
    private Transform barrelMidTransform; // �߰� �跲
    [SerializeField]
    private Transform barrelMidOriginTransform; // �跲�� ���� ��ġ�� ������ ����
    [SerializeField]
    private Transform barrelMidTargetTransform; // �跲�� �����ϰ� �����ϴ� ��ġ�� ������ ����

    [SerializeField]
    private Transform barrelEndTransform; // ���� �� �跲
    [SerializeField]
    private Transform barrelEndOriginTransform;
    [SerializeField]
    private Transform barrelEndTargetTransform;

    // ��ƼŬ ũ�� ���� ��� �޼��忡 �ʿ��� ����
    private RectTransform reticleImageRectTransform;
    private Vector2 reticleOriginalSize;
    private Vector2 reticleDecreasedSize;
    private Vector2 reticleIncreasedSize;
    private float decreaseSizeTime = 0.1f;
    private float increaseSizeTime = 0.2f;
    private float originSizeTime = 1f;

    // �ó׸ӽ� FOV ���� ��� �ʿ��� ����
    private CinemachineVirtualCamera fpsCinemachineVirtualCamera; // fps ī�޶��� �ó׸ӽŹ��߾�ī�޶� ������Ʈ�� ���� ����

    // Vignette Intensity �ʵ� ���� ��� �ʿ��� ����
    private Vignette vignette;
    private float vignetteOriginIntensity;

    void Awake()
    {
        // Resources �������� ��ź �������� �ҷ���, Load �Լ��� �Ķ���ʹ� path���� Resources ������ �ִٸ� ������ �̸����ε� ����
        cannonShell = (GameObject)Resources.Load("Projectile");// == cannon = Resources.Load<GameObject>("Cannon");
        fireSFX = (GameObject)Resources.Load("Cannon_Shot_FX"); // 

        sfx = GetComponent<AudioSource>();
        fireSFXClip = Resources.Load<AudioClip>("Fire2");
        pv = GetComponent<PhotonView>();

        shellDropSFXClip = Resources.Load<AudioClip>("MetalPipeFalling"); 
        reloadSFXClip = Resources.Load<AudioClip>("Reload");
        fireInsideSFXClip = Resources.Load<AudioClip>("CannonFireInside");

        reticleImageRectTransform = UIManager.instance.reticleRectTransform;
        reticleOriginalSize = reticleImageRectTransform.sizeDelta; // ��ƼŬ�� ���� ������ ����
        reticleDecreasedSize = reticleImageRectTransform.sizeDelta - new Vector2(80, 80); // ���� ��� ��ƼŬ ũ��
        reticleIncreasedSize = reticleImageRectTransform.sizeDelta + new Vector2(50, 50); // ���� �� ��ƼŬ ũ��

        fpsCinemachineVirtualCamera = Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera; // FOV ���� ����

        UIManager.instance.fpsVolume.profile.TryGet<Vignette>(out vignette);
        vignetteOriginIntensity = vignette.intensity.value;
    }

    void Update()
    {
        // ���콺 ���� ��ư �Է�
        if (pv.IsMine && Input.GetMouseButtonDown(0))
        {
            if (GameManager.instance.isActiveLeaveGameUI)
            {
                return;
            }

            // �ݹ� �غ� �Ϸ� �����϶��� �߻� ����
            if (isReadyToFire == true)
            {
                Fire();
                ShakeCamera(); // ī�޶� ����
                StartRecoilBuffer(); // ������
                StartControlReticleSize(); // ��ƼŬ ũ�� ���� ����
                StartControlCameraFOV(); // ī�޶� FOV ���� ����
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

    // ����-���� �ڷ�ƾ ���� �޼���
    private void StartRecoilBuffer()
    {
        StartCoroutine(RecoilBuffer());
    }

    private IEnumerator RecoilBuffer() // Wave �˸� UI �����Ͽ� ������ ����
    {
        // ����
        accumTime = 0f;

        while (accumTime < blowBackTime)
        {
            barrelMidTransform.position = Vector3.Lerp(barrelMidTransform.position, barrelMidTargetTransform.position, accumTime / blowBackTime); // �߰� �跲 ����
            barrelEndTransform.position = Vector3.Lerp(barrelEndTransform.position, barrelEndTargetTransform.position, accumTime / blowBackTime); // �� �跲 ����
            yield return 0;
            accumTime += Time.deltaTime;
        }
        barrelMidTransform.position = barrelMidTargetTransform.position;
        barrelEndTransform.position = barrelEndTargetTransform.position;
        //Debug.Log("Barrel Back");

        yield return new WaitForSeconds(0.02f);

        // ����
        accumTime = 0f;

        while (accumTime < recoilTime + 2f)
        {
            barrelMidTransform.position = Vector3.Lerp(barrelMidTransform.position, barrelMidOriginTransform.position, Time.deltaTime * recoilTime); // Awake�޼��忡�� ������ �߰� �跲�� ���� ��ġ�� ����
            barrelEndTransform.position = Vector3.Lerp(barrelEndTransform.position, barrelEndOriginTransform.position, Time.deltaTime * recoilTime); // Awake�޼��忡�� ������ �� �跲�� ���� ��ġ�� ����
            yield return 0;
            accumTime += Time.deltaTime;
        }

        barrelMidTransform.position = barrelMidOriginTransform.position;
        barrelEndTransform.position = barrelEndOriginTransform.position;
        //Debug.Log("Recoil Done");

        //yield return new WaitForSeconds(1.0f);

        sfx.PlayOneShot(reloadSFXClip);
        yield return new WaitForSeconds(1.2f);

        isReadyToFire = true; // ������ ���� ��ġ�� ���ƿ���, ������ ȿ������ ������ �ݹ� �غ� �Ϸ�� ����
    }

    // ī�޶� ���� �޼���
    private void ShakeCamera()
    {
        if (Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled)
        {
            CinemachineShakeTPS.Instance.ShakeCamera(shakeIntensityTPS, 0.1f); // TPS ī�޶� shakeIntensityTPS ��ŭ�� ������ ����
        }
        else if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
        {
            CinemachineShakeFPS.Instance.ShakeCamera(shakeIntensityFPS, 0.1f); //FPS ī�޶� shakeIntensityFPS ��ŭ�� ������ ����
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
            // ���� ��� ��ƼŬ ���
            accumTime = 0f;
            while (accumTime < decreaseSizeTime)
            {
                reticleImageRectTransform.sizeDelta = Vector2.Lerp(reticleImageRectTransform.sizeDelta, reticleDecreasedSize, accumTime / decreaseSizeTime);
                //Debug.Log("reticleDecreasedSize");
                yield return 0;
                accumTime += Time.deltaTime;
            }
            reticleImageRectTransform.sizeDelta = reticleDecreasedSize;

            // ��ƼŬ ��� �� ��� Ȯ��
            accumTime = 0f;
            while (accumTime < increaseSizeTime)
            {
                reticleImageRectTransform.sizeDelta = Vector2.Lerp(reticleImageRectTransform.sizeDelta, reticleIncreasedSize, accumTime / increaseSizeTime);
                //Debug.Log("reticleIncreasedSize");
                yield return 0;
                accumTime += Time.deltaTime;
            }
            reticleImageRectTransform.sizeDelta = reticleIncreasedSize;

            // ��ƼŬ Ȯ�밡 ������ õõ�� ���� ũ���
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

            // 25���� 35�־����� 20��������� 25�������
            // ���� ��� FOV �� �ø���
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

            // ��� FOV ���̱�
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

            // õõ�� ���� ũ���
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