using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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
        var viewportRT = viewport.AddComponent<RectTransform>();
        UIHelper.Stretch(viewportRT);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewportRT;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot = new Vector2(0, 0.5f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.padding = new RectOffset(40, 40, 30, 30);
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
            },
            err => _statusText.text = "오류: " + err));
    }

    private void AddCharacterCard(CharacterListItem item)
    {
        _cardIndex++;

        var card = new GameObject("Card_" + item.name);
        card.transform.SetParent(_listContent, false);
        card.AddComponent<Image>().color = new Color(0.15f, 0.18f, 0.28f);
        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 300;
        le.minWidth = 300;
        var btn = card.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.3f, 0.45f);
        colors.pressedColor = new Color(0.1f, 0.12f, 0.2f);
        btn.colors = colors;
        string capturedId = item.id;
        btn.onClick.AddListener(() => OnCharacterSelected(capturedId));

        // 상단 컬러 헤더 (슬롯 번호)
        var header = new GameObject("Header");
        header.transform.SetParent(card.transform, false);
        header.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.75f);
        UIHelper.SetAnchors(header.GetComponent<RectTransform>(),
            new Vector2(0, 0.72f), new Vector2(1, 1f));
        UIHelper.CreateText(header.transform, $"No.{_cardIndex}", 36,
            Vector2.zero, Vector2.one);

        // 캐릭터 이름
        var nameTmp = UIHelper.CreateText(card.transform, item.name, 30,
            new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.72f));
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.enableWordWrapping = true;

        // 구분선
        var line = new GameObject("Line");
        line.transform.SetParent(card.transform, false);
        line.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);
        UIHelper.SetAnchors(line.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.485f), new Vector2(0.9f, 0.495f));

        // 무기
        var weaponTmp = UIHelper.CreateText(card.transform, "[무기] " + item.weapon, 26,
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.48f));
        weaponTmp.color = new Color(0.85f, 0.85f, 1f);

        // 컨셉
        var conceptTmp = UIHelper.CreateText(card.transform, item.concept, 24,
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.30f));
        conceptTmp.color = new Color(0.7f, 0.9f, 0.7f);
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
