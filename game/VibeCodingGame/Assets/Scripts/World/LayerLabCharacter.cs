using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Layer Lab 2D Minimal Characters + Casual Monsters 파츠 조립 + 걷기 애니메이션
// 모든 localPosition 값은 Man_0X.prefab에서 직접 추출한 Layer Lab 원본 좌표
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

    private void BuildPlayer(int idx)
    {
        string p = $"LayerLab/minimal/{idx}";

        switch (idx)
        {
            case 1: // 남자 원시인 — Man_01.prefab
                AddLayer(p, "Cape",   10,  0.040f, 0.445f);
                _legA = AddLayer(p, "Leg",  11,  0.260f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.095f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12,  0.070f, 0.445f);
                AddLayer(p, "Head",   14,  0.085f, 0.930f);
                AddLayer(p, "Mouth",  15,  0.120f, 0.705f);
                AddLayer(p, "Weapon", 16, -0.265f, 0.785f);
                break;

            case 2: // 여자 원시인 — Man_02.prefab
                AddLayer(p, "Cape",   10, -0.010f, 0.445f);
                _legA = AddLayer(p, "Leg",  11,  0.195f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.160f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12,  0.015f, 0.445f);
                AddLayer(p, "Head",   14,  0.000f, 0.985f);
                AddLayer(p, "Mouth",  15,  0.065f, 0.720f);
                AddLayer(p, "Weapon", 16, -0.115f, 0.485f);
                break;

            case 3: // 남자 전사 — Man_03.prefab
                AddLayer(p, "Cape",   10,  0.070f, 0.390f);
                _legA = AddLayer(p, "Leg",  11,  0.265f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.090f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12,  0.075f, 0.435f);
                AddLayer(p, "Head",   14,  0.080f, 0.910f);
                AddLayer(p, "Mouth",  15,  0.125f, 0.690f);
                AddLayer(p, "Weapon", 16, -0.235f, 0.670f);
                break;

            case 4: // 여자 궁수 — Man_04.prefab
                AddLayer(p, "Cape",   10, -0.195f, 0.385f);
                _legA = AddLayer(p, "Leg",  11,  0.055f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.300f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12, -0.120f, 0.415f);
                AddLayer(p, "Head",   14, -0.165f, 0.875f);
                AddLayer(p, "Mouth",  15, -0.030f, 0.720f);
                AddLayer(p, "Weapon",  16,  0.090f, 0.480f);
                AddLayer(p, "Weapon_", 16,  0.255f, 0.520f);
                break;

            case 5: // 스파르타 — Man_05.prefab
                AddLayer(p, "Cape",   10, -0.135f, 0.400f);
                _legA = AddLayer(p, "Leg",  11,  0.060f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.295f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12, -0.130f, 0.445f);
                AddLayer(p, "Head",   14, -0.130f, 0.865f);
                AddLayer(p, "Mouth",  15, -0.080f, 0.690f);
                AddLayer(p, "Weapon", 11,  0.000f, 0.445f);
                AddLayer(p, "Shield", 16,  0.240f, 0.410f);
                break;

            case 6: // 아처 — Man_06.prefab
                AddLayer(p, "Back",    9, -0.105f, 0.495f);
                AddLayer(p, "Cape",   10, -0.015f, 0.400f);
                _legA = AddLayer(p, "Leg",  11,  0.180f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.175f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12,  0.000f, 0.445f);
                AddLayer(p, "Head",   14,  0.005f, 0.965f);
                AddLayer(p, "Mouth",  15,  0.065f, 0.715f);
                AddLayer(p, "Weapon", 16,  0.000f, 0.415f);
                break;

            case 7: // 군인 베레모 — Man_07.prefab
                AddLayer(p, "Cape",   10,  0.040f, 0.435f);
                _legA = AddLayer(p, "Leg",  11,  0.235f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.120f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12,  0.045f, 0.445f);
                AddLayer(p, "Head",   14,  0.010f, 0.925f);
                AddLayer(p, "Mouth",  15,  0.105f, 0.700f);
                AddLayer(p, "Weapon", 16, -0.255f, 0.665f);
                break;

            case 8: // 일반 군인 — Man_08.prefab
                AddLayer(p, "Cape",   10, -0.155f, 0.385f);
                _legA = AddLayer(p, "Leg",  11,  0.095f, 0.220f);
                _legB = AddLayer(p, "Leg2", 11, -0.260f, 0.220f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "Body",   12, -0.085f, 0.445f);
                AddLayer(p, "Neck",   13, -0.125f, 0.570f);
                AddLayer(p, "Head",   14, -0.095f, 0.895f);
                AddLayer(p, "Mouth",  15, -0.030f, 0.690f);
                AddLayer(p, "Weapon", 16,  0.110f, 0.400f);
                break;
        }
    }

    private void BuildMonster(MonsterKind kind)
    {
        switch (kind)
        {
            case MonsterKind.Goblin:
            {
                string p = "LayerLab/monsters/goblin";
                _legA = AddLayer(p, "leg",  5,  0.130f, 0.255f);
                _legB = AddLayer(p, "leg2", 5, -0.140f, 0.255f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "body", 6,  0.000f, 0.580f);
                AddLayer(p, "arm",  7, -0.025f, 0.465f);
                AddLayer(p, "arm2", 7,  0.300f, 0.415f);
                break;
            }
            case MonsterKind.Skull:
            {
                string p = "LayerLab/monsters/skull";
                _legA = AddLayer(p, "leg",  5, -0.090f, 0.295f);
                _legB = AddLayer(p, "leg2", 5,  0.200f, 0.285f);
                if (_legB != null) _legB.gameObject.SetActive(false);
                AddLayer(p, "body", 6,  0.055f, 0.575f);
                AddLayer(p, "arm",  7, -0.130f, 0.775f);
                AddLayer(p, "head", 8,  0.105f, 0.870f);
                break;
            }
            case MonsterKind.Slime:
            {
                string p = "LayerLab/monsters/slime";
                AddLayer(p, "body",   6,  0.025f, 0.430f);
                AddLayer(p, "weapon", 7, -0.190f, 0.465f);
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
        var s = transform.localScale;
        transform.localScale = new Vector3(right ? Mathf.Abs(s.x) : -Mathf.Abs(s.x), s.y, s.z);
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
