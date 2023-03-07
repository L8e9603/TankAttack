using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CinemachineShakeTPS : MonoBehaviour
{
    // 싱글턴, 메모리에 인스턴스화, 시네머신 카메라 하나로 아무데서나 흔들어 제낌
    public static CinemachineShakeTPS Instance { get; private set; } // 싱글턴

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
        // Noise 필드의 BasicMultiChannelPerlin는 GetCinemachineComponent 메소드로 접근한다
        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;

        shakeTimer = time;
        startingIntensity = time;
        startingIntensity = intensity;
    }

    private void Update()
    {
        // 흔들어 제끼는 중이면
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime; // 타이머의 시간이 흐른다
            if (shakeTimer <= 0f)
            {
                // 흔들기 그만
                CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =
                    cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0.03f; // 흔들림 강도를 0.05까지 급격히 줄이고 0.05부터 선형보간
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(0.03f, 0f, (1 - (shakeTimer / shakeTimerTotal)));
            }

        }
    }
}
