using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
// using UnityStandardAssets.Utility; // ���� ����Ƽ �⺻ �ּ��̾���, Camera.main.GetComponent<SmoothFollow>().target = camPivot; ������ ������ ���ӽ����̽�
using Cinemachine;

public class TankMove : MonoBehaviourPun, IPunObservable
{
    [HideInInspector]
    public bool isDie = false; // true�� ������ õõ�� ����

    [SerializeField]
    private float defaultMoveSpeed = 3.5f;
    [SerializeField]
    private float accelerationMoveSpeed = 5f;
    [SerializeField]
    private float currentMoveSpeed;
    
    public float rotSpeed = 40.0f; // �ͷ� stabilizeSpeed �ӵ��� ��ü ȸ���ӵ��� ���� ��Ű�� ���� public ����
    
    private Rigidbody rbody;
    private Transform tr;
    public float h, v; // ����� Ű �Է��� �ޱ� ���� ����
    private PhotonView pv = null; // ���� �� ������Ʈ

    // ������ũ�� ������ �ۼ����� �� ����� ���� ���� �� �ʱ�ȭ
    private Vector3 bodyCurrentPosition = Vector3.zero; // ���� ��ũ ��ü�� ��ǥ
    private Quaternion bodyCurrentRotation = Quaternion.identity; // ���� ��ũ ��ü�� ȸ�� ��

    private Vector2 rTrackOffset = Vector2.zero; // ���� ��ũ�� ���� ���ѱ˵� Offset ��
    private Vector2 lTrackOffset = Vector2.zero;
    private Quaternion[] rWheelCurrentRotation = new Quaternion[15];
    private Quaternion[] lWheelCurrentRotation = new Quaternion[15];

    // ���ѱ˵� ���� ����
    [SerializeField]
    private float trackScrollSpeed = 0.5f; // ���ѱ˵� �ؽ�ó Offset ��ũ�� �ӵ�

    [SerializeField]
    private Renderer _rendererR;

    [SerializeField]
    private Renderer _rendererL;

    // �� ���� ����, localRotation ���� �����ϸ� ���� ����
    [SerializeField]
    private Transform[] wheelR;

    [SerializeField]
    private Transform[] wheelL;

    [SerializeField]
    private float wheelRotSpeed = 100f;
    private float wheelRotAddSpeed = 300f;

    private bool isCollision; // ��ũ�� �浹 ���¸� �����ϴ� ����, �浹�����̸� ����Ʈ Ű�� ���ӵ��� ����

    [SerializeField]
    private Transform turretTransform; // �ͷ� - �ٵ� ���Ŀ� �� ����
    private RaycastHit hit; // ������ ���鿡 ���� ��ġ�� ������ ����

    [SerializeField]
    private AudioSource engineAudioSource; // ������ ���ѷ��� ������ҽ�

    // �������� ���� ���� ����
    private float countdown = 11f;
    private bool isInCombatArea = true;
    public bool isDeserted = false;
    public bool isCoroutineStarted = false;

    // ���� ����
    [SerializeField]
    private ParticleSystem engineSmokeParticleSystem_L;
    [SerializeField]
    private ParticleSystem engineSmokeParticleSystem_R;
    private float originMaxParticles = 50f;
    private float originGravityModifire = -0.02f;

