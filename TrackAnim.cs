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
        // 좌 우 제자리 회전 시에도 궤도 회전
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            var offset2 = Time.time * scrollSpeed * Input.GetAxisRaw("Horizontal");

            _renderer.material.SetTextureOffset("_MainTex", new Vector2(0, offset2));
            _renderer.material.SetTextureOffset("_BumpMap", new Vector2(0, offset2));
        }

        else
        {
            // 텍스처의 Offset Y값이 음수가 되면 탱크가 앞으로 전진 하는 것 처럼 보임
            var offset = Time.time * scrollSpeed * Input.GetAxisRaw("Vertical");
            // _MainTex : Diffuse 물체의 깊이감, 입체감 / _BumpMap : Normal Map 평면상 높이값, 굴곡 / _Cube : CubeMap
            _renderer.material.SetTextureOffset("_MainTex", new Vector2(0, offset));
            _renderer.material.SetTextureOffset("_BumpMap", new Vector2(0, offset));
            _renderer.material.mainTextureOffset = new Vector2(0, offset);
        }
    }
}
