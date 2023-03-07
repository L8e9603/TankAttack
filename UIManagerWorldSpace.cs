using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드
using UnityEngine.UI; // UI 관련 코드
using UnityEngine.Rendering;

// 필요한 UI에 즉시 접근하고 변경할 수 있도록 허용하는 UI 매니저
public class UIManagerWorldSpace : MonoBehaviour
{
    // 레트로 스타일 싱글톤 구현

/*    // 싱글톤 접근용 프로퍼티
    public static UIManagerWorldSpace instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<UIManagerWorldSpace>();
            }

            return m_instance;
        }
    }

    private static UIManagerWorldSpace m_instance; // 싱글톤이 할당될 변수
*/

    public static UIManagerWorldSpace Instance { get; private set; } // 싱글턴, 메모리에 인스턴스화

    private UIManagerWorldSpace uIManagerWorldSpace;

    public GameObject fpsUI;

    private void Awake()
    {
        Instance = this;
        uIManagerWorldSpace = GetComponent<UIManagerWorldSpace>();
    }

    private void Update()
    {

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
}

