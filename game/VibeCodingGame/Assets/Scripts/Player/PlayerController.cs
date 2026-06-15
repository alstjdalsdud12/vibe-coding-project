using UnityEngine;

public class MapPlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Camera _cam;
    private bool _movementEnabled = true;
    private const float Speed = 7f;
    private static readonly Vector2 MapMin = new Vector2(-20f, 0f);
    private static readonly Vector2 MapMax = new Vector2(21f, 90f);

    private LayerLabCharacter _layerChar;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _layerChar = GetComponentInChildren<LayerLabCharacter>();
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

        UpdateAnimation(dir);
    }

    private void UpdateAnimation(Vector2 dir)
    {
        if (_layerChar == null) return;
        _layerChar.SetWalking(dir.magnitude > 0.1f);
        _layerChar.SetFacing(dir.x);
    }

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;
        if (!enabled && _rb != null) _rb.velocity = Vector2.zero;
        if (!enabled) _layerChar?.SetWalking(false);
    }
}
