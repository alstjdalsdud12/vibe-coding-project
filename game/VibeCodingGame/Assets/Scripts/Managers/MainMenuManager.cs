using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    private static readonly Color[] SlotColors =
    {
        new Color(0.30f, 0.55f, 0.95f),
        new Color(0.70f, 0.25f, 0.85f),
        new Color(0.20f, 0.75f, 0.45f),
        new Color(0.90f, 0.65f, 0.15f),
        new Color(0.85f, 0.28f, 0.28f),
    };

    private ApiClient   _api;
    private Transform   _canvas;
    private Transform   _listContent;
    private TextMeshProUGUI _statusText;
    private int         _cardIndex;

    // Texture2D로 캐시 — ReadPixels로 RT 내용을 복사해 안정적으로 표시
    private readonly Dictionary<int, Texture2D> _previewCache = new Dictionary<int, Texture2D>();
    private int _previewSlot;

    private void Start()
    {
        _api = gameObject.AddComponent<ApiClient>();
        UIHelper.CreateCanvas(out _);
        _canvas = FindObjectOfType<Canvas>().transform;
        BuildUI();
        LoadCharacterList();
    }

    private void OnDestroy()
    {
        foreach (var tex in _previewCache.Values)
            if (tex != null) Destroy(tex);
        _previewCache.Clear();
    }

    // ── UI 구성 ──────────────────────────────────────────────
    private void BuildUI()
    {
        var bg = UIHelper.CreatePanel(_canvas, new Color(0.06f, 0.05f, 0.12f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        var header = UIHelper.CreatePanel(_canvas, new Color(0.10f, 0.07f, 0.22f), "Header");
        UIHelper.SetAnchors(header.GetComponent<RectTransform>(),
            new Vector2(0f, 0.80f), new Vector2(1f, 1f));

        var headerLine = UIHelper.CreatePanel(_canvas, new Color(0.6f, 0.35f, 1.0f, 0.7f), "HeaderLine");
        UIHelper.SetAnchors(headerLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.798f), new Vector2(1f, 0.803f));

        var shadow = UIHelper.CreateText(_canvas, "캐릭터  선택", 68,
            new Vector2(0.102f, 0.855f), new Vector2(0.902f, 0.978f));
        shadow.color = new Color(0.08f, 0f, 0.2f, 0.8f);

        var title = UIHelper.CreateText(_canvas, "캐릭터  선택", 68,
            new Vector2(0.10f, 0.858f), new Vector2(0.90f, 0.980f));
        title.color = new Color(1f, 0.88f, 0.50f);
        var ol = title.gameObject.AddComponent<Outline>();
        ol.effectColor    = new Color(0.7f, 0.2f, 0.05f, 1f);
        ol.effectDistance = new Vector2(3, -3);

        var sub = UIHelper.CreateText(_canvas, "어떤 모험가와 함께하시겠습니까?", 26,
            new Vector2(0.08f, 0.808f), new Vector2(0.92f, 0.856f));
        sub.color = new Color(0.75f, 0.65f, 1.0f, 0.9f);

        _statusText = UIHelper.CreateText(_canvas, "불러오는 중...", 26,
            new Vector2(0.05f, 0.758f), new Vector2(0.95f, 0.796f));
        _statusText.color = new Color(0.7f, 0.6f, 0.9f);

        // ── 카드 스크롤 ──────────────────────────────────────
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(_canvas, false);
        UIHelper.SetAnchors(scrollGO.AddComponent<RectTransform>(),
            new Vector2(0f, 0.22f), new Vector2(1f, 0.758f));
        scrollGO.AddComponent<Image>().color = Color.clear;

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.vertical          = false;
        scroll.horizontal        = true;
        scroll.scrollSensitivity = 40f;
        scroll.decelerationRate  = 0.12f;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        UIHelper.Stretch(vpRT);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = vpRT;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0);
        cRT.anchorMax = new Vector2(0, 1);
        cRT.pivot     = new Vector2(0, 0.5f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;

        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 36;
        hlg.padding              = new RectOffset(60, 60, 24, 24);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        content.AddComponent<ContentSizeFitter>().horizontalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = cRT;
        _listContent   = content.transform;

        var footerLine = UIHelper.CreatePanel(_canvas, new Color(0.6f, 0.35f, 1.0f, 0.7f), "FooterLine");
        UIHelper.SetAnchors(footerLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.218f), new Vector2(1f, 0.222f));

        var newBtn = UIHelper.CreateButton(_canvas, "+ 새 캐릭터 만들기",
            new Vector2(0.08f, 0.115f), new Vector2(0.92f, 0.205f),
            new Color(0.15f, 0.48f, 0.28f));
        newBtn.onClick.AddListener(() => SceneManager.LoadScene("CharacterCreationScene"));

        var backBtn = UIHelper.CreateButton(_canvas, "타이틀로 돌아가기",
            new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.105f),
            new Color(0.18f, 0.14f, 0.30f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
    }

    // ── 캐릭터 목록 로드 ─────────────────────────────────────
    private void LoadCharacterList()
    {
        StartCoroutine(_api.GetAllCharacters(
            items =>
            {
                _cardIndex = 0;
                _statusText.text = items.Length == 0
                    ? "저장된 캐릭터가 없습니다."
                    : "";
                foreach (var item in items)
                    AddCharacterCard(item);
            },
            err => _statusText.text = "오류: " + err));
    }

    // ── 캐릭터 카드 ──────────────────────────────────────────
    private void AddCharacterCard(CharacterListItem item)
    {
        Color accent = SlotColors[_cardIndex % SlotColors.Length];
        Color accentDark = new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f);
        Color accentBright = new Color(
            Mathf.Min(accent.r + 0.35f, 1f),
            Mathf.Min(accent.g + 0.35f, 1f),
            Mathf.Min(accent.b + 0.35f, 1f));
        _cardIndex++;

        var card = new GameObject("Card_" + item.name);
        card.transform.SetParent(_listContent, false);
        card.AddComponent<Image>().color = new Color(0.09f, 0.08f, 0.18f);
        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth = le.minWidth = 370;

        var btn = card.AddComponent<Button>();
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.16f, 0.14f, 0.30f);
        btnColors.pressedColor     = new Color(0.06f, 0.05f, 0.12f);
        btn.colors = btnColors;
        string capturedId = item.id;
        btn.onClick.AddListener(() => OnCharacterSelected(capturedId));

        // ── 상단 초상화 영역 ─────────────────────
        var portrait = new GameObject("Portrait");
        portrait.transform.SetParent(card.transform, false);
        portrait.AddComponent<Image>().color = accentDark;
        UIHelper.SetAnchors(portrait.GetComponent<RectTransform>(),
            new Vector2(0f, 0.46f), new Vector2(1f, 1f));

        // 캐릭터 프리뷰 RawImage
        var previewGO = new GameObject("CharacterPreview");
        previewGO.transform.SetParent(portrait.transform, false);
        var rawImg = previewGO.AddComponent<RawImage>();
        UIHelper.SetAnchors(previewGO.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.98f));
        int charIdx = item.characterIndex > 0 ? item.characterIndex : 1;
        StartCoroutine(CapturePreviewAsync(charIdx, rawImg));

        // 초상화 하단 accent 라인
        var portraitBottom = new GameObject("PortraitBottom");
        portraitBottom.transform.SetParent(portrait.transform, false);
        portraitBottom.AddComponent<Image>().color = accent;
        UIHelper.SetAnchors(portraitBottom.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0.05f));

        // 슬롯 배지 (우상단)
        var badge = new GameObject("Badge");
        badge.transform.SetParent(portrait.transform, false);
        badge.AddComponent<Image>().color = accent;
        UIHelper.SetAnchors(badge.GetComponent<RectTransform>(),
            new Vector2(0.72f, 0.78f), new Vector2(1f, 1f));
        UIHelper.CreateText(badge.transform, $"No.{_cardIndex}", 24,
            Vector2.zero, Vector2.one);

        // ── 하단 정보 영역 ───────────────────────
        var nameTmp = UIHelper.CreateText(card.transform, item.name, 36,
            new Vector2(0.07f, 0.32f), new Vector2(0.93f, 0.46f));
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color     = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.enableWordWrapping = false;

        var line = new GameObject("Divider");
        line.transform.SetParent(card.transform, false);
        line.AddComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.4f);
        UIHelper.SetAnchors(line.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.315f), new Vector2(0.95f, 0.32f));

        var weaponTmp = UIHelper.CreateText(card.transform,
            "무기  " + item.weapon, 30,
            new Vector2(0.07f, 0.19f), new Vector2(0.93f, 0.31f));
        weaponTmp.color     = accentBright;
        weaponTmp.alignment = TextAlignmentOptions.Left;
        weaponTmp.enableWordWrapping = true;

        var conceptTmp = UIHelper.CreateText(card.transform, item.concept, 28,
            new Vector2(0.07f, 0.04f), new Vector2(0.93f, 0.19f));
        conceptTmp.color     = new Color(0.72f, 0.72f, 0.90f);
        conceptTmp.alignment = TextAlignmentOptions.Left;
        conceptTmp.enableWordWrapping = true;
    }

    // ── 캐릭터 프리뷰 캡처 ───────────────────────────────────
    // 캐릭터를 화면 밖에 생성 → 전용 카메라로 RT 렌더 → ReadPixels로 Texture2D 복사
    // Texture2D로 표시하면 RT 타이밍 의존 없이 안정적
    private IEnumerator CapturePreviewAsync(int charIdx, RawImage target)
    {
        if (_previewCache.TryGetValue(charIdx, out var cached))
        {
            target.texture = cached;
            yield break;
        }

        float slotX = 900f + _previewSlot * 10f;
        _previewSlot++;

        // 캐릭터 (화면 밖 위치)
        var root = new GameObject($"Prev_{charIdx}");
        root.transform.position = new Vector3(slotX, 900f, 0f);
        var visual = new GameObject("V");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 1.2f;
        LayerLabCharacter.AttachPlayer(visual, charIdx);

        // 전용 캡처 카메라
        var camGO = new GameObject($"PrevCam_{charIdx}");
        camGO.transform.position = new Vector3(slotX, 900.7f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 0.85f;
        cam.aspect           = 1f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.09f, 0.08f, 0.18f, 1f);
        cam.cullingMask      = -1;
        cam.depth            = 10;

        var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        // SpriteRenderer가 렌더 배치에 포함될 때까지 3 프레임 대기
        yield return null;
        yield return null;
        yield return null;

        cam.Render();

        // RT 내용을 Texture2D로 복사 (타이밍 의존 없이 안정적으로 표시 가능)
        RenderTexture.active = rt;
        var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        _previewCache[charIdx] = tex;
        target.texture = tex;

        rt.Release();
        Destroy(rt);
        Destroy(root);
        Destroy(camGO);
    }

    // ── 캐릭터 선택 ──────────────────────────────────────────
    private void OnCharacterSelected(string id)
    {
        StartCoroutine(_api.GetCharacterById(id,
            ch =>
            {
                GameState.CurrentCharacter = ch;
                SceneManager.LoadScene("GameScene");
            },
            err => _statusText.text = "오류: " + err));
    }
}
