using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Layer Lab 2D Minimal Characters + Casual Monsters 파츠 조립 + 걷기 애니메이션
// 모든 localPosition은 Visual GO(scale=2) 기준 로컬 좌표
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

    // ── 플레이어 파츠 조립 ────────────────────────────────────────
    // 스프라이트 픽셀 기준 (100PPU, Visual scale=2):
    //   Head 94x76px → world 약 0.54 위
    //   Body 63x49px → 기준점(0,0)
    //   Leg  21x20px → world 약 0.34 아래
    //   Weapon 64x93px → 오른쪽 상단
    private void BuildPlayer(int idx)
    {
        string p = $"LayerLab/minimal/{idx}";
        AddLayer(p, "Back",    9, 0,      0);
        AddLayer(p, "Cape",   10, 0,     -0.05f);
        _legA = AddLayer(p, "Leg",  11,  0,     -0.17f);
        _legB = AddLayer(p, "Leg2", 11,  0,     -0.17f);
        if (_legB != null) _legB.gameObject.SetActive(false);
        AddLayer(p, "Body",   12, 0,      0);
        AddLayer(p, "Neck",   13, 0,      0.18f);
        AddLayer(p, "Head",   14, 0,      0.27f);
        AddLayer(p, "Mouth",  15, 0,      0.22f);
        AddLayer(p, "Weapon", 16, 0.15f,  0.15f);
        AddLayer(p, "Shield", 16, -0.15f, 0);
    }

    // ── 몬스터 파츠 조립 ──────────────────────────────────────────
    // Skull body 62x63, head 62x52, leg 25x33, arm 27x28
    // Goblin body 68x78(head 포함), arm 49x41, leg 24x25
    // Slime  body 74x70, weapon 41x57
    private void BuildMonster(MonsterKind kind)
    {
        switch (kind)
        {
            case MonsterKind.Goblin:
            {
                string p = "LayerLab/monsters/goblin";
                _legA = AddLayer(p, "leg",  5,  0,      -0.22f);
                _legB = AddLayer(p, "leg2", 5,  0,      -0.22f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "body", 6, 0,       0.08f);
                AddLayer(p, "arm",  7, 0.22f,   0.10f);
                AddLayer(p, "arm2", 7, -0.10f,  0.05f);
                break;
            }
            case MonsterKind.Skull:
            {
                string p = "LayerLab/monsters/skull";
                _legA = AddLayer(p, "leg",  5,  0,      -0.24f);
                _legB = AddLayer(p, "leg2", 5,  0,      -0.24f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "body", 6, 0,       0);
                AddLayer(p, "arm",  7, 0.22f,   0.05f);
                AddLayer(p, "arm2", 7, -0.12f,  0.05f);
                AddLayer(p, "head", 8, 0,       0.29f);
                break;
            }
            case MonsterKind.Slime:
            {
                string p = "LayerLab/monsters/slime";
                AddLayer(p, "body",   6,  0,      0);
                AddLayer(p, "weapon", 7,  0.20f,  0.28f);
                break;
            }
        }
    }

    private SpriteRenderer AddLayer(string folder, string name, int order, float lx = 0, float ly = 0)
    {
        var spr = Resources.Load<Sprite>($"{folder}/{name}");
        if (spr == null) return null;
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = new Vector3(lx, ly, 0);
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = order;
        _allLayers.Add(sr);
        return sr;
    }

    // ── 애니메이션 API ────────────────────────────────────────────
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
