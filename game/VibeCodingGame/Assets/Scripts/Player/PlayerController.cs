using UnityEngine;

public class MapPlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Camera _cam;
    private bool _movementEnabled = true;
    private const float Speed = 7f;
    private static readonly Vector2 MapMin = new Vector2(-20f, 0f);
    private static readonly Vector2 MapMax = new Vector2(21f, 90f);

    private SpriteRenderer[] _arrows;
    private static readonly Color ArrowActive = new Color(1f, 0.9f, 0.3f, 0.92f);
    private static readonly Color ArrowDim   = new Color(1f, 1f, 1f, 0.12f);

    private LayerLabCharacter _layerChar;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _layerChar = GetComponentInChildren<LayerLabCharacter>();
        SetupArrows();
    }

    private void SetupArrows()
    {
        var sprite = CreateTriangleSprite();
        var positions = new Vector3[] {
            new Vector3( 1.6f,  0,   0),
            new Vector3( 0,     1.6f,0),
            new Vector3(-1.6f,  0,   0),
            new Vector3( 0,    -1.6f,0),
        };
        var rotations = new float[] { 0, 90, 180, 270 };
        _arrows = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Arrow_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = positions[i];
            go.transform.localRotation = Quaternion.Euler(0, 0, rotations[i]);
            go.transform.localScale    = new Vector3(0.9f, 0.9f, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.color        = ArrowDim;
            sr.sortingOrder = 20;
            _arrows[i] = sr;
        }
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null)
            _cam.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
    }

    private void FixedUpdate()
    {
        if (!_movementEnabled)
        {
            _rb.velocity = Vector2.zero;
            UpdateArrows(Vector2.zero);
            return;
        }

        Vector2 dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.touchCount > 0)
        {
            var delta = Input.GetTouch(0).position
                      - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (delta.magnitude > 40f) dir = delta.normalized;
        }

        dir = dir.magnitude > 0 ? dir.normalized : Vector2.zero;
        _rb.velocity = dir * Speed;

        var pos = _rb.position;
        pos.x = Mathf.Clamp(pos.x, MapMin.x, MapMax.x);
        pos.y = Mathf.Clamp(pos.y, MapMin.y, MapMax.y);
        _rb.position = pos;

        UpdateArrows(dir);
        UpdateAnimation(dir);
    }

    private void UpdateAnimation(Vector2 dir)
    {
        if (_layerChar == null) return;
        _layerChar.SetWalking(dir.magnitude > 0.1f);
        _layerChar.SetFacing(dir.x);
    }

    private void UpdateArrows(Vector2 dir)
    {
        if (_arrows == null) return;
        _arrows[0].color = dir.x >  0.1f ? ArrowActive : ArrowDim;
        _arrows[1].color = dir.y >  0.1f ? ArrowActive : ArrowDim;
        _arrows[2].color = dir.x < -0.1f ? ArrowActive : ArrowDim;
        _arrows[3].color = dir.y < -0.1f ? ArrowActive : ArrowDim;
    }

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;
        if (!enabled && _rb != null) _rb.velocity = Vector2.zero;
        if (!enabled)
        {
            UpdateArrows(Vector2.zero);
            _layerChar?.SetWalking(false);
        }
    }

    // 오른쪽을 가리키는 삼각형 스프라이트 생성 (회전으로 4방향 사용)
    private static Sprite CreateTriangleSprite(int res = 32)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = res / 2f;
        for (int px = 0; px < res; px++)
            for (int py = 0; py < res; py++)
            {
                float maxX = res * (1f - Mathf.Abs(py - half) / half);
                tex.SetPixel(px, py, px < maxX ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}
