using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ڵ�
using UnityEngine.UI; // UI ���� �ڵ�
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// �ʿ��� UI�� ��� �����ϰ� ������ �� �ֵ��� ����ϴ� UI �Ŵ���
public class UIManager : MonoBehaviour
{
    // �̱��� ���ٿ� ������Ƽ
    public static UIManager instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<UIManager>();
            }

            return m_instance;
        }
    }

    private static UIManager m_instance; // �̱����� �Ҵ�� ����

    public GameObject leaveGameUI; // �κ�� �̵�, ���� ������ UI

    public GameObject returnToCombatAreaUI; // �������� ��� ��� UI

    public GameObject scoreBoardUI; // ������ UI, ���� �� �� Ű�� ������ �� �� ����
    public Text textScoreBoardPlayerInfo; // �����ǿ� ǥ�� �� �÷��̾� ����

    public GameObject tpsUI;

    public GameObject fpsUI;

    public GameObject respawnUI;
    
    public CanvasGroup fpsUIPanelCanvasGroup; // 1��Ī 3��Ī ��ȯ ȿ���� �� �ǳ� �̹����� ĵ���� �׷�
    private Coroutine coroutine;
    private float accumTime = 0f;
    private float fadeTime = 0.3f;

    public RectTransform reticleRectTransform; // FireCannon.cs���� ��ƼŬ ũ�⸦ �������� �����ϱ� ���� ����

    public Volume fpsVolume; // FireCannon.cs���� ����Ʈ���μ��� ���� ��� ���� ����

    public Image hitPositionCrosshair;

    public Image ImageReticle; // ��ũ���� ��ƼŬ �ٸ��� ���� �Ҵ�

    public void SetActiveLeaveGameUI(bool active)
    {
        if (active)
        {
            leaveGameUI.SetActive(true);
        }
        else if (!active)
        {
            leaveGameUI.SetActive(false);
        }
    }

    public void SetActiveReturnToCombatAreaUI(bool active)
    {
        if (active)
        {
            returnToCombatAreaUI.SetActive(true);
        }
        else if (!active)
        {
            returnToCombatAreaUI.SetActive(false);
        }
    }

    public void SetActiveScoreBoardUI(bool active)
    {
        if (active)
        {
            scoreBoardUI.SetActive(true);
        }
        else if (!active)
        {
            scoreBoardUI.SetActive(false);
        }
    }

    public void SetActiveTPSUI(bool active)
    {
        if (active)
        {
            tpsUI.SetActive(true);
        }
        else if (!active)
        {
            tpsUI.SetActive(false);
        }
    }

    public void SetActiveFPSUI(bool active)
    {
        if (active)
        {
            fpsUI.SetActive(true);
        }
        else if (!active)
        {
            fpsUI.SetActive(false);
        }
    }

    public void StartCoroutineFadeout()
    {
        fpsUIPanelCanvasGroup.alpha = 1.0f;

        if (coroutine != null)
        {
            StopAllCoroutines();
            coroutine = null;
        }
        coroutine = StartCoroutine(FadeOut(fpsUIPanelCanvasGroup));
    }

    public IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        fpsUIPanelCanvasGroup.alpha = 1.0f;
        yield return new WaitForSeconds(0.25f);

        accumTime = 0f;
     
        while (accumTime < fadeTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, accumTime / fadeTime);
            yield return 0;
            accumTime += Time.deltaTime;
        }

        canvasGroup.alpha = 0f;
        
        yield return null;
    }

    public void SetActiveHitPositionCrosshair(bool active)
    {
        if (active)
        {
            hitPositionCrosshair.enabled = true;
        }
        else if (!active)
        {
            hitPositionCrosshair.enabled = false;
        }
    }

    public void SetActiveRespawnUI(bool active)
    {
        if (active)
        {
            respawnUI.SetActive(true);
        }
        else if (!active)
        {
            respawnUI.SetActive(false);
        }
    }
}