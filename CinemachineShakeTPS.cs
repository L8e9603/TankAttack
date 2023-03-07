using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CinemachineShakeTPS : MonoBehaviour
{
    // �̱���, �޸𸮿� �ν��Ͻ�ȭ, �ó׸ӽ� ī�޶� �ϳ��� �ƹ������� ���� ����
    public static CinemachineShakeTPS Instance { get; private set; } // �̱���

    private CinemachineVirtualCamera cinemachineVirtualCamera;
    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;

    private void Awake()
    {
        Instance = this;
        cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();

    }

    public void ShakeCamera(float intensity, float time)
    {
        // Noise �ʵ��� BasicMultiChannelPerlin�� GetCinemachineComponent �޼ҵ�� �����Ѵ�
        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;

        shakeTimer = time;
        startingIntensity = time;
        startingIntensity = intensity;
    }

    private void Update()
    {
        // ���� ������ ���̸�
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime; // Ÿ�̸��� �ð��� �帥��
            if (shakeTimer <= 0f)
            {
                // ���� �׸�
                CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =
                    cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0.03f; // ��鸲 ������ 0.05���� �ް��� ���̰� 0.05���� ��������
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(0.03f, 0f, (1 - (shakeTimer / shakeTimerTotal)));
            }

        }
    }
}
