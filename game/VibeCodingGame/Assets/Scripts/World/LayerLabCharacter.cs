using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerLabCharacter : MonoBehaviour
{
    public enum MonsterKind { Skull, Goblin, Slime }

    private readonly List<SpriteRenderer> _allLayers = new List<SpriteRenderer>();
    private SpriteRenderer _legA, _legB;
    private Coroutine _animRoutine;
    private bool _walking;
    private bool _facingRight;

    public static LayerLabCharacter AttachPlayer(GameObject go, int idx)
    {
        var c = go.AddComponent<LayerLabCharacter>();
        c.BuildPlayer(Mathf.Clamp(idx, 1, 8));
        return c;
    }

    public static LayerLabCharacter AttachMonster(GameObject go, MonsterKind kind)
    {
        var c = go.AddComponent<LayerLabCharacter>();
        c.BuildMonster(kind);
        return c;
    }

    // ── player layers ─────────────────────────────────────────────
    private void BuildPlayer(int idx)
    {
        string p = $"LayerLab/minimal/{idx}";
        AddLayer(p, "Back",    9);
        AddLayer(p, "Cape",   10);
        _legA = AddLayer(p, "Leg",  10);
        _legB = AddLayer(p, "Leg2", 10);
        if (_legB != null) _legB.gameObject.SetActive(false);
        AddLayer(p, "Body",   11);
        AddLayer(p, "Neck",   11);
        AddLayer(p, "Head",   12);
        AddLayer(p, "Mouth",  13);
        AddLayer(p, "Weapon", 14);
        AddLayer(p, "Shield", 14);
    }

    // ── monster layers ────────────────────────────────────────────
    private void BuildMonster(MonsterKind kind)
    {
        switch (kind)
        {
            case MonsterKind.Goblin:
            {
                string p = "LayerLab/monsters/goblin";
                _legA = AddLayer(p, "leg",  5);
                _legB = AddLayer(p, "leg2", 5);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "body", 6);
                AddLayer(p, "arm",  7);
                AddLayer(p, "arm2", 7);
                break;
            }
            case MonsterKind.Skull:
            {
                string p = "LayerLab/monsters/skull";
                _legA = AddLayer(p, "leg",  5);
                _legB = AddLayer(p, "leg2", 5);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "body", 6);
                AddLayer(p, "arm",  7);
                AddLayer(p, "arm2", 7);
                AddLayer(p, "head", 8);
                break;
            }
            case MonsterKind.Slime:
            {
                string p = "LayerLab/monsters/slime";
                AddLayer(p, "body",   6);
                AddLayer(p, "weapon", 7);
                break;
            }
        }
    }

    private SpriteRenderer AddLayer(string folder, string name, int order)
    {
        var spr = Resources.Load<Sprite>($"{folder}/{name}");
        if (spr == null) return null;
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = order;
        _allLayers.Add(sr);
        return sr;
    }

    // ── animation API ─────────────────────────────────────────────
    public void SetWalking(bool walk)
    {
        if (walk == _walking) return;
        _walking = walk;
        if (walk)
        {
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(LegCycle());
        }
        else
        {
            if (_animRoutine != null) { StopCoroutine(_animRoutine); _animRoutine = null; }
            ShowLeg(true);
        }
    }

    public void SetFacing(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        bool right = dx > 0;
        if (right == _facingRight) return;
        _facingRight = right;
        foreach (var sr in _allLayers)
            if (sr != null) sr.flipX = right;
    }

    private void OnEnable()
    {
        if (_walking)
        {
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(LegCycle());
        }
    }

    private void ShowLeg(bool a)
    {
        if (_legA != null) _legA.gameObject.SetActive(a);
        if (_legB != null) _legB.gameObject.SetActive(!a);
    }

    private IEnumerator LegCycle()
    {
        bool a = true;
        while (true)
        {
            ShowLeg(a);
            a = !a;
            yield return new WaitForSeconds(0.18f);
        }
    }
}
