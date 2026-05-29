using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    private ApiClient _api;
    private Transform _canvas;
    private Transform _listContent;
    private TextMeshProUGUI _statusText;
    private int _cardIndex;

    private void Start()
    {
        _api = gameObject.AddComponent<ApiClient>();
        UIHelper.CreateCanvas(out _);
        _canvas = FindObjectOfType<Canvas>().transform;
        BuildUI();
        LoadCharacterList();
    }

    private void BuildUI()
    {
        // 하늘색 배경
        var bg = UIHelper.CreatePanel(_canvas, new Color(0.53f, 0.81f, 0.98f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        // 타이틀
        UIHelper.CreateText(_canvas, "캐릭터 선택", 64,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));

        _statusText = UIHelper.CreateText(_canvas, "불러오는 중...", 30,
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.88f));

        // 가로 스크롤 영역
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(_canvas, false);
        UIHelper.SetAnchors(scrollGO.AddComponent<RectTransform>(),
            new Vector2(0f, 0.18f), new Vector2(1f, 0.83f));
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.vertical = false;
        scroll.horizontal = true;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewport.AddComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot = new Vector2(0, 0.5f);
        contentRT.anchoredPosition = Vector2.zero;
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24;
        hlg.padding = new RectOffset(40, 40, 20, 20);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        _listContent = content.transform;

        // 새 캐릭터 버튼
        var newBtn = UIHelper.CreateButton(_canvas, "+ 새 캐릭터",
            new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.15f), new Color(0.2f, 0.65f, 0.3f));
        newBtn.onClick.AddListener(() => SceneManager.LoadScene("CharacterCreationScene"));
    }

    private void LoadCharacterList()
    {
        StartCoroutine(_api.GetAllCharacters(
            items =>
            {
                _cardIndex = 0;
                _statusText.text = items.Length == 0 ? "저장된 캐릭터가 없습니다.\n아래 버튼으로 새 캐릭터를 만드세요." : "";
                foreach (var item in items) AddCharacterCard(item);
                StartCoroutine(RefreshCardTexts());
            },
            err => _statusText.text = "오류: " + err));
    }

    private IEnumerator RefreshCardTexts()
    {
        yield return null;
        yield return null;
        foreach (var tmp in _listContent.GetComponentsInChildren<TextMeshProUGUI>())
            tmp.ForceMeshUpdate(false, true);
    }

    private void AddCharacterCard(CharacterListItem item)
    {
        _cardIndex++;

        // 카드 컨테이너
        var card = new GameObject("Card_" + item.name);
        card.transform.SetParent(_listContent, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.15f, 0.18f, 0.28f);
        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 280;
        le.flexibleHeight = 1;
        var btn = card.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.3f, 0.45f);
        colors.pressedColor = new Color(0.1f, 0.12f, 0.2f);
        btn.colors = colors;
        string capturedId = item.id;
        btn.onClick.AddListener(() => OnCharacterSelected(capturedId));

        var font = UIHelper.GetFont();

        // 상단 컬러 헤더 (슬롯 번호)
        var header = new GameObject("Header");
        header.transform.SetParent(card.transform, false);
        header.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.75f);
        UIHelper.SetAnchors(header.AddComponent<RectTransform>(),
            new Vector2(0, 0.72f), new Vector2(1, 1f));

        var numGO = new GameObject("Num");
        numGO.transform.SetParent(header.transform, false);
        var numTmp = numGO.AddComponent<TextMeshProUGUI>();
        if (font != null) numTmp.font = font;
        numTmp.text = $"No.{_cardIndex}";
        numTmp.fontSize = 36;
        numTmp.alignment = TextAlignmentOptions.Center;
        numTmp.color = Color.white;
        UIHelper.Stretch(numGO.GetComponent<RectTransform>());

        // 캐릭터 이름
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(card.transform, false);
        var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        if (font != null) nameTmp.font = font;
        nameTmp.text = item.name;
        nameTmp.fontSize = 30;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = Color.white;
        nameTmp.enableWordWrapping = true;
        UIHelper.SetAnchors(nameGO.AddComponent<RectTransform>(),
            new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.72f));

        // 구분선
        var line = new GameObject("Line");
        line.transform.SetParent(card.transform, false);
        line.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);
        UIHelper.SetAnchors(line.AddComponent<RectTransform>(),
            new Vector2(0.1f, 0.485f), new Vector2(0.9f, 0.495f));

        // 무기
        var weaponGO = new GameObject("Weapon");
        weaponGO.transform.SetParent(card.transform, false);
        var weaponTmp = weaponGO.AddComponent<TextMeshProUGUI>();
        if (font != null) weaponTmp.font = font;
        weaponTmp.text = "⚔ " + item.weapon;
        weaponTmp.fontSize = 26;
        weaponTmp.alignment = TextAlignmentOptions.Center;
        weaponTmp.color = new Color(0.85f, 0.85f, 1f);
        UIHelper.SetAnchors(weaponGO.AddComponent<RectTransform>(),
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.48f));

        // 컨셉
        var conceptGO = new GameObject("Concept");
        conceptGO.transform.SetParent(card.transform, false);
        var conceptTmp = conceptGO.AddComponent<TextMeshProUGUI>();
        if (font != null) conceptTmp.font = font;
        conceptTmp.text = item.concept;
        conceptTmp.fontSize = 24;
        conceptTmp.alignment = TextAlignmentOptions.Center;
        conceptTmp.color = new Color(0.7f, 0.9f, 0.7f);
        UIHelper.SetAnchors(conceptGO.AddComponent<RectTransform>(),
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.30f));
    }

    private void OnCharacterSelected(string id)
    {
        StartCoroutine(_api.GetCharacterById(id,
            character =>
            {
                GameState.CurrentCharacter = character;
                SceneManager.LoadScene("GameScene");
            },
            err => _statusText.text = "오류: " + err));
    }
}
