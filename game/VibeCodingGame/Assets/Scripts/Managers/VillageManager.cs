using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 마을 씬 매니저 — 캐릭터 선택 후 진입하는 메인 허브
// 상점/미션/우편/캐릭터정보 팝업 + 던전 입장 버튼 제공
public class VillageManager : MonoBehaviour
{
    private CharacterData _ch;
    private Transform _canvasTF;
    private GameObject _shopPanel, _missionPanel, _mailPanel, _infoPanel;

    private void Start()
    {
        _ch = GameState.CurrentCharacter;
        SetupCamera();
        SetupWorld();
        SetupUI();
    }

    // ── 카메라 ──────────────────────────────────────────────────
    private void SetupCamera()
    {
        if (Camera.main != null) return;
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.04f, 0.07f, 0.14f);
    }

    // ── 월드 (배경 + 건물 + 캐릭터) ─────────────────────────────
    private void SetupWorld()
    {
        var sq = MakeSquare();

        // 지면
        Decor(sq, new Vector3(0f, -3.2f, 1f), new Vector3(40f, 5f, 1f),
              new Color(0.10f, 0.20f, 0.09f), -1);

        // 건물 몸체
        Decor(sq, new Vector3(-4.6f, -1.4f, 0.5f), new Vector3(2.2f, 3.0f, 1f),
              new Color(0.32f, 0.20f, 0.07f), 0);
        Decor(sq, new Vector3( 4.6f, -1.3f, 0.5f), new Vector3(2.0f, 3.5f, 1f),
              new Color(0.18f, 0.25f, 0.42f), 0);
        Decor(sq, new Vector3(-7.8f, -2.0f, 0.5f), new Vector3(1.8f, 2.2f, 1f),
              new Color(0.28f, 0.16f, 0.05f), 0);
        Decor(sq, new Vector3( 7.8f, -1.8f, 0.5f), new Vector3(2.0f, 2.8f, 1f),
              new Color(0.14f, 0.30f, 0.16f), 0);

        // 지붕
        Decor(sq, new Vector3(-4.6f,  0.0f, 0.4f), new Vector3(2.6f, 0.8f, 1f),
              new Color(0.20f, 0.10f, 0.04f), 1);
        Decor(sq, new Vector3( 4.6f,  0.1f, 0.4f), new Vector3(2.4f, 0.9f, 1f),
              new Color(0.10f, 0.14f, 0.28f), 1);

        // 창문 (작은 밝은 사각형)
        Decor(sq, new Vector3(-4.6f, -1.2f, 0.3f), new Vector3(0.5f, 0.4f, 1f),
              new Color(0.9f, 0.85f, 0.5f, 0.7f), 2);
        Decor(sq, new Vector3( 4.6f, -1.0f, 0.3f), new Vector3(0.5f, 0.4f, 1f),
              new Color(0.9f, 0.85f, 0.5f, 0.7f), 2);

        // 캐릭터
        int idx = _ch.generated.characterIndex;
        if (idx < 1 || idx > 8) idx = 1;
        var heroGO = new GameObject("VillageHero");
        heroGO.transform.position = new Vector3(0f, -1.5f, 0f);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(heroGO.transform, false);
        visual.transform.localScale = Vector3.one * 2f;
        LayerLabCharacter.AttachPlayer(visual, idx).SetWalking(true);
    }

    private void Decor(Sprite sq, Vector3 pos, Vector3 scale, Color color, int order)
    {
        var go = new GameObject("D");
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sq;
        sr.color        = color;
        sr.sortingOrder = order;
    }

    private Sprite MakeSquare()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    // ── UI 진입 ─────────────────────────────────────────────────
    private void SetupUI()
    {
        UIHelper.CreateCanvas(out _);
        _canvasTF = FindObjectOfType<Canvas>().transform;

        BuildTopBar();
        BuildQuestBanner();
        BuildSideIcons();
        BuildVillageName();
        BuildFeatureButtons();
        BuildBottomBar();

        // 팝업 미리 생성 (비활성 상태)
        _shopPanel    = BuildShopPopup();
        _missionPanel = BuildMissionPopup();
        _mailPanel    = BuildMailPopup();
        _infoPanel    = BuildInfoPopup();
    }

    // ── 상단 HUD ────────────────────────────────────────────────
    private void BuildTopBar()
    {
        var t = _canvasTF;

        var bg = UIHelper.CreatePanel(t, new Color(0.04f, 0.03f, 0.10f, 0.94f), "TopBar");
        UIHelper.SetAnchors(bg.GetComponent<RectTransform>(), new Vector2(0f, 0.928f), Vector2.one);

        // 레벨 배지
        var lv = UIHelper.CreatePanel(t, new Color(0.20f, 0.38f, 0.85f), "LvBadge");
        UIHelper.SetAnchors(lv.GetComponent<RectTransform>(),
            new Vector2(0.01f, 0.935f), new Vector2(0.12f, 0.994f));
        UIHelper.CreateText(lv.transform, $"Lv.{CalcLevel()}", 24, Vector2.zero, Vector2.one);

        // HP 바
        var hpBG = UIHelper.CreatePanel(t, new Color(0.28f, 0.05f, 0.05f), "HPBG");
        UIHelper.SetAnchors(hpBG.GetComponent<RectTransform>(),
            new Vector2(0.14f, 0.950f), new Vector2(0.55f, 0.990f));
        var hpFill = UIHelper.CreatePanel(hpBG.transform, new Color(0.85f, 0.14f, 0.14f), "HPFill");
        UIHelper.SetAnchors(hpFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var hpTxt = UIHelper.CreateText(hpBG.transform, $"HP  {_ch.generated.stats.hp}",
            20, new Vector2(0.03f, 0f), Vector2.one, TextAlignmentOptions.Left);
        hpTxt.color = new Color(1f, 0.8f, 0.8f);

        // ATK
        var atkBG = UIHelper.CreatePanel(t, new Color(0.55f, 0.26f, 0.04f), "AtkBG");
        UIHelper.SetAnchors(atkBG.GetComponent<RectTransform>(),
            new Vector2(0.57f, 0.950f), new Vector2(0.73f, 0.990f));
        UIHelper.CreateText(atkBG.transform, $"ATK  {_ch.generated.stats.atk}", 20,
            Vector2.zero, Vector2.one).color = new Color(1f, 0.85f, 0.5f);

        // MP 바
        var mpBG = UIHelper.CreatePanel(t, new Color(0.05f, 0.07f, 0.28f), "MPBG");
        UIHelper.SetAnchors(mpBG.GetComponent<RectTransform>(),
            new Vector2(0.75f, 0.950f), new Vector2(0.99f, 0.990f));
        var mpFill = UIHelper.CreatePanel(mpBG.transform, new Color(0.14f, 0.32f, 0.85f), "MPFill");
        UIHelper.SetAnchors(mpFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var mpTxt = UIHelper.CreateText(mpBG.transform, $"MP  {_ch.generated.stats.mp}",
            20, new Vector2(0.03f, 0f), Vector2.one, TextAlignmentOptions.Left);
        mpTxt.color = new Color(0.7f, 0.8f, 1f);

        // 구분선
        var line = UIHelper.CreatePanel(t, new Color(0.5f, 0.28f, 0.9f, 0.55f), "TopLine");
        UIHelper.SetAnchors(line.GetComponent<RectTransform>(),
            new Vector2(0f, 0.926f), new Vector2(1f, 0.929f));
    }

    private int CalcLevel()
    {
        var s = _ch.generated.stats;
        return Mathf.Max(1, (s.hp / 20 + s.atk / 5 + s.def / 4 + s.mp / 20) / 4);
    }

    // ── 퀘스트 배너 + 설정 ──────────────────────────────────────
    private void BuildQuestBanner()
    {
        var t = _canvasTF;

        var bg = UIHelper.CreatePanel(t, new Color(0.08f, 0.06f, 0.18f, 0.88f), "QuestBanner");
        UIHelper.SetAnchors(bg.GetComponent<RectTransform>(),
            new Vector2(0.11f, 0.879f), new Vector2(0.87f, 0.924f));

        string quest = (_ch.generated.abilities != null && _ch.generated.abilities.Count > 0)
            ? "현재 임무: " + _ch.generated.abilities[0].name
            : "마을에서 휴식을 취하세요";
        var txt = UIHelper.CreateText(bg.transform, quest, 22,
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), TextAlignmentOptions.Left);
        txt.color = new Color(1f, 0.88f, 0.50f);

        var settingsBtn = UIHelper.CreateButton(t, "⚙",
            new Vector2(0.878f, 0.881f), new Vector2(0.994f, 0.922f),
            new Color(0.18f, 0.14f, 0.30f));
        settingsBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }

    // ── 좌우 아이콘 버튼 ────────────────────────────────────────
    private void BuildSideIcons()
    {
        var t = _canvasTF;

        // 좌측
        MakeSideIcon(t, "선물", new Vector2(0.01f, 0.770f), new Vector2(0.12f, 0.870f),
            new Color(0.12f, 0.09f, 0.24f), () => TogglePanel(_shopPanel));
        MakeSideIcon(t, "미션", new Vector2(0.01f, 0.640f), new Vector2(0.12f, 0.740f),
            new Color(0.12f, 0.09f, 0.24f), () => TogglePanel(_missionPanel));

        // 우측 — 우편
        MakeSideIcon(t, "우편", new Vector2(0.88f, 0.770f), new Vector2(0.99f, 0.870f),
            new Color(0.12f, 0.09f, 0.24f), () => TogglePanel(_mailPanel));

        // 알림 배지 (빨간 점)
        var badge = UIHelper.CreatePanel(t, new Color(0.88f, 0.14f, 0.14f), "MailBadge");
        UIHelper.SetAnchors(badge.GetComponent<RectTransform>(),
            new Vector2(0.944f, 0.854f), new Vector2(0.992f, 0.878f));
        UIHelper.CreateText(badge.transform, "1", 14, Vector2.zero, Vector2.one);

        // 우측 — 캐릭터 정보
        MakeSideIcon(t, "정보", new Vector2(0.88f, 0.640f), new Vector2(0.99f, 0.740f),
            new Color(0.10f, 0.18f, 0.28f), () => TogglePanel(_infoPanel));
    }

    private void MakeSideIcon(Transform parent, string label, Vector2 min, Vector2 max,
                               Color color, System.Action onClick)
    {
        var btn = UIHelper.CreateButton(parent, label, min, max, color);
        btn.onClick.AddListener(() => onClick());
        var lbl = UIHelper.CreateText(parent, label, 17,
            new Vector2(min.x, min.y - 0.034f), new Vector2(max.x, min.y));
        lbl.color = new Color(0.78f, 0.78f, 1f, 0.9f);
    }

    // ── 마을 이름 ────────────────────────────────────────────────
    private void BuildVillageName()
    {
        var t = _canvasTF;

        var title = UIHelper.CreateText(t, $"{_ch.generated.name}의 마을", 46,
            new Vector2(0.05f, 0.640f), new Vector2(0.95f, 0.740f));
        title.color      = new Color(1f, 0.92f, 0.55f);
        title.fontStyle  = FontStyles.Bold;
        var ol = title.gameObject.AddComponent<Outline>();
        ol.effectColor    = new Color(0.35f, 0.08f, 0.0f, 0.85f);
        ol.effectDistance = new Vector2(2, -2);

        var sub = UIHelper.CreateText(t, "— 안전 구역 —", 24,
            new Vector2(0.15f, 0.607f), new Vector2(0.85f, 0.638f));
        sub.color = new Color(0.65f, 0.85f, 0.65f, 0.85f);

        // 장식 라인
        var lineL = UIHelper.CreatePanel(t, new Color(0.6f, 0.45f, 0.10f, 0.45f), "LineL");
        UIHelper.SetAnchors(lineL.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.649f), new Vector2(0.28f, 0.653f));
        var lineR = UIHelper.CreatePanel(t, new Color(0.6f, 0.45f, 0.10f, 0.45f), "LineR");
        UIHelper.SetAnchors(lineR.GetComponent<RectTransform>(),
            new Vector2(0.72f, 0.649f), new Vector2(0.95f, 0.653f));
    }

    // ── 기능 버튼 3개 (상점 / 던전 / 선술집) ────────────────────
    private void BuildFeatureButtons()
    {
        var t = _canvasTF;

        var bar = UIHelper.CreatePanel(t, new Color(0.04f, 0.03f, 0.10f, 0.80f), "FeatureBar");
        UIHelper.SetAnchors(bar.GetComponent<RectTransform>(),
            new Vector2(0f, 0.265f), new Vector2(1f, 0.382f));
        var topLine = UIHelper.CreatePanel(t, new Color(0.5f, 0.28f, 0.9f, 0.5f), "FeatureTopLine");
        UIHelper.SetAnchors(topLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.380f), new Vector2(1f, 0.383f));

        var labels  = new[] { "상점",  "던전",   "선술집" };
        var colors  = new[] {
            new Color(0.50f, 0.36f, 0.04f),
            new Color(0.58f, 0.08f, 0.08f),
            new Color(0.10f, 0.32f, 0.16f),
        };
        float[] xs = { 0.02f, 0.35f, 0.68f };
        float[] xe = { 0.33f, 0.66f, 0.98f };

        for (int i = 0; i < labels.Length; i++)
        {
            int captured = i;
            var btn = UIHelper.CreateButton(t, labels[i],
                new Vector2(xs[i], 0.273f), new Vector2(xe[i], 0.374f), colors[i]);
            btn.onClick.AddListener(() =>
            {
                if (captured == 1) SceneManager.LoadScene("GameScene");
                else if (captured == 0) TogglePanel(_shopPanel);
                else TogglePanel(_missionPanel);
            });
        }
    }

    // ── 하단 바 (버튼 + 캐릭터 정보) ────────────────────────────
    private void BuildBottomBar()
    {
        var t = _canvasTF;

        var bg = UIHelper.CreatePanel(t, new Color(0.04f, 0.03f, 0.10f, 0.94f), "BottomBar");
        UIHelper.SetAnchors(bg.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0.263f));

        var topLine = UIHelper.CreatePanel(t, new Color(0.5f, 0.28f, 0.9f, 0.6f), "BotLine");
        UIHelper.SetAnchors(topLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.261f), new Vector2(1f, 0.264f));

        // 액션 버튼
        var backBtn = UIHelper.CreateButton(t, "← 목록",
            new Vector2(0.02f, 0.188f), new Vector2(0.30f, 0.256f),
            new Color(0.18f, 0.14f, 0.30f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        var dungeonBtn = UIHelper.CreateButton(t, "⚔ 던전 입장!",
            new Vector2(0.32f, 0.183f), new Vector2(0.68f, 0.260f),
            new Color(0.68f, 0.38f, 0.04f));
        dungeonBtn.onClick.AddListener(() => SceneManager.LoadScene("GameScene"));
        var dTmp = dungeonBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (dTmp != null) dTmp.fontSize = 42;

        var expBtn = UIHelper.CreateButton(t, "원정 →",
            new Vector2(0.70f, 0.188f), new Vector2(0.98f, 0.256f),
            new Color(0.10f, 0.30f, 0.42f));
        expBtn.onClick.AddListener(() => TogglePanel(_missionPanel));

        // 캐릭터 이름
        var nameTxt = UIHelper.CreateText(t, _ch.generated.name, 36,
            new Vector2(0.03f, 0.122f), new Vector2(0.97f, 0.182f), TextAlignmentOptions.Left);
        nameTxt.color     = new Color(1f, 0.88f, 0.50f);
        nameTxt.fontStyle = FontStyles.Bold;

        // 스탯 한 줄
        var s = _ch.generated.stats;
        var statsTxt = UIHelper.CreateText(t,
            $"HP {s.hp}   ATK {s.atk}   DEF {s.def}   MP {s.mp}", 22,
            new Vector2(0.02f, 0.070f), new Vector2(0.98f, 0.118f), TextAlignmentOptions.Left);
        statsTxt.color = new Color(0.75f, 0.75f, 0.92f);

        // 특수능력 이름
        if (_ch.generated.abilities != null && _ch.generated.abilities.Count > 0)
        {
            var names = new List<string>();
            for (int i = 0; i < Mathf.Min(3, _ch.generated.abilities.Count); i++)
                names.Add(_ch.generated.abilities[i].name);
            var abilTxt = UIHelper.CreateText(t,
                "특기: " + string.Join(" · ", names), 20,
                new Vector2(0.02f, 0.018f), new Vector2(0.98f, 0.065f), TextAlignmentOptions.Left);
            abilTxt.color = new Color(0.55f, 0.78f, 1.0f);
        }
    }

    // ── 팝업 공통 껍데기 ─────────────────────────────────────────
    private GameObject BuildPopupShell(string title, out Transform panelTF)
    {
        var overlay = new GameObject("Popup_" + title);
        overlay.transform.SetParent(_canvasTF, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
        UIHelper.Stretch(overlay.GetComponent<RectTransform>());

        var panel = UIHelper.CreatePanel(overlay.transform, new Color(0.06f, 0.05f, 0.14f), "Panel");
        UIHelper.SetAnchors(panel.GetComponent<RectTransform>(),
            new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.93f));

        var header = UIHelper.CreatePanel(panel.transform, new Color(0.10f, 0.08f, 0.22f), "Header");
        UIHelper.SetAnchors(header.GetComponent<RectTransform>(),
            new Vector2(0f, 0.875f), new Vector2(1f, 1f));

        var headerLine = UIHelper.CreatePanel(panel.transform, new Color(0.6f, 0.32f, 1.0f, 0.7f), "HLine");
        UIHelper.SetAnchors(headerLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.873f), new Vector2(1f, 0.877f));

        var titleTxt = UIHelper.CreateText(panel.transform, title, 40,
            new Vector2(0.04f, 0.875f), new Vector2(0.78f, 0.998f), TextAlignmentOptions.Left);
        titleTxt.color     = new Color(1f, 0.88f, 0.50f);
        titleTxt.fontStyle = FontStyles.Bold;

        var closeBtn = UIHelper.CreateButton(panel.transform, "✕",
            new Vector2(0.80f, 0.888f), new Vector2(0.97f, 0.988f),
            new Color(0.40f, 0.08f, 0.08f));
        closeBtn.onClick.AddListener(() => overlay.SetActive(false));

        panelTF = panel.transform;
        overlay.SetActive(false);
        return overlay;
    }

    // ── 상점 팝업 ────────────────────────────────────────────────
    private GameObject BuildShopPopup()
    {
        var overlay = BuildPopupShell("상점", out Transform tf);

        var items = new (string name, string price, Color strip)[]
        {
            ("회복 포션",   "500 G",   new Color(0.7f, 0.1f, 0.1f)),
            ("마나 포션",   "300 G",   new Color(0.1f, 0.2f, 0.7f)),
            ("방어구 강화", "1,000 G", new Color(0.3f, 0.3f, 0.1f)),
            ("무기 강화",   "1,500 G", new Color(0.5f, 0.2f, 0.05f)),
        };

        for (int i = 0; i < items.Length; i++)
        {
            float y2 = 0.84f - i * 0.17f;
            float y1 = y2 - 0.14f;
            var it = items[i];

            var row = UIHelper.CreatePanel(tf, new Color(0.10f, 0.09f, 0.20f), $"I{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));

            var strip = UIHelper.CreatePanel(row.transform, it.strip, "Strip");
            UIHelper.SetAnchors(strip.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(0.04f, 1f));

            var nameT = UIHelper.CreateText(row.transform, it.name, 28,
                new Vector2(0.07f, 0.1f), new Vector2(0.62f, 0.9f), TextAlignmentOptions.Left);
            nameT.color = Color.white;

            var priceT = UIHelper.CreateText(row.transform, it.price, 28,
                new Vector2(0.63f, 0.1f), new Vector2(0.97f, 0.9f), TextAlignmentOptions.Right);
            priceT.color = new Color(1f, 0.85f, 0.3f);
        }

        return overlay;
    }

    // ── 미션 팝업 ────────────────────────────────────────────────
    private GameObject BuildMissionPopup()
    {
        var overlay = BuildPopupShell("미션", out Transform tf);
        var abilities = _ch.generated.abilities;

        if (abilities != null && abilities.Count > 0)
        {
            for (int i = 0; i < Mathf.Min(4, abilities.Count); i++)
            {
                float y2 = 0.84f - i * 0.19f;
                float y1 = y2 - 0.16f;
                var ab = abilities[i];

                var row = UIHelper.CreatePanel(tf, new Color(0.08f, 0.12f, 0.10f), $"M{i}");
                UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                    new Vector2(0.03f, y1), new Vector2(0.97f, y2));

                bool done = (i == 0);
                var badge = UIHelper.CreatePanel(row.transform,
                    done ? new Color(0.18f, 0.58f, 0.18f) : new Color(0.48f, 0.28f, 0.04f), "Badge");
                UIHelper.SetAnchors(badge.GetComponent<RectTransform>(),
                    new Vector2(0f, 0.1f), new Vector2(0.20f, 0.9f));
                UIHelper.CreateText(badge.transform, done ? "완료" : "진행중", 20,
                    Vector2.zero, Vector2.one);

                var nameT = UIHelper.CreateText(row.transform, ab.name, 26,
                    new Vector2(0.22f, 0.50f), new Vector2(0.97f, 0.92f), TextAlignmentOptions.Left);
                nameT.color = Color.white;

                var descT = UIHelper.CreateText(row.transform, ab.description, 20,
                    new Vector2(0.22f, 0.05f), new Vector2(0.97f, 0.50f), TextAlignmentOptions.Left);
                descT.color = new Color(0.70f, 0.70f, 0.88f);
            }
        }
        else
        {
            UIHelper.CreateText(tf, "임무가 없습니다.", 30,
                new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.58f))
                .color = new Color(0.6f, 0.6f, 0.8f);
        }

        return overlay;
    }

    // ── 우편함 팝업 ──────────────────────────────────────────────
    private GameObject BuildMailPopup()
    {
        var overlay = BuildPopupShell("우편함", out Transform tf);

        // 캐릭터 스토리 메일
        var mail1 = UIHelper.CreatePanel(tf, new Color(0.10f, 0.08f, 0.20f), "Mail1");
        UIHelper.SetAnchors(mail1.GetComponent<RectTransform>(),
            new Vector2(0.03f, 0.62f), new Vector2(0.97f, 0.84f));

        var sender = UIHelper.CreateText(mail1.transform, "📜 " + _ch.generated.name + "의 이야기", 24,
            new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.96f), TextAlignmentOptions.Left);
        sender.color = new Color(1f, 0.88f, 0.5f);

        string story = _ch.generated.story ?? "이 모험가의 이야기는 아직 쓰여지지 않았습니다.";
        if (story.Length > 90) story = story.Substring(0, 90) + "...";
        var storyT = UIHelper.CreateText(mail1.transform, story, 20,
            new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.57f), TextAlignmentOptions.Left);
        storyT.color = new Color(0.80f, 0.80f, 0.92f);

        // 탐험 지역 메일
        if (_ch.generated.locations != null && _ch.generated.locations.Count > 0)
        {
            var mail2 = UIHelper.CreatePanel(tf, new Color(0.08f, 0.12f, 0.22f), "Mail2");
            UIHelper.SetAnchors(mail2.GetComponent<RectTransform>(),
                new Vector2(0.03f, 0.40f), new Vector2(0.97f, 0.60f));

            UIHelper.CreateText(mail2.transform, "🗺 탐험 지역 안내", 24,
                new Vector2(0.03f, 0.55f), new Vector2(0.97f, 0.95f), TextAlignmentOptions.Left)
                .color = new Color(0.5f, 0.8f, 1.0f);

            var loc = _ch.generated.locations[0];
            string locDesc = loc.name;
            if (!string.IsNullOrEmpty(loc.description))
                locDesc += " — " + loc.description.Substring(0, Mathf.Min(40, loc.description.Length));
            UIHelper.CreateText(mail2.transform, locDesc, 20,
                new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.54f), TextAlignmentOptions.Left)
                .color = new Color(0.75f, 0.85f, 0.95f);
        }

        UIHelper.CreateText(tf, "그 외 새 편지 없음", 22,
            new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.39f))
            .color = new Color(0.45f, 0.45f, 0.58f);

        return overlay;
    }

    // ── 캐릭터 정보 팝업 ─────────────────────────────────────────
    private GameObject BuildInfoPopup()
    {
        var overlay = BuildPopupShell(_ch.generated.name, out Transform tf);
        var s = _ch.generated.stats;

        var statData = new (string label, int val, Color col)[]
        {
            ("❤ HP",  s.hp,  new Color(0.9f, 0.2f, 0.2f)),
            ("⚔ ATK", s.atk, new Color(0.9f, 0.5f, 0.1f)),
            ("🛡 DEF", s.def, new Color(0.3f, 0.6f, 0.9f)),
            ("✦ MP",  s.mp,  new Color(0.5f, 0.3f, 0.9f)),
        };

        for (int i = 0; i < statData.Length; i++)
        {
            float y2 = 0.84f - i * 0.12f;
            float y1 = y2 - 0.10f;
            var sd = statData[i];

            var row = UIHelper.CreatePanel(tf, new Color(0.10f, 0.09f, 0.20f), $"S{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));

            var lbl = UIHelper.CreateText(row.transform, sd.label, 26,
                new Vector2(0.03f, 0.1f), new Vector2(0.55f, 0.9f), TextAlignmentOptions.Left);
            lbl.color = sd.col;

            var val = UIHelper.CreateText(row.transform, sd.val.ToString(), 28,
                new Vector2(0.55f, 0.1f), new Vector2(0.97f, 0.9f), TextAlignmentOptions.Right);
            val.color     = Color.white;
            val.fontStyle = FontStyles.Bold;
        }

        // 스토리 요약
        if (!string.IsNullOrEmpty(_ch.generated.story))
        {
            string brief = _ch.generated.story;
            if (brief.Length > 65) brief = brief.Substring(0, 65) + "...";
            UIHelper.CreateText(tf, brief, 20,
                new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.32f), TextAlignmentOptions.Left)
                .color = new Color(0.72f, 0.72f, 0.88f);
        }

        return overlay;
    }

    // ── 팝업 토글 ────────────────────────────────────────────────
    private void TogglePanel(GameObject panel)
    {
        bool wasActive = panel.activeSelf;
        _shopPanel.SetActive(false);
        _missionPanel.SetActive(false);
        _mailPanel.SetActive(false);
        _infoPanel.SetActive(false);
        if (!wasActive) panel.SetActive(true);
    }
}