    void Awake() // start->Awake�� �ٲ�, ��ŸƮ�� ����Ǳ� ���� OnStartPhotonSer~~ ����Ǹ� ���� �߻���
    {
        currentMoveSpeed = defaultMoveSpeed;

        rbody = GetComponent<Rigidbody>();
        tr = GetComponent<Transform>();
        pv = GetComponent<PhotonView>();
        // ������ ���� Ÿ���� ����
        pv.Synchronization = ViewSynchronization.Unreliable; // ����� ������Ʈ�� ���� �ʵ�, (��Ӵٿ� �޴� ������ ������� Off(RPC�� ������ �ۼ��� �� ��), TCP, UDP, ��ȭ�� ���涧��)
        //ObservedComponents �Ӽ��� TankMove ��ũ��Ʈ�� ������
        pv.ObservedComponents[0] = this;

        if (pv.IsMine)// �������� �ƴ��� �˻� �� ���� ī�޶� ���ø� ����ٴϵ��� ��
        {
            GameObject.FindGameObjectWithTag("MinimapCamera").GetComponent<CopyPosition>().target = this.transform; // �̴ϸ� ī�޶� ������ ����ٴϵ��� ����

            //Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().Follow = tpsCameraPosition;

            /*            Camera.main.GetComponent<SmoothFollow>().target = camPivot;
                        // �����߽��� ���� ����
                        rbody.centerOfMass = new Vector3(0.0f, -0.5f, 0.0f);
            */
        }
        else
        {
            // ���� ��ũ�� �������� �̿����� ����
            //rbody.isKinematic = true;
        }
        // ���� ��ũ�� ��ġ �� ȸ�� ���� ó���� ������ �ʱⰪ ����
        bodyCurrentPosition = tr.position; // ���� ��ũ(���� �н�)�� ��ǥ�� �� ������ ���� �� ��ũ�� ��ǥ�� ��������
        bodyCurrentRotation = tr.rotation; // �Ӹ� �ƴ϶� ���߿� ���� �÷��̾�� �� ��ġ�� �𸣱� ������ �� ��ġ�� ������ �۽����ְ� ���߿� ���� �÷��̾�� �������� �� ��ġ���� ���Ź޾� ������ �� ��ġ�� �˾Ƴ�

        rTrackOffset = _rendererR.material.mainTextureOffset;
        lTrackOffset = _rendererL.material.mainTextureOffset;

        for (int i = 0; i < wheelR.Length; i++) // ���� ��ũ�� ������ ���� �� ��ŭ �ݺ� ����
        {
            rWheelCurrentRotation[i] = wheelR[i].localRotation; // ���� ��ũ�� ȸ������ �� ������ ���� ��ũ ������ ȸ���� ����
        }
        for (int i = 0; i < wheelL.Length; i++) // ���� ��ũ�� ���� ���� �� ��ŭ �ݺ� ����
        {
            lWheelCurrentRotation[i] = wheelL[i].localRotation; // ���� ��ũ�� ȸ������ �� ������ ���� ��ũ ������ ȸ���� ����
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(countdown);

        // �����̸�
        if (pv.IsMine)
        {
            if (!isDie)
            {
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
            else if (isDie)
            {
                // ������ ���� ������ ���ߴ� ȿ�� ����
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * 0.5f);
                h = Mathf.Lerp(h, 0f, Time.deltaTime * 1f);
                v = Mathf.Lerp(v, 0f, Time.deltaTime * 1f);

            }

            // ��ũ�� �ӵ��� ���� ������ ��ȭ
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, 1f + (currentMoveSpeed * 1f / accelerationMoveSpeed * (Mathf.Abs(v) + Mathf.Abs(h) * 1f / 2f)), Time.deltaTime * 5f); // pitch�� �ִ� 2���� �ø�, currentMoveSpeed * 1f / accelerationMoveSpeed�� �ִ밪�� 1, ���� ���� ���밪���� ��ȯ

            // ��ũ�� �ӵ��� ���� ���� ���� ��ȭ
            engineSmokeParticleSystem_L.maxParticles = (int)Mathf.Lerp(engineSmokeParticleSystem_L.maxParticles, originMaxParticles + 1000f * (currentMoveSpeed / accelerationMoveSpeed), Time.deltaTime * 10f); // �⺻ ��ƼŬ�� 40�� �����ϸ� 1000���� ����
            engineSmokeParticleSystem_R.maxParticles = (int)Mathf.Lerp(engineSmokeParticleSystem_R.maxParticles, originMaxParticles + 1000f * (currentMoveSpeed / accelerationMoveSpeed), Time.deltaTime * 10f); // �⺻ ��ƼŬ�� 40�� �����ϸ� 1000���� ����
            engineSmokeParticleSystem_L.gravityModifier = Mathf.Lerp(engineSmokeParticleSystem_L.gravityModifier, originGravityModifire - 0.1f * (currentMoveSpeed / accelerationMoveSpeed) , Time.deltaTime * 5f);
            engineSmokeParticleSystem_R.gravityModifier = Mathf.Lerp(engineSmokeParticleSystem_L.gravityModifier, originGravityModifire - 0.1f * (currentMoveSpeed / accelerationMoveSpeed) , Time.deltaTime * 5f);
            // Debug.Log("MaxParticles : "+ engineSmokeParticleSystem_L.maxParticles + " / " + engineSmokeParticleSystem_L.gravityModifier);

            // �� �Է��� �����Ǹ� ���� ����
            if (h != 0 || v != 0)
            {
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, defaultMoveSpeed, Time.deltaTime * defaultMoveSpeed / 2.0f);
            }
            else
            {
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * defaultMoveSpeed / 2.0f);
            }

