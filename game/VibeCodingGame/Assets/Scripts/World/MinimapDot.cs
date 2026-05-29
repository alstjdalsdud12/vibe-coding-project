using UnityEngine;

// 미니맵 위에 플레이어 위치를 표시하는 점
public class MinimapDot : MonoBehaviour
{
    public Vector2 WorldMin;
    public Vector2 WorldSize;

    private Transform _target;
    private RectTransform _rt;

    private void Start() => _rt = GetComponent<RectTransform>();

    private void Update()
    {
        if (_target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _target = p.transform;
            return;
        }
        var uv = new Vector2(
            Mathf.Clamp01((_target.position.x - WorldMin.x) / WorldSize.x),
            Mathf.Clamp01((_target.position.y - WorldMin.y) / WorldSize.y)
        );
        _rt.anchorMin = _rt.anchorMax = uv;
        _rt.anchoredPosition = Vector2.zero;
    }
}
