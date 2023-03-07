using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackAnim : MonoBehaviour
{
    private float scrollSpeed = 1.0f;
    private Renderer _renderer;

    // Start is called before the first frame update
    void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // �� �� ���ڸ� ȸ�� �ÿ��� �˵� ȸ��
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            var offset2 = Time.time * scrollSpeed * Input.GetAxisRaw("Horizontal");

            _renderer.material.SetTextureOffset("_MainTex", new Vector2(0, offset2));
            _renderer.material.SetTextureOffset("_BumpMap", new Vector2(0, offset2));
        }

        else
        {
            // �ؽ�ó�� Offset Y���� ������ �Ǹ� ��ũ�� ������ ���� �ϴ� �� ó�� ����
            var offset = Time.time * scrollSpeed * Input.GetAxisRaw("Vertical");
            // _MainTex : Diffuse ��ü�� ���̰�, ��ü�� / _BumpMap : Normal Map ���� ���̰�, ���� / _Cube : CubeMap
            _renderer.material.SetTextureOffset("_MainTex", new Vector2(0, offset));
            _renderer.material.SetTextureOffset("_BumpMap", new Vector2(0, offset));
            _renderer.material.mainTextureOffset = new Vector2(0, offset);
        }
    }
}
