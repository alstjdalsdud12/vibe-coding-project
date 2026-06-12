using UnityEngine;
using System;

public class MonsterController : MonoBehaviour
{
    public string ZoneName;
    public int ZoneIndex;
    public Action<MonsterController> OnPlayerContact;

    private Rigidbody2D _rb;
    private LayerLabCharacter _layerChar;
    private Vector2 _patrolA, _patrolB;
    private float _speed;
    private bool _paused;
    private bool _goToB;

    public void Init(Vector2 a, Vector2 b, float speed)
    {
        _patrolA = a;
        _patrolB = b;
        _speed = speed;
        _rb = GetComponent<Rigidbody2D>();
        _layerChar = GetComponentInChildren<LayerLabCharacter>();
        _layerChar?.SetWalking(true);
    }

    private void FixedUpdate()
    {
        if (_paused || _rb == null) { if (_rb != null) _rb.velocity = Vector2.zero; return; }

        Vector2 target = _goToB ? _patrolB : _patrolA;
        Vector2 dir = target - (Vector2)transform.position;
        if (dir.magnitude < 0.3f)
        {
            _goToB = !_goToB;
        }
        else
        {
            _rb.velocity = dir.normalized * _speed;
            _layerChar?.SetFacing(dir.x);
        }
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (_rb != null && paused) _rb.velocity = Vector2.zero;
        _layerChar?.SetWalking(!paused);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (_paused) return;
        if (col.CompareTag("Player"))
            OnPlayerContact?.Invoke(this);
    }
}