            // ������ �̵��� ȸ��
            Acceleration(); // ����Ʈ Ű�� �����Ǹ� currentMoveSpeed = moveSpeed * float

            tr.Translate(Vector3.forward * v * currentMoveSpeed * Time.deltaTime); // ��������

/*            // ��ƽ�� ��ũ�� ȸ��
            tr.Rotate(Vector3.up * rotSpeed * h * Time.deltaTime); // ȸ��
*/
            // �ڵ��� ��ũ�� ȸ��
            if (v >= 0)
            {
                tr.Rotate(Vector3.up * rotSpeed * h * Time.deltaTime); // ������ ȸ�� �Ǵ� ���ڸ� ȸ��
            }
            else if (v < 0)
            {
                tr.Rotate(Vector3.up * rotSpeed * -h * Time.deltaTime); // �������̸� ȸ�� �ݴ���Ͽ� �ڵ��� �ڵ�ó�� ����
            }

            // ���ѱ˵� �ִϸ��̼�
            TracksAndWheelsAnimation();
            
            // ���������� ����� ī��Ʈ�ٿ� ����
            ReturnToCombatAreaCountdown();
        }

        else // ���� ��ũ(���� �н�)�� �̵��� ȸ��
        {
            tr.position = Vector3.Lerp(tr.position, bodyCurrentPosition, Time.deltaTime * 3.0f);
            tr.rotation = Quaternion.Slerp(tr.rotation, bodyCurrentRotation, Time.deltaTime * 3.0f);

            // ���� ��ũ�� ���ѱ˵� ȸ��
            _rendererR.material.mainTextureOffset = Vector2.Lerp(_rendererR.material.mainTextureOffset, rTrackOffset, Time.deltaTime * 3.0f);
            _rendererL.material.mainTextureOffset = Vector2.Lerp(_rendererL.material.mainTextureOffset, lTrackOffset, Time.deltaTime * 3.0f);

            // ���� ��ũ�� ������ �� ȸ��
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
        if (stream.IsWriting) // ���� �÷��̾��� ��ġ ���� �۽�
        {
            stream.SendNext(tr.position);
            stream.SendNext(tr.rotation);

            stream.SendNext(_rendererR.material.mainTextureOffset);
            stream.SendNext(_rendererL.material.mainTextureOffset);

            // ������ ȸ���� ����
            for (int i = 0; i < wheelR.Length; i++)
            {
                stream.SendNext(wheelR[i].localRotation);
            }
            for (int i = 0; i < wheelL.Length; i++)
            {
                stream.SendNext(wheelL[i].localRotation);
            }
        }
        else // ���� �÷��̾��� ��ġ ���� ����
        {
            bodyCurrentPosition = (Vector3)stream.ReceiveNext();
            bodyCurrentRotation = (Quaternion)stream.ReceiveNext();

            // ��, �� Ʈ���� Vector2 ��Ƽ���� Offset ���� ����
            rTrackOffset = (Vector2)stream.ReceiveNext();
            lTrackOffset = (Vector2)stream.ReceiveNext();

            // ��,�� ������ ȸ�� ���ʹϾ� ���� ����
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

    // Ű �Էº� ���ѱ˵�, �� ȸ�� �ִϸ��̼� ó��
    private void TracksAndWheelsAnimation()
    {
        //Debug.Log("Vertical : " + Input.GetAxis("Vertical"));
        //Debug.Log("Horizontal : " + Input.GetAxis("Horizontal"));

        // ���� �ݴ� ������ ����Ű�� ���ÿ� �Է��Ͽ���, ������ ���� ����� 0�� ��� �ִϸ��̼� ���� ���� ����
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S) && Input.GetAxis("Vertical") == 0)
        {
            return; 
        }
        // ���� �ݴ� ������ ����Ű�� ���ÿ� �Է��Ͽ���, ������ ���� ����� 0�� ��� �ִϸ��̼� ���� ���� ����
        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D) && Input.GetAxis("Horizontal") == 0)
        {
            return;
        }


        // ���ڸ� ��, ��ȸ�� �� ���ѱ˵� ȸ��
        //if (Input.GetKey(KeyCode.A)) // ��ȸ��
        if (Input.GetAxis("Horizontal") < 0) // ��ȸ��
        {
            MoveForwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
            MoveBackwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
            MoveForwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
            MoveBackwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
        }
        //else if (Input.GetKey(KeyCode.D)) // ��ȸ��
        else if (Input.GetAxis("Horizontal") > 0) // ��ȸ��
        {
            MoveBackwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
            MoveForwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
            MoveBackwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
            MoveForwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
        }

        // ����, ������ ���ѱ˵��� �� �ִϸ��̼�
        // ������ ���ѱ˵��� �� �ִϸ��̼�
        //if (Input.GetKey(KeyCode.W))
        if (Input.GetAxis("Vertical") > 0)
        {
            //if (Input.GetKey(KeyCode.A)) // �����ϸ鼭 ��ȸ��
            if (Input.GetAxis("Horizontal") < 0) // �����ϸ鼭 ��ȸ��
            {
                // ���� �˵� ���� ȸ��

                // ���� �˵� ���� ȸ��
                MoveForwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
                MoveForwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
                MoveForwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
                MoveForwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
            }
            //else if (Input.GetKey(KeyCode.D)) // �����ϸ鼭 ��ȸ��
            else if (Input.GetAxis("Horizontal") > 0) // �����ϸ鼭 ��ȸ��
            {
                // ���� �˵� ���� ȸ��

                // ���� �˵� ���� ȸ��
                MoveForwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
                MoveForwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
                MoveForwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
                MoveForwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
            }
            else // �׳� ����
            {
                MoveForwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
                MoveForwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
                MoveForwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
                MoveForwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
            }
        }
        // ���� �� ���ѱ˵��� �� �ִϸ��̼�
        //else if (Input.GetKey(KeyCode.S))
        else if (Input.GetAxis("Vertical") < 0)
        {
            //if (Input.GetKey(KeyCode.A)) // �����ϸ鼭 ��ȸ��
            if (Input.GetAxis("Horizontal") < 0) // �����ϸ鼭 ��ȸ��
            {
                // �¿� �˵� �Ѵ� ����, ������ �� ���� ȸ��
                MoveBackwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
                MoveBackwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
                MoveBackwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
                MoveBackwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
            }
            //else if (Input.GetKey(KeyCode.D)) // �����ϸ鼭 ��ȸ��
            else if (Input.GetAxis("Horizontal") > 0) // �����ϸ鼭 ��ȸ��
            {
                // �¿� �˵� �Ѵ� ����, ������ �� ���� ȸ��
                MoveBackwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
                MoveBackwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
                MoveBackwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
                MoveBackwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
            }
            else // �׳� ����
            {
                MoveBackwardTrackAnim(_rendererR); // ���� ���ѱ˵� ���� ȸ��
                MoveBackwardTrackAnim(_rendererL); // ���� ���ѱ˵� ���� ȸ��
                MoveBackwardAllOfWheel(wheelR); // ���� ��� �� ���� ȸ��
                MoveBackwardAllOfWheel(wheelL); // ���� ��� �� ���� ȸ��
            }
        }
    }


    // Track //
    // ���ѱ˵� ȸ�� �޼��� //
    // ���ѱ˵� ���� ȸ��
    private void MoveForwardTrackAnim(Renderer renderer_RorL)
    {
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1) ; // �������� ��������
        float offset = vertical * Time.deltaTime * trackScrollSpeed * -1.0f; // Tank v2�� Ʈ���� Y���� ������ �� �����ϴ� �����̱� ������ -1�� ������
        renderer_RorL.material.mainTextureOffset = new Vector2(0, offset - Time.time);
    }

    // ���ѱ˵� ���� ȸ��
    private void MoveBackwardTrackAnim(Renderer renderer_RorL)
    {
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0); // �������� ��������
        float offset = vertical * Time.deltaTime * trackScrollSpeed * -1.0f; // Tank v2�� Ʈ���� Y���� ������ �� �����ϴ� �����̱� ������ -1�� ������
        renderer_RorL.material.mainTextureOffset = new Vector2(0, offset + Time.time);
    }

    // Wheel //
    // �� ȸ�� �޼��� //
    // �� 1���� ���� ȸ��, localEulerAngles.x ���� �����ϸ� ����ȸ��
    private void MoveForwardWheelRotation(Transform wheelTransform)
    {
        // �� 1���� ���� ȸ��
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), 0, 1); // �������� ��������
        float eulerX = vertical * Time.deltaTime * wheelRotSpeed;
        wheelTransform.localEulerAngles = new Vector3(eulerX + Time.time * wheelRotAddSpeed, 0, 0);
    }
    // ��, �� �� �� �ʿ� �ִ� ��� �� ���� ȸ��
    private void MoveForwardAllOfWheel(Transform[] wheelTransform)
    {
        foreach(Transform transform in wheelTransform)
        {
            MoveForwardWheelRotation(transform);
        }
    }

    // �� 1���� ���� ȸ��, localEulerAngles.x ���� �����ϸ� ����ȸ��
    private void MoveBackwardWheelRotation(Transform wheelTransform)
    {
        // �� 1���� ���� ȸ��
        float vertical = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0); // �������� ��������
        float eulerX = vertical * Time.deltaTime * wheelRotSpeed;
        wheelTransform.localEulerAngles = new Vector3(eulerX - Time.time * wheelRotAddSpeed, 0, 0);
    }
    // ��, �� �� �� �ʿ� �ִ� ��� �� ���� ȸ��
    private void MoveBackwardAllOfWheel(Transform[] wheelTransform)
    {
        foreach (Transform transform in wheelTransform)
        {
            MoveBackwardWheelRotation(transform);
        }
    }

    // ����Ʈ Ű�� �����Ǵµ��� ������ �����ϴ� �޼���
    private void Acceleration()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (isCollision) // ���𰡿� �浹�� ���¶�� �������� ����
            {
                return;
            }
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, defaultMoveSpeed * 1.7f, Time.deltaTime * defaultMoveSpeed / 2.0f) ;
        }
    }


    // �浹 �˻� �޼���
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ReturnToCombatArea"))
        {
            if (!isDeserted)
            {
                isInCombatArea = false;
            }
            // Ż��ó�� �Ǿ��ٸ�
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

        // Exit �� �ݶ��̴��� �±װ� �����̸� �ٽ� ���� �� �� �ֵ��� isCollision�� false�� ����
        if (other.CompareTag("Building"))
        {
            isCollision = false;
        }
    }

    // �������� ���� ī��Ʈ�ٿ� �޼���
    public void ReturnToCombatAreaCountdown()
    {
        // ���������� ���� �ʰ�
        if (!isInCombatArea)
        {
            // ī��Ʈ �ð��� 0 �ʰ���
            if (countdown > 0)
            {
                countdown -= Time.deltaTime;
                //Mathf.Clamp(countdown, 0f, 11f);
                UIManager.instance.SetActiveReturnToCombatAreaUI(true);
                UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "RETURN TO COMBAT AREA\n" + Mathf.Floor(countdown);
            }
            // ī��Ʈ �ð��� 0 ���ϸ�
            else if (countdown <= 0)
            {
                // �ؽ�Ʈ�� ������ Ż�� �˸�
                isDeserted = true;
                UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "RETURN TO COMBAT AREA\n0";

                if (isDeserted)
                {
                    StartDeserted();
                }
            }
        }
        // �������� ���� �ִٸ�
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
        Debug.Log("�������� ���, Deserted �ڷ�ƾ ����");

        isCoroutineStarted = true; // �ڷ�ƾ �ѹ��� ����ǰ� ��

        UIManager.instance.returnToCombatAreaUI.GetComponentInChildren<Text>().text = "DESERTED";

        //ī�޶� ���� ��Ȱ��ȭ
        Camera.main.GetComponent<CameraMovement>().enabled = false;

        // ��ũ ���� ��Ȱ��ȭ
        isDie = true;
        GetComponentInChildren<TurretControl>().enabled = false;
        GetComponentInChildren<CannonControl>().enabled = false;
        GetComponentInChildren<FireCannon>().enabled = false;
        UIManager.instance.tpsUI.SetActive(false); // TPSUI�� ũ�ν����� ������ź���� ũ�ν���� ��Ȱ��ȭ

        // 1��Ī ���¿��ٸ� 1��Ī UI�� ����, 3��Ī ī�޶�� ��ȯ
        if (Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = true)
        {
            UIManager.instance.fpsUI.SetActive(false);
            Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = false;
            Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = true;
        }

        // ī�޶� ��ȯ ����� EaseIn����, ī�޶� ��ȯ �ҿ�ð� ����
        Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseIn, 3f);

        // ����ķ���� ��ȯ
        Camera.main.GetComponent<CameraMovement>().tpsCinemachineVirtualCamera.enabled = false;
        Camera.main.GetComponent<CameraMovement>().fpsCinemachineVirtualCamera.enabled = false;
        Camera.main.GetComponent<CameraMovement>().deathCinemachineVirtualCamera.enabled = true;

        isInCombatArea = true;
        UIManager.instance.returnToCombatAreaUI.SetActive(false); // �������� ��� UI ��Ȱ��ȭ

        StartCoroutine(GetComponent<TankDamage>().RespawnCoroutine()); // ������ ����
        
        yield return null;
    }
}

