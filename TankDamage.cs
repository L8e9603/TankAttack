using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon;
using Cinemachine;

public class TankDamage : LivingEntity, IPunObservable
{
    private MeshRenderer[] renderers; // ��ũ�� ����ó���� ���� ������ ������Ʈ
    private GameObject destroyEffect = null; // ��ũ�� ü���� 0�� �Ǿ� ������ �� ����Ʈ
    /*    private int initHp = 100; // ���� ü��
        private int currHp = 0; // ���� ü��
    */

    private PhotonView pv;
    public Canvas hudCanvas;
    public Image hpBar;

    [SerializeField]
    private GameObject destoyedTank;

    private TankMove tankMove; // ü���� 0�� �� ��ũ ���� ��� ���� ���� ����
    private FireCannon fireCannon; // ü���� 0�� �� ĳ�� �߻縦 ���� ���� ����

    private AudioSource audioSource;
    [SerializeField]
    private AudioClip[] hitClip; // ��Ʈ ���� �پ�ȭ, ��Ʈ���� ������Ʈ�� AudioSource ������Ʈ�� �����Ŵ
    private AudioClip[] ricochetClip; // ��Ʈ ���������� �� ��źȿ����, ��Ʈ��Ų ������Ʈ�� AudioSource ������Ʈ�� �����Ŵ
    private int selectedHitClipIndex; // ���� �߻� �� �ش��ϴ� �ε��� ��ȣ�� hitClip ���

    // ������ ���� ����
    private int selectedSpawnPointIndex;


    private float cameraBlendTime = 2.5f;

    [SerializeField]
    private Transform turretTransform;

    private CameraMovement cameraMovement;

    private float respawnCountdown = 6f;

    public bool isReadyToSpawn = false; // ���� ������ �غ� ����
    public bool IsReadyToSpawnCurrent = false; // ���� ��ũ ������ �غ� ����

    private bool isDeathCamCoroutineEnd = false;
    private bool isRespawnCamCoroutineEnd = false;

