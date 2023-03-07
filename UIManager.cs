using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드
using UnityEngine.UI; // UI 관련 코드
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 필요한 UI에 즉시 접근하고 변경할 수 있도록 허용하는 UI 매니저
public class UIManager : MonoBehaviour
{
    // 싱글톤 접근용 프로퍼티
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

    private static UIManager m_instance; // 싱글톤이 할당될 변수

    public GameObject leaveGameUI; // 로비로 이동, 게임 떠나기 UI

    public GameObject returnToCombatAreaUI; // 전투지역 벗어남 경고 UI

    public GameObject scoreBoardUI; // 점수판 UI, 게임 중 탭 키를 누르면 볼 수 있음
    public Text textScoreBoardPlayerInfo; // 점수판에 표시 될 플레이어 정보

    public GameObject tpsUI;

    public GameObject fpsUI;

    public GameObject respawnUI;
    
    public CanvasGroup fpsUIPanelCanvasGroup; // 1인칭 3인칭 전환 효과에 쓸 판넬 이미지의 캔버스 그룹
    private Coroutine coroutine;
    private float accumTime = 0f;
    private float fadeTime = 0.3f;

    public RectTransform reticleRectTransform; // FireCannon.cs에서 레티클 크기를 동적으로 제어하기 위한 변수

    public Volume fpsVolume; // FireCannon.cs에서 포스트프로세싱 동적 제어를 위한 변수

    public Image hitPositionCrosshair;

    public Image ImageReticle; // 탱크마다 레티클 다르게 동적 할당

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