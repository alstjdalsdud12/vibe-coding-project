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
        var bg = UIHelper.CreatePanel(_canvas, new Color(0.05f, 0.05f, 0.1f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        UIHelper.CreateText(_canvas, "캐릭터 선택", 60,
            new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.97f));

        _statusText = UIHelper.CreateText(_canvas, "불러오는 중...", 32,
            new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.88f));

        // 스크롤 영역
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(_canvas, false);
        UIHelper.SetAnchors(scrollGO.AddComponent<RectTransform>() ?? scrollGO.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.82f));
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        var scroll = scrollGO.AddComponent<ScrollRect>();

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewport.AddComponent<RectTransform>() ?? viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        _listContent = content.transform;

        var newBtn = UIHelper.CreateButton(_canvas, "새 캐릭터 만들기",
            new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.13f), new Color(0.1f, 0.6f, 0.3f));
        newBtn.onClick.AddListener(() => SceneManager.LoadScene("CharacterCreationScene"));
    }

    private void LoadCharacterList()
    {
        StartCoroutine(_api.GetAllCharacters(
            items =>
            {
                _statusText.text = items.Length == 0 ? "저장된 캐릭터가 없습니다." : "";
                foreach (var item in items) AddCharacterCard(item);
            },
            err => _statusText.text = "오류: " + err));
    }

    private void AddCharacterCard(CharacterListItem item)
    {
        var card = new GameObject("Card_" + item.name);
        card.transform.SetParent(_listContent, false);
        card.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 120;

        var btn = card.AddComponent<Button>();
        string capturedId = item.id;
        btn.onClick.AddListener(() => OnCharacterSelected(capturedId));

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(card.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{item.name}</b>  {item.weapon} · {item.concept}";
        tmp.fontSize = 32;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.margin = new Vector4(20, 0, 20, 0);
        UIHelper.Stretch(textGO.GetComponent<RectTransform>());
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
