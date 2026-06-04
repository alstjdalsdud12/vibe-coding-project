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

    private void Start()
    {
        _api = gameObject.AddComponent<ApiClient>();
        UIHelper.CreateCanvas(out _);
        _canvas = FindObjectOfType<Canvas>().transform;
        BuildUI();
        LoadCharacterList();
    }

    // ── UI 구성 ──────────────────────────────────────────────
    private void BuildUI()
    {
        // 배경
        var bg = UIHelper.CreatePanel(_canvas, new Color(0.06f, 0.05f, 0.12f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        // 상단 헤더 영역 (더 넓게)
        var header = UIHelper.CreatePanel(_canvas, new Color(0.10f, 0.07f, 0.22f), "Header");
        UIHelper.SetAnchors(header.GetComponent<RectTransform>(),
            new Vector2(0f, 0.80f), new Vector2(1f, 1f));

        // 헤더 하단 보라색 라인
        var headerLine = UIHelper.CreatePanel(_canvas, new Color(0.6f, 0.35f, 1.0f, 0.7f), "HeaderLine");
        UIHelper.SetAnchors(headerLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.798f), new Vector2(1f, 0.803f));

        // 제목 그림자
        var shadow = UIHelper.CreateText(_canvas, "캐릭터  선택", 68,
            new Vector2(0.102f, 0.855f), new Vector2(0.902f, 0.978f));
        shadow.color = new Color(0.08f, 0f, 0.2f, 0.8f);

        // 제목
        var title = UIHelper.CreateText(_canvas, "캐릭터  선택", 68,
            new Vector2(0.10f, 0.858f), new Vector2(0.90f, 0.980f));
        title.color = new Color(1f, 0.88f, 0.50f);
        var ol = title.gameObject.AddComponent<Outline>();
        ol.effectColor    = new Color(0.7f, 0.2f, 0.05f, 1f);
        ol.effectDistance = new Vector2(3, -3);

        // 서브타이틀 (구분선 위로 올려서 겹침 방지)
        var sub = UIHelper.CreateText(_canvas, "어떤 모험가와 함께하시겠습니까?", 26,
            new Vector2(0.08f, 0.808f), new Vector2(0.92f, 0.856f));
        sub.color = new Color(0.75f, 0.65f, 1.0f, 0.9f);

        // 상태 텍스트 (로딩/에러)
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

        // 하단 라인
        var footerLine = UIHelper.CreatePanel(_canvas, new Color(0.6f, 0.35f, 1.0f, 0.7f), "FooterLine");
        UIHelper.SetAnchors(footerLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.218f), new Vector2(1f, 0.222f));

        // ── 하단 버튼 ─────────────────────────────────────────
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

        // 카드 루트
        var card = new GameObject("Card_" + item.name);
        card.transform.SetParent(_listContent, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.09f, 0.08f, 0.18f);
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

        // 초상화 영역 하단 그라데이션 느낌 (얇은 accent 라인)
        var portraitBottom = new GameObject("PortraitBottom");
        portraitBottom.transform.SetParent(portrait.transform, false);
        portraitBottom.AddComponent<Image>().color = accent;
        UIHelper.SetAnchors(portraitBottom.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0.05f));

        // 캐릭터 초상화 스프라이트 (spumParts 있으면 헤어, 없으면 이름 첫 글자)
        if (item.spumParts != null && !string.IsNullOrEmpty(item.spumParts.hair))
        {
            var portraitImg = new GameObject("PortraitSprite");
            portraitImg.transform.SetParent(portrait.transform, false);
            var img = portraitImg.AddComponent<Image>();
            UIHelper.SetAnchors(portraitImg.GetComponent<RectTransform>(),
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            img.preserveAspect = true;
            SpumCharacterLoader.SetPortrait(img, item.spumParts);
        }
        else
        {
            string initial = item.name.Length > 0 ? item.name[0].ToString() : "?";
            var initTmp = UIHelper.CreateText(portrait.transform, initial, 160,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            initTmp.color = new Color(1f, 1f, 1f, 0.12f);
            initTmp.fontStyle = FontStyles.Bold;
        }

        // 슬롯 번호 배지 (우상단)
        var badge = new GameObject("Badge");
        badge.transform.SetParent(portrait.transform, false);
        badge.AddComponent<Image>().color = accent;
        UIHelper.SetAnchors(badge.GetComponent<RectTransform>(),
            new Vector2(0.72f, 0.78f), new Vector2(1f, 1f));
        UIHelper.CreateText(badge.transform, $"No.{_cardIndex}", 24,
            Vector2.zero, Vector2.one);

        // ── 하단 정보 영역 ───────────────────────
        // 이름
        var nameTmp = UIHelper.CreateText(card.transform, item.name, 36,
            new Vector2(0.07f, 0.32f), new Vector2(0.93f, 0.46f));
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color     = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.enableWordWrapping = false;

        // 구분선
        var line = new GameObject("Divider");
        line.transform.SetParent(card.transform, false);
        line.AddComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.4f);
        UIHelper.SetAnchors(line.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.315f), new Vector2(0.95f, 0.32f));

        // 무기
        var weaponTmp = UIHelper.CreateText(card.transform,
            "무기  " + item.weapon, 30,
            new Vector2(0.07f, 0.19f), new Vector2(0.93f, 0.31f));
        weaponTmp.color     = accentBright;
        weaponTmp.alignment = TextAlignmentOptions.Left;
        weaponTmp.enableWordWrapping = true;

        // 컨셉
        var conceptTmp = UIHelper.CreateText(card.transform, item.concept, 28,
            new Vector2(0.07f, 0.04f), new Vector2(0.93f, 0.19f));
        conceptTmp.color     = new Color(0.72f, 0.72f, 0.90f);
        conceptTmp.alignment = TextAlignmentOptions.Left;
        conceptTmp.enableWordWrapping = true;
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