    public int kill = 0;
    public int death = 0;


    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        health = startingHealth; // ���� ü���� �ʱ�ȭ, LivingEntity�κ��� ��ӹ��� ����
        hpBar.color = Color.green; // Filled �̹��� ������ ������� ����
        destroyEffect = Resources.Load<GameObject>("Explosion_TankDestroy_FX"); // ��ũ ���� ��ƼŬ �ý��� ������ �ε�
        renderers = GetComponentsInChildren<MeshRenderer>(); // Tank ������Ʈ�� �ڽ��� BodyMesh�� �޽��������� �̿��Ͽ� ����ó��
        tankMove = GetComponent<TankMove>();
        fireCannon = GetComponent<FireCannon>();
        audioSource = GetComponent<AudioSource>();
        cameraMovement = Camera.main.GetComponent<CameraMovement>();
    }

    [PunRPC]
    public override void OnDamage(float damage)
    {
        //Debug.Log(PhotonNetwork.NickName + "hit, left health = " + health);

        // �ǰ� ȿ���� Ŭ�� ���� ���
        selectedHitClipIndex = Random.Range(0, hitClip.Length);
        audioSource.PlayOneShot(hitClip[selectedHitClipIndex], 1f);

        // LivingEntity�� OnDamage() ����, ����� ����
        base.OnDamage(damage);

        if (health / startingHealth <= 0.4f)
        {
            hpBar.color = Color.red; // 40% ���ϴ� ������
        }
        else if (health / startingHealth <= 0.6f)
        {
            hpBar.color = Color.yellow; // 60% ���ϴ� �����
        }
        
    }

    public override void Die()
    {
        base.Die();

        GetComponentInChildren<TurretControl>().enabled = false; // �ͷ� ���� ��Ȱ��ȭ
        GetComponentInChildren<CannonControl>().enabled = false; // ���� ���� ���� ��Ȱ��ȭ
        fireCannon.enabled = false; // ĳ�� �߻� ��Ȱ��ȭ
        tankMove.isDie = true; // ��ũ�� ���� �� �� ������ �ʰ� ������ ����� �� ó�� õõ�� ����
        audioSource.PlayOneShot(hitClip[selectedHitClipIndex]); // LivingEntity���� OnDamage �޼��� �ȿ� Die�Լ� �־ ������ ����?
        StartCoroutine(ExplosionTank()); // ��ũ ���� �ڷ�ƾ �Լ� ����

        StartCoroutine(RespawnCoroutine());
    }

    // ��ũ ü���� 0�� �Ǹ� ������ �����ϴ� �ڷ�ƾ
    private IEnumerator ExplosionTank() // ���� ȿ�� ���� �G ������ �ڷ�ƾ �Լ�
    {
        isReadyToSpawn = false;
        IsReadyToSpawnCurrent = false;

        hudCanvas.enabled = false; // HUD ��Ȱ��ȭ
        //Debug.Log("HUD��Ȱ��ȭ");

        SetTankVisible(false); // �ı����� ���� ��ũ �𵨸� ������ ����
        //Debug.Log("�ı����� ���� ��ũ �𵨸� ������ ����");

        destoyedTank.SetActive(true); // �ı��� ��ũ �𵨸� Ȱ��ȭ
        //Debug.Log("�ı��� ��ũ �𵨸� Ȱ��ȭ");

        GameObject effect = GameObject.Instantiate(destroyEffect, transform.position + new Vector3(0,0.5f,0), Quaternion.identity); // ��ũ ���� ����Ʈ ����
        Destroy(effect, 3.0f); // ����Ʈ 3�� �� �ı�

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

    // ����ķ ���� �ڷ�ƾ
    private IEnumerator DeathCamAction()
    {
        isDeathCamCoroutineEnd = false;

        // ����ķ ����
        if (photonView.IsMine)
        {
            Debug.Log("����ķ ���� ����");

            UIManager.instance.tpsUI.SetActive(false); // TPSUI�� ũ�ν����� ������ź���� ũ�ν���� ��Ȱ��ȭ

            Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, cameraBlendTime); // ī�޶� ��ȯ�� EaseOut����

            // fpsķ�� Ȱ��ȭ �Ǿ� �־��ٸ� ī�޶�� UI ��Ȱ��ȭ
            if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled)
            {
                UIManager.instance.fpsUI.SetActive(false);
                Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = false;
            }

            Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = false; // tpsķ ��Ȱ��ȭ
            Camera.main.GetComponent<CameraMovement>().deathCinemachineVirtualCamera.enabled = true; // ����ķ Ȱ��ȭ
        }

        yield return new WaitForSeconds(3f); // 3�ʰ� ����ķ �����ֱ�

        isDeathCamCoroutineEnd = true;
    }

    // ������ķ ���� �ڷ�ƾ
    private IEnumerator RespawnCamAction()
    {
        isRespawnCamCoroutineEnd = false;

        if (pv.IsMine)
        {
            Debug.Log("������ķ ���� ����");

            cameraMovement.getInput = false; // ���������� ����� ī�޶� ȸ�� �Է� ���� 

            // ������ķ���� ��ȯ
            Camera.main.GetComponent<CameraMovement>().enabled = true;
            Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, cameraBlendTime); // ī�޶� ��ȯ�� EaseOut����
            Camera.main.GetComponent<CameraMovement>().deathCinemachineVirtualCamera.enabled = false; // ����ķ ��Ȱ��ȭ
            Camera.main.GetComponent<CameraMovement>().respawnCinemachineVirtualCamera.enabled = true; // ������ķ Ȱ��ȭ

            float time = 0f;
            float rotateTime = 2f;

            // ������ķ�� ���� �ٶ󺸵��� ����
            while (time < rotateTime)
            {
                Camera.main.transform.rotation
                    = Quaternion.Lerp(Camera.main.transform.rotation, Camera.main.GetComponent<CameraMovement>().respawnCameraTransform.rotation, Time.deltaTime * 3f);
                yield return null;
                time += Time.deltaTime;
            }

            // ������ ī��Ʈ�ٿ� ����
            respawnCountdown = 6f; // ������ �ð� �ʱ�ȭ
            UIManager.instance.respawnUI.GetComponent<Text>().enabled = true; // ������ UI Ȱ��ȭ, ���������� ����� ����

            // ������ �����ð� ����
            while (respawnCountdown >= 0)
            {
                UIManager.instance.respawnUI.GetComponent<Text>().text = "RESPAWN IN\n" + Mathf.Floor(respawnCountdown);
                yield return null;
                respawnCountdown -= Time.deltaTime;
            }

            while (!Input.GetKeyDown(KeyCode.Space))
            {
                UIManager.instance.respawnUI.GetComponent<Text>().text = "SPACE TO RESPAWN";
                isReadyToSpawn = true; // ����ȭ�Ǵ� ������
                yield return null;
            }

            Debug.Log("while ��������");
            UIManager.instance.respawnUI.GetComponent<Text>().enabled = false;            
        }

        while (!isReadyToSpawn) // ������Ʈ �޼��忡�� ����ȭ ���ŵǸ� ��������
        {
            yield return null;
        }

        // ������ ķ���� TPSķ���� ��ȯ
        if (pv.IsMine)
        {
            Camera.main.GetComponent<CameraMovement>().respawnCinemachineVirtualCamera.enabled = false; // ������ķ ��Ȱ��ȭ
            Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = true; // tpsķ Ȱ��ȭ, EaseOut���� tpsķ���� �̵�

            Quaternion rot2 = Quaternion.Euler(Camera.main.GetComponent<CameraMovement>().rotX, Camera.main.GetComponent<CameraMovement>().rotY, 0); // �׾��� �� ī�޶� ȸ���� ��������

            float time = 0f;
            float rotateTime = 2f;

            while (time < rotateTime)
            {
                //Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, rot2, time / rotateTime);
                Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, rot2, Time.deltaTime * 3f);
                yield return null;
                time += Time.deltaTime;
            }

            Camera.main.transform.rotation = rot2; // �׾��� �� ������ ������ �����ϰ� ī�޶� ȸ��

            // UI �缳��
            UIManager.instance.tpsUI.SetActive(true); // TPSUI�� ũ�ν����� ������ź���� ũ�ν���� Ȱ��ȭ

            cameraMovement.getInput = true; // ī�޶� ���� ����

            Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f); // ī�޶� ��ȯ�� Cut����
        }

        isRespawnCamCoroutineEnd = true;
    }

    private void Respawn()
    {
        gameObject.SetActive(false);

        // ��ũ�� ������ �������� �̵�
        if (pv.IsMine)
        {
            selectedSpawnPointIndex = Random.Range(0, GameManager.instance.spawnPoint.Length); // ������ ���� ���� ����
            this.transform.position = GameManager.instance.spawnPoint[selectedSpawnPointIndex].position; // ��ũ�� ������ �������� �̵�
        }

        Quaternion rot = Quaternion.Euler(0, Camera.main.GetComponent<CameraMovement>().rotY, 0); // ī�޶��� Y ȸ���� ��������
        this.gameObject.transform.rotation = rot; // ��ü�� ī�޶� Y ȸ�������� ȸ��
        turretTransform.localRotation = Quaternion.Euler(0, 0, 0); // �ͷ��� ��ü�� ������ �ǵ��� ȸ�� ����
        Debug.Log("��ũ�� ������������ �̵�");

        // ���� �缳��
        GetComponentInChildren<TurretControl>().enabled = true; // Deserted���� ���Ҵ� ���� �缳��
        GetComponentInChildren<CannonControl>().enabled = true;
        fireCannon.enabled = true;

        // ��ũ ���� �缳��, �ٸ� Ŭ���̾�Ʈ�� �����ϴ� ���� ��ũ���� �����
        destoyedTank.SetActive(false); // �ı��� ��ũ �𵨸� ��Ȱ��ȭ
        Debug.Log("�ı��� ��ũ �𵨸� ��Ȱ��ȭ");

        SetTankVisible(true); // ��ũ ������ Ȱ��ȭ
        Debug.Log("��ũ ������ Ȱ��ȭ");

        hudCanvas.enabled = true; // HUD Ȱ��ȭ
        Debug.Log("HUD Ȱ��ȭ");

        tankMove.isDie = false;
        Debug.Log("isDie = false");

        hpBar.fillAmount = 1.0f; // hpBar�� �ٽ� ä��
        Debug.Log("hpBar�� �ٽ� ä��");

        hpBar.color = Color.green; // �ٽ� �������
        Debug.Log("�ٽ� �������");

        dead = false;

        health = startingHealth; // ü���� �ٽ� 100����
        Debug.Log("ü���� �ٽ� 100����");

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
        // ü�¹� ��������
        hpBar.fillAmount = Mathf.Lerp(hpBar.fillAmount, health / startingHealth, Time.deltaTime * 10f);

        // �� ���� ��ũ�� ������ Ÿ�̹� ����ȭ
        if (!pv.IsMine)
        {
            isReadyToSpawn = IsReadyToSpawnCurrent;
        }

        if (health > 0 && !tankMove.isDeserted)
        {
            hpBar.color = Color.green; // �ٽ� �������
            destoyedTank.SetActive(false); // �ı��� ��ũ �𵨸� ��Ȱ��ȭ
            SetTankVisible(true); // ��ũ ������ Ȱ��ȭ
            hudCanvas.enabled = true; // HUD Ȱ��ȭ

            GetComponentInChildren<TurretControl>().enabled = true; // Deserted���� ���Ҵ� ���� ���� �缳��
            GetComponentInChildren<CannonControl>().enabled = true;
            fireCannon.enabled = true;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // ������ �غ� ���� �۽�
        {
            stream.SendNext(isReadyToSpawn);
        }
        else // ���� ��ũ�� ������ �غ� ���� ����
        {
            IsReadyToSpawnCurrent = (bool)stream.ReceiveNext();
        }
    }
}
