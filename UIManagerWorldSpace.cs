using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ڵ�
using UnityEngine.UI; // UI ���� �ڵ�
using UnityEngine.Rendering;

// �ʿ��� UI�� ��� �����ϰ� ������ �� �ֵ��� ����ϴ� UI �Ŵ���
public class UIManagerWorldSpace : MonoBehaviour
{
    // ��Ʈ�� ��Ÿ�� �̱��� ����

/*    // �̱��� ���ٿ� ������Ƽ
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

    private static UIManagerWorldSpace m_instance; // �̱����� �Ҵ�� ����
*/

    public static UIManagerWorldSpace Instance { get; private set; } // �̱���, �޸𸮿� �ν��Ͻ�ȭ

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

