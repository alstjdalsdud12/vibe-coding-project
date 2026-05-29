using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterCreationManager : MonoBehaviour
{
    private ApiClient _api;
    private TMP_InputField _name, _appearance, _weapon, _concept, _worldview;
    private TextMeshProUGUI _errorText;
    private GameObject _loadingPanel;

    private void Start()
    {
        _api = gameObject.AddComponent<ApiClient>();
        UIHelper.CreateCanvas(out _);
        var canvas = FindObjectOfType<Canvas>().transform;
        BuildUI(canvas);
    }

    private void BuildUI(Transform canvas)
    {
        var bg = UIHelper.CreatePanel(canvas, new Color(0.05f, 0.05f, 0.1f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        UIHelper.CreateText(canvas, "캐릭터 생성", 56,
            new Vector2(0.1f, 0.90f), new Vector2(0.9f, 0.97f));

        _name = UIHelper.CreateInputField(canvas, "이름 (예: 아르카나)",
            new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.89f));

        _appearance = UIHelper.CreateInputField(canvas, "외형 (예: 검은 머리, 키 큰 여성)",
            new Vector2(0.05f, 0.69f), new Vector2(0.95f, 0.79f));

        _weapon = UIHelper.CreateInputField(canvas, "무기 (예: 지팡이)",
            new Vector2(0.05f, 0.57f), new Vector2(0.95f, 0.67f));

        _concept = UIHelper.CreateInputField(canvas, "컨셉 (예: 신중한 마법사)",
            new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.55f));

        _worldview = UIHelper.CreateInputField(canvas, "세계관 (예: 중세 마법 왕국)",
            new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.43f));

        _errorText = UIHelper.CreateText(canvas, "", 28,
            new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.32f));
        _errorText.color = new Color(1f, 0.4f, 0.4f);

        var createBtn = UIHelper.CreateButton(canvas, "생성",
            new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.25f));
        createBtn.onClick.AddListener(OnCreate);

        var backBtn = UIHelper.CreateButton(canvas, "뒤로",
            new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.13f), new Color(0.3f, 0.3f, 0.35f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        _loadingPanel = UIHelper.CreatePanel(canvas, new Color(0, 0, 0, 0.7f), "Loading");
        UIHelper.Stretch(_loadingPanel.GetComponent<RectTransform>());
        UIHelper.CreateText(_loadingPanel.transform, "AI가 캐릭터를 생성중...", 48,
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f));
        _loadingPanel.SetActive(false);
    }

    private void OnCreate()
    {
        string n = _name.text.Trim();
        string a = _appearance.text.Trim();
        string w = _weapon.text.Trim();
        string c = _concept.text.Trim();
        string wv = _worldview.text.Trim();

        if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(a) || string.IsNullOrEmpty(w) ||
            string.IsNullOrEmpty(c) || string.IsNullOrEmpty(wv))
        {
            _errorText.text = "모든 항목을 입력해주세요.";
            return;
        }

        _errorText.text = "";
        _loadingPanel.SetActive(true);

        StartCoroutine(_api.CreateCharacter(n, a, w, c, wv,
            character =>
            {
                GameState.CurrentCharacter = character;
                SceneManager.LoadScene("GameScene");
            },
            err =>
            {
                _loadingPanel.SetActive(false);
                _errorText.text = "오류: " + err;
            }));
    }
}
