using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Sprite _heroSprite;       // 인스펙터에서 캐릭터 스프라이트 할당
    [SerializeField] private float  _heroScale = 1.0f; // 크기 (1.0 기준, 줄이려면 0.5 등)
    [SerializeField] private float  _heroYOffset = 0f; // 위아래 위치 조정 (위=양수, 아래=음수)

    private TextMeshProUGUI _tapText;
    private RectTransform   _titleFloat;
    private RectTransform   _subFloat;
    private RectTransform   _tapFloat;

    private SpriteRenderer   _skySR;
    private SpriteRenderer   _sunSR;
    private SpriteRenderer   _moonSR;
    private SpriteRenderer[] _starsSR;
    private SpriteRenderer[] _monsterEyes;
    private SpriteRenderer[] _monsterSilhouettes;
    private Transform[]      _trees;
    private float[]          _swayOffsets;

    private float _dayProgress = 0.30f;
    private const float DaySpeed  = 1f / 40f;
    private const float HorizonY  = -2.0f;
    private const float OrthoSize = 5.0f;

    // 소팅오더: 태양/달(2) < 땅(8) < 길·눈(9) < 나무배경(10) < 나무전경(14) < 영웅(18)
    private const int SO_Sky    = 0;
    private const int SO_Stars  = 1;
    private const int SO_Sun    = 2;
    private const int SO_Ground = 8;
    private const int SO_Path   = 9;
    private const int SO_Eyes   = 9;
    private const int SO_TreeBG  = 10;
    private const int SO_TreeFG  = 14;
    private const int SO_HeroCape = 18; // 망토: 나무 위, 몸통 뒤
    private const int SO_Hero     = 19; // 영웅 몸체

    private static readonly Color SkyNight = new Color(0.03f, 0.07f, 0.22f); // 어두운 파란색
    private static readonly Color SkyDawn  = new Color(0.70f, 0.28f, 0.10f);
    private static readonly Color SkyDay   = new Color(0.28f, 0.58f, 0.95f);
    private static readonly Color SkyDusk  = new Color(0.62f, 0.16f, 0.10f);

    private void Start()
    {
        SetupCamera();
        BuildWorldScene();
        UIHelper.CreateCanvas(out _);
        var canvas = FindObjectOfType<Canvas>().transform;
        BuildUI(canvas);
    }

    private void Update()
    {
        _dayProgress = (_dayProgress + Time.deltaTime * DaySpeed) % 1f;
        UpdateSky();
        UpdateCelestials();
        SwayTrees();
        UpdateStars();
        UpdateMonsterEyes();
        FloatTexts();
    }

    private void FloatTexts()
    {
        float t = Time.time;
        float hDrift = Mathf.Sin(t * 0.28f) * 6f;
        if (_titleFloat) _titleFloat.anchoredPosition = new Vector2(hDrift,       Mathf.Sin(t * 0.65f)        * 20f);
        if (_subFloat)   _subFloat.anchoredPosition   = new Vector2(-hDrift * 0.5f, Mathf.Sin(t * 0.48f + 1.4f) * 13f);
        if (_tapFloat)   _tapFloat.anchoredPosition   = new Vector2(0,              Mathf.Sin(t * 0.85f + 0.7f) * 8f);
    }

    // ─── 카메라 ────────────────────────────────────────────
    private void SetupCamera()
    {
        UIHelper.CreateCamera();
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic     = true;
        cam.orthographicSize = OrthoSize;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.backgroundColor    = Color.black;
    }

    // ─── 월드 씬 ───────────────────────────────────────────
    private void BuildWorldScene()
    {
        var skyGO = new GameObject("Sky");
        _skySR = skyGO.AddComponent<SpriteRenderer>();
        _skySR.sprite = MakeSquareSprite();
        _skySR.sortingOrder = SO_Sky;
        skyGO.transform.localScale = new Vector3(40, 30, 1);

        // 땅 top edge = HorizonY: center = HorizonY - scaleY/2 = -2 - 4 = -6
        MakeWorldSprite("Ground", new Vector3(0, HorizonY - 4f, 0),
            new Vector3(40, 8, 1), new Color(0.09f, 0.20f, 0.07f), SO_Ground);

        BuildPath();
        BuildStars();
        BuildCelestials();
        BuildTrees();
        BuildHero();
        BuildMonsterSilhouettes();
        BuildMonsterEyes();

        UpdateSky();
        UpdateCelestials();
        UpdateStars();
        UpdateMonsterEyes();
    }

    // ─── 꼬불꼬불 길 ───────────────────────────────────────
    private void BuildPath()
    {
        var color = new Color(0.63f, 0.48f, 0.28f);
        int segs = 48;
        for (int i = 0; i < segs; i++)
        {
            float t = (float)i / (segs - 1);
            float x = Mathf.Lerp(-14f, 14f, t);
            float y = HorizonY - 0.55f + Mathf.Sin(t * Mathf.PI * 2.5f) * 0.15f;
            MakeWorldSprite("Path_" + i, new Vector3(x, y, 0),
                new Vector3(0.82f, 0.34f, 1), color, SO_Path);
        }
    }

    private SpriteRenderer MakeWorldSprite(string name, Vector3 pos, Vector3 scale,
                                            Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite();
        sr.color        = color;
        sr.sortingOrder = order;
        return sr;
    }

    // ─── 영웅 ─────────────────────────────────────────────
    private void BuildHero()
    {
        var go = new GameObject("Hero");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = SO_Hero;

        if (_heroSprite != null)
        {
            sr.sprite = _heroSprite;
            sr.color  = Color.white;

            // 크기: 해상도 무관하게 목표 높이로 자동 스케일
            float targetH   = 1.8f * _heroScale;
            float autoScale = targetH / _heroSprite.bounds.size.y;
            go.transform.localScale = new Vector3(autoScale, autoScale, 1f);

            // 위치: 크기 완전히 무관. Hero Y Offset으로만 조정 (기본 0, 내리려면 -0.5 등)
            go.transform.position = new Vector3(-0.4f, HorizonY + 0.5f + _heroYOffset, 0);
        }
        else
        {
            sr.sprite = MakeHeroPixelArt();
            sr.color  = new Color(0.06f, 0.05f, 0.12f);
            go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            go.transform.position   = new Vector3(-0.4f, HorizonY, 0);
        }
    }

    // 48x80 픽셀 후드 암살자 - 팔 오른쪽으로 뻗어 단검 겨누는 포즈
    private static Sprite MakeHeroPixelArt()
    {
        const int W = 48, H = 80;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                tex.SetPixel(x, y, Color.clear);

        void F(int x0, int y0, int x1, int y1)
        {
            for (int xx = Mathf.Max(0,x0); xx <= Mathf.Min(W-1,x1); xx++)
                for (int yy = Mathf.Max(0,y0); yy <= Mathf.Min(H-1,y1); yy++)
                    tex.SetPixel(xx, yy, Color.white);
        }

        // ── 부츠 ────────────────────────────────────────────
        F(7,  0, 13,  3);
        F(8,  4, 12, 10);
        F(17, 0, 23,  3);
        F(18, 4, 22, 10);

        // ── 다리 ────────────────────────────────────────────
        F(8,  11, 12, 26);
        F(18, 11, 22, 26);
        F(7,  27, 13, 36);   // 왼 허벅지
        F(17, 27, 23, 36);   // 오른 허벅지

        // ── 망토 (왼쪽으로만 길게 펄럭임) ──────────────────
        F(0,  14,  7, 30);   // 왼 망토 하단
        F(0,  30,  6, 48);   // 왼 망토 상단

        // ── 몸통 ────────────────────────────────────────────
        F(7,  37, 22, 52);

        // ── 어깨 ────────────────────────────────────────────
        F(5,  53, 24, 58);

        // ── 목 ──────────────────────────────────────────────
        F(10, 59, 16, 62);

        // ── 후드 (왼쪽으로 치우쳐 뾰족) ─────────────────────
        F(7,  63, 20, 67);
        F(8,  68, 18, 71);
        F(9,  72, 17, 74);
        F(10, 75, 16, 76);
        F(11, 77, 15, 77);
        F(12, 78, 14, 79);   // 뾰족한 끝

        // ── 오른팔 (수평으로 오른쪽 쭉 뻗음) ───────────────
        F(22, 50, 36, 55);

        // ── 단검 (오른쪽 끝에서 비스듬히 겨눔) ──────────────
        F(34, 48, 37, 55);   // 그립
        F(34, 45, 34, 48);   // 가드 (세로 막대)
        F(35, 43, 47, 46);   // 날 (오른쪽으로 뻗음)
        F(46, 40, 47, 43);   // 날끝 (뾰족)

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.3f, 0f), H);
    }

    // ─── 몬스터 실루엣 (밤에만 나무 뒤에서 등장) ──────────
    private void BuildMonsterSilhouettes()
    {
        float[] mx = { -2.4f, -1.2f,  1.2f,  2.3f };
        float[] ey = { HorizonY + 0.25f, HorizonY + 0.40f,
                       HorizonY + 0.30f, HorizonY + 0.18f };

        const float scale      = 1.0f;
        const float headNormY  = 0.82f; // 눈이 블롭 상단 근처에 위치

        _monsterSilhouettes = new SpriteRenderer[mx.Length];
        var sp = MakeMonsterBodySprite();

        for (int i = 0; i < mx.Length; i++)
        {
            var go = new GameObject("MonsterBody_" + i);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sp;
            sr.color        = new Color(0.01f, 0.01f, 0.03f, 0f);
            sr.sortingOrder = SO_Ground - 1; // 땅·나무 모두 뒤

            float bottomY = ey[i] - headNormY * scale;
            go.transform.position   = new Vector3(mx[i], bottomY, 0);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            _monsterSilhouettes[i]  = sr;
        }
    }

    // ─── 몬스터 눈빛 (밤에만 등장) ─────────────────────────
    private void BuildMonsterEyes()
    {
        float[] ex = { -2.4f, -1.2f, 1.2f, 2.3f };
        float[] ey = { HorizonY + 0.25f, HorizonY + 0.40f,
                        HorizonY + 0.30f, HorizonY + 0.18f };

        _monsterEyes = new SpriteRenderer[ex.Length * 2];
        var sp = MakeCircleSprite(16);

        for (int i = 0; i < ex.Length; i++)
            for (int s = 0; s < 2; s++)
            {
                float ox = ex[i] + (s == 0 ? -0.11f : 0.11f);
                var go = new GameObject($"Eye_{i}_{s}");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = sp;
                sr.color        = new Color(0.90f, 0.05f, 0.05f, 0f);
                sr.sortingOrder = SO_Eyes;
                go.transform.position   = new Vector3(ox, ey[i], 0);
                go.transform.localScale = new Vector3(0.10f, 0.08f, 1);
                _monsterEyes[i * 2 + s] = sr;
            }
    }

    private void UpdateMonsterEyes()
    {
        if (_monsterEyes == null) return;

        float n;
        if      (_dayProgress < 0.22f) n = 1f;
        else if (_dayProgress < 0.32f) n = 1f - (_dayProgress - 0.22f) / 0.10f;
        else if (_dayProgress < 0.68f) n = 0f;
        else if (_dayProgress < 0.78f) n = (_dayProgress - 0.68f) / 0.10f;
        else                           n = 1f;

        float pulse = Mathf.Sin(Time.time * 1.8f) * 0.25f + 0.75f;
        float alpha = n * pulse * 0.88f;

        foreach (var sr in _monsterEyes)
            if (sr != null) sr.color = new Color(0.92f, 0.05f, 0.05f, alpha);

        // 실루엣: 눈보다 살짝 먼저 등장, 펄스 없이 안정적으로
        float bodyAlpha = n * 0.82f;
        if (_monsterSilhouettes != null)
            foreach (var sr in _monsterSilhouettes)
                if (sr != null) sr.color = new Color(0.01f, 0.01f, 0.03f, bodyAlpha);
    }

    // ─── 하늘 색 ───────────────────────────────────────────
    private void UpdateSky()
    {
        if (_skySR == null) return;
        _skySR.color = SampleSkyColor(_dayProgress);
    }

    private Color SampleSkyColor(float t)
    {
        if (t < 0.20f) return SkyNight;
        if (t < 0.30f) return Color.Lerp(SkyNight, SkyDawn,  (t - 0.20f) / 0.10f);
        if (t < 0.38f) return Color.Lerp(SkyDawn,  SkyDay,   (t - 0.30f) / 0.08f);
        if (t < 0.62f) return SkyDay;
        if (t < 0.70f) return Color.Lerp(SkyDay,   SkyDusk,  (t - 0.62f) / 0.08f);
        if (t < 0.80f) return Color.Lerp(SkyDusk,  SkyNight, (t - 0.70f) / 0.10f);
        return SkyNight;
    }

    // ─── 태양 & 달 ─────────────────────────────────────────
    private void BuildCelestials()
    {
        var sunGO = new GameObject("Sun");
        _sunSR = sunGO.AddComponent<SpriteRenderer>();
        _sunSR.sprite = MakeCircleSprite(96);
        _sunSR.sortingOrder = SO_Sun;
        sunGO.transform.localScale = new Vector3(1.2f, 1.2f, 1);

        var haloGO = new GameObject("SunHalo");
        haloGO.transform.SetParent(sunGO.transform);
        var haloSR = haloGO.AddComponent<SpriteRenderer>();
        haloSR.sprite       = MakeCircleSprite(96);
        haloSR.color        = new Color(1f, 0.85f, 0.30f, 0.22f);
        haloSR.sortingOrder = SO_Sun - 1;
        haloGO.transform.localScale = new Vector3(2.8f, 2.8f, 1);

        var moonGO = new GameObject("Moon");
        _moonSR = moonGO.AddComponent<SpriteRenderer>();
        _moonSR.sprite = MakeCircleSprite(64);
        _moonSR.color  = new Color(0.88f, 0.92f, 1.00f, 0.90f);
        _moonSR.sortingOrder = SO_Sun;
        moonGO.transform.localScale = new Vector3(0.75f, 0.75f, 1);
    }

    private void UpdateCelestials()
    {
        if (_sunSR == null || _moonSR == null) return;

        float sunAngle = _dayProgress * Mathf.PI * 2f - Mathf.PI * 0.5f;
        float sunX = Mathf.Cos(sunAngle) * 2.0f;
        float sunY = HorizonY + Mathf.Sin(sunAngle) * 4.5f;
        _sunSR.transform.position = new Vector3(sunX, sunY, 0);
        _sunSR.gameObject.SetActive(sunY > HorizonY - 4f);

        if (_sunSR.gameObject.activeSelf)
        {
            float h = Mathf.Clamp01((sunY - HorizonY) / 6.0f);
            _sunSR.color = Color.Lerp(new Color(1f, 0.35f, 0.05f), new Color(1f, 0.95f, 0.55f), h);
            float s = Mathf.Lerp(1.6f, 1.0f, h);
            _sunSR.transform.localScale = new Vector3(s, s, 1);
        }

        float moonAngle = sunAngle + Mathf.PI;
        float moonX = Mathf.Cos(moonAngle) * 2.0f;
        float moonY = HorizonY + Mathf.Sin(moonAngle) * 6.0f;
        _moonSR.transform.position = new Vector3(moonX, moonY, 0);
        _moonSR.gameObject.SetActive(moonY > HorizonY - 4f);
    }

    // ─── 별 ────────────────────────────────────────────────
    private void BuildStars()
    {
        var rng = new System.Random(42);
        var sp  = MakeCircleSprite(12);
        _starsSR = new SpriteRenderer[55];
        for (int i = 0; i < _starsSR.Length; i++)
        {
            var go = new GameObject("Star_" + i);
            _starsSR[i] = go.AddComponent<SpriteRenderer>();
            _starsSR[i].sprite = sp;
            _starsSR[i].sortingOrder = SO_Stars;
            float sc = (float)(rng.NextDouble() * 0.07 + 0.03);
            go.transform.localScale = new Vector3(sc, sc, 1);
            go.transform.position   = new Vector3(
                (float)(rng.NextDouble() * 5.5 - 2.75),
                (float)(rng.NextDouble() * 6.5 + HorizonY + 0.5f), 0);
        }
    }

    private void UpdateStars()
    {
        if (_starsSR == null) return;
        float n;
        if      (_dayProgress < 0.22f) n = 1f;
        else if (_dayProgress < 0.30f) n = 1f - (_dayProgress - 0.22f) / 0.08f;
        else if (_dayProgress < 0.70f) n = 0f;
        else if (_dayProgress < 0.78f) n = (_dayProgress - 0.70f) / 0.08f;
        else                           n = 1f;
        foreach (var sr in _starsSR)
            if (sr != null) sr.color = new Color(1f, 1f, 1f, n * 0.85f);
    }

    // ─── 나무 ──────────────────────────────────────────────
    private void BuildTrees()
    {
        // 세로 모바일 기준: OrthoSize=5 → 가로 ±2.8 유닛 표시
        float[] tx  = { -2.7f, -2.1f, -1.5f, -0.9f,  1.0f,  1.6f,  2.2f,  2.8f };
        float[] tsc = {  1.10f, 1.45f, 0.90f, 1.25f,  1.20f, 0.88f, 1.40f, 1.05f };
        int[]   tor = { SO_TreeBG, SO_TreeFG, SO_TreeBG, SO_TreeFG,
                         SO_TreeFG, SO_TreeBG, SO_TreeFG, SO_TreeBG };

        _trees       = new Transform[tx.Length];
        _swayOffsets = new float[tx.Length];
        var rng = new System.Random(13);

        for (int i = 0; i < tx.Length; i++)
        {
            _swayOffsets[i] = (float)(rng.NextDouble() * Mathf.PI * 2);
            var root = new GameObject("Tree_" + i);
            root.transform.position = new Vector3(tx[i], HorizonY, 0);
            _trees[i] = root.transform;
            BuildTreeMesh(root.transform, tsc[i], tor[i]);
        }
    }

    private void BuildTreeMesh(Transform root, float sc, int baseOrder)
    {
        float trunkH = 1.15f * sc, trunkW = 0.18f * sc;

        var trunkGO = new GameObject("Trunk");
        trunkGO.transform.SetParent(root);
        trunkGO.transform.localPosition = new Vector3(0, trunkH * 0.5f, 0);
        trunkGO.transform.localScale    = new Vector3(trunkW, trunkH, 1);
        var trunkSR = trunkGO.AddComponent<SpriteRenderer>();
        trunkSR.sprite = MakeSquareSprite();
        trunkSR.color  = new Color(0.16f, 0.09f, 0.04f);
        trunkSR.sortingOrder = baseOrder;

        for (int i = 0; i < 3; i++)
        {
            float baseY = trunkH * 0.55f + i * 0.72f * sc;
            var leaf = new GameObject("Leaf" + i);
            leaf.transform.SetParent(root);
            leaf.transform.localPosition = new Vector3(0, baseY, 0);
            leaf.transform.localScale    = new Vector3((1.35f - i * 0.30f) * sc, 0.92f * sc, 1);
            var sr = leaf.AddComponent<SpriteRenderer>();
            sr.sprite = MakeUpTriangleSprite(32);
            sr.color  = new Color(0.04f + i * 0.015f, 0.17f + i * 0.03f, 0.04f);
            sr.sortingOrder = baseOrder + 1 + i;
        }
    }

    private void SwayTrees()
    {
        if (_trees == null) return;
        for (int i = 0; i < _trees.Length; i++)
        {
            if (_trees[i] == null) continue;
            float angle = Mathf.Sin(Time.time * 0.72f + _swayOffsets[i]) * 2.8f;
            _trees[i].localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // ─── UI ────────────────────────────────────────────────
    private void BuildUI(Transform canvas)
    {
        TextMeshProUGUI dummy;
        _titleFloat = CreateExtrudedFloatGroup(canvas, "나만의 스토리", 96,
            new Vector2(0.10f, 0.62f), new Vector2(0.90f, 0.78f),
            new Color(1.0f, 0.92f, 0.42f), new Color(0.55f, 0.22f, 0.04f), 6, 3f, out dummy);

        _subFloat = CreateExtrudedFloatGroup(canvas, "AI가 만드는 나만의 RPG", 36,
            new Vector2(0.10f, 0.56f), new Vector2(0.90f, 0.63f),
            new Color(1.0f, 0.82f, 0.32f), new Color(0.48f, 0.18f, 0.03f), 4, 2f, out dummy);

        var tapArea = new GameObject("TapArea");
        tapArea.transform.SetParent(canvas, false);
        tapArea.AddComponent<Image>().color = Color.clear;
        UIHelper.Stretch(tapArea.GetComponent<RectTransform>());
        tapArea.AddComponent<Button>()
               .onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        var ver = UIHelper.CreateText(canvas, "v0.1  ·  Vibe Coding Project", 22,
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.07f));
        ver.color = new Color(0.70f, 0.50f, 0.30f, 0.70f);
        SetTMPStyle(ver, 0.15f, 0.12f, new Color32(40, 10, 2, 200));

        _tapFloat = CreateExtrudedFloatGroup(canvas, "▼  탭하여 시작  ▼", 34,
            new Vector2(0.10f, 0.05f), new Vector2(0.90f, 0.14f),
            new Color(1.0f, 0.82f, 0.20f), new Color(0.48f, 0.18f, 0.03f), 3, 2f, out _tapText);
        StartCoroutine(BlinkTapText());
    }

    // 입체 압출 레이어 + 부유 컨테이너 생성
    private static RectTransform CreateExtrudedFloatGroup(
        Transform canvas, string text, float size,
        Vector2 aMin, Vector2 aMax,
        Color faceColor, Color sideColor,
        int depth, float stepPx,
        out TextMeshProUGUI mainTMP)
    {
        var go = new GameObject("Float_" + text);
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        UIHelper.SetAnchors(rt, aMin, aMax);

        // 뒤쪽 레이어부터 앞쪽으로 (압출 효과)
        for (int i = depth; i >= 1; i--)
        {
            float blend = (depth > 1) ? (float)(depth - i) / (depth - 1) : 1f;
            Color c = Color.Lerp(sideColor * 0.25f, sideColor, blend);
            var layer = UIHelper.CreateText(go.transform, text, size, Vector2.zero, Vector2.one);
            layer.color = c;
            layer.GetComponent<RectTransform>().anchoredPosition = new Vector2(stepPx * i, -stepPx * i);
            SetTMPStyle(layer, 0.20f, 0f, new Color32(0, 0, 0, 0));
        }

        // 전면 텍스트
        var main = UIHelper.CreateText(go.transform, text, size, Vector2.zero, Vector2.one);
        main.color = faceColor;
        SetTMPStyle(main, 0.30f, 0.18f, new Color32(140, 20, 4, 255));
        mainTMP = main;
        return rt;
    }

    private static void SetTMPStyle(TextMeshProUGUI tmp, float dilate, float outlineW, Color32 outlineColor)
    {
        var mat = tmp.fontMaterial;
        mat.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
        if (outlineW > 0f)
        {
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineW);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        }
    }

    // ─── 몬스터 바디 스프라이트 (세로로 늘린 반원 블롭) ──────
    private static Sprite MakeMonsterBodySprite()
    {
        const int W = 32, H = 48;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float cx = W * 0.5f, rx = W * 0.5f, ry = H;
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                float dx = (x - cx) / rx;
                float dy = (float)y / ry;   // y=0 평평한 바닥, y=H 둥근 꼭대기
                tex.SetPixel(x, y, dx*dx + dy*dy <= 1f ? Color.white : Color.clear);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,W,H), new Vector2(0.5f, 0f), H);
    }

    // ─── 스프라이트 팩토리 ─────────────────────────────────
    private static Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    private static Sprite MakeCircleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float h = res * 0.5f;
        for (int x = 0; x < res; x++)
            for (int y = 0; y < res; y++)
            {
                float dx = (x - h) / h, dy = (y - h) / h;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Max(0f, d - 0.82f) / 0.18f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    // 위가 넓고 아래가 뾰족한 삼각형 (망토용), 피벗=상단중앙
    private static Sprite MakeDownTriangleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = res * 0.5f;
        for (int px = 0; px < res; px++)
            for (int py = 0; py < res; py++)
            {
                // py=0 바닥(뾰족), py=res-1 위(넓음)
                float halfW = half * (float)py / (res - 1);
                tex.SetPixel(px, py, Mathf.Abs(px - half) < halfW ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 1f), res);
    }

    private static Sprite MakeUpTriangleSprite(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = res * 0.5f;
        for (int px = 0; px < res; px++)
            for (int py = 0; py < res; py++)
            {
                float halfW = half * (float)(res - py) / res;
                tex.SetPixel(px, py, Mathf.Abs(px - half) < halfW ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0f), res);
    }

    // ─── 코루틴 ────────────────────────────────────────────
    private IEnumerator BlinkTapText()
    {
        while (true)
        {
            yield return Fade(_tapText, 0.9f, 0.15f, 0.8f);
            yield return Fade(_tapText, 0.15f, 0.9f, 0.8f);
        }
    }

    private IEnumerator Fade(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        float t = 0;
        var c = tmp.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            tmp.color = c;
            yield return null;
        }
        c.a = to;
        tmp.color = c;
    }
}
