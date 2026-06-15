using UnityEngine;
using UnityEngine.UI;
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
        // 배경
        var bg = UIHelper.CreatePanel(canvas, new Color(0.06f, 0.05f, 0.12f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        // 헤더
        var header = UIHelper.CreatePanel(canvas, new Color(0.10f, 0.07f, 0.22f), "Header");
        UIHelper.SetAnchors(header.GetComponent<RectTransform>(),
            new Vector2(0f, 0.88f), new Vector2(1f, 1f));

        var headerLine = UIHelper.CreatePanel(canvas, new Color(0.6f, 0.35f, 1.0f, 0.7f), "HeaderLine");
        UIHelper.SetAnchors(headerLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.878f), new Vector2(1f, 0.882f));

        var title = UIHelper.CreateText(canvas, "캐릭터 생성", 56,
            new Vector2(0.08f, 0.888f), new Vector2(0.92f, 0.972f));
        title.color = new Color(1f, 0.88f, 0.50f);
        var ol = title.gameObject.AddComponent<Outline>();
        ol.effectColor    = new Color(0.7f, 0.2f, 0.05f, 1f);
        ol.effectDistance = new Vector2(3, -3);

        // 입력 필드 5개 (라벨 + 필드)
        string[] labelNames  = { "이름", "외형", "무기", "컨셉", "세계관" };
        string[] placeholders = {
            "예: 아르카나",
            "예: 검은 머리, 키 큰 여성",
            "예: 지팡이",
            "예: 신중한 마법사",
            "예: 중세 마법 왕국",
        };

        float groupH = 0.124f;
        float startY = 0.880f;
        float labelH = 0.030f, fieldH = 0.075f, labelFieldGap = 0.005f;

        var fields = new TMP_InputField[5];
        for (int i = 0; i < 5; i++)
        {
            float top = startY - i * groupH;
            float labelBot = top - labelH;
            float fieldTop = labelBot - labelFieldGap;
            float fieldBot = fieldTop - fieldH;

            // 라벨
            var lbl = UIHelper.CreateText(canvas, labelNames[i], 24,
                new Vector2(0.05f, labelBot), new Vector2(0.95f, top),
                TextAlignmentOptions.Left);
            lbl.color = new Color(0.75f, 0.65f, 1.0f, 0.95f);

            // 구분선
            var line = UIHelper.CreatePanel(canvas, new Color(0.6f, 0.35f, 1.0f, 0.25f), "Line");
            UIHelper.SetAnchors(line.GetComponent<RectTransform>(),
                new Vector2(0.05f, top - 0.001f), new Vector2(0.95f, top));

            // 입력 필드
            fields[i] = UIHelper.CreateInputField(canvas, placeholders[i],
                new Vector2(0.05f, fieldBot), new Vector2(0.95f, fieldTop));
        }

        _name       = fields[0];
        _appearance = fields[1];
        _weapon     = fields[2];
        _concept    = fields[3];
        _worldview  = fields[4];

        // 에러 텍스트
        _errorText = UIHelper.CreateText(canvas, "", 24,
            new Vector2(0.05f, 0.255f), new Vector2(0.95f, 0.280f));
        _errorText.color = new Color(1f, 0.42f, 0.42f);

        // 생성 버튼
        var createBtn = UIHelper.CreateButton(canvas, "AI로 생성하기",
            new Vector2(0.05f, 0.145f), new Vector2(0.95f, 0.240f),
            new Color(0.15f, 0.48f, 0.28f));
        createBtn.onClick.AddListener(OnCreate);

        // 뒤로 버튼
        var backBtn = UIHelper.CreateButton(canvas, "뒤로",
            new Vector2(0.05f, 0.035f), new Vector2(0.95f, 0.130f),
            new Color(0.18f, 0.14f, 0.30f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        // 로딩 오버레이
        _loadingPanel = UIHelper.CreatePanel(canvas, new Color(0, 0, 0, 0.80f), "Loading");
        UIHelper.Stretch(_loadingPanel.GetComponent<RectTransform>());
        var loadingText = UIHelper.CreateText(_loadingPanel.transform,
            "AI가 캐릭터를 생성중...", 44,
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.58f));
        loadingText.color = new Color(0.75f, 0.65f, 1.0f);
        _loadingPanel.SetActive(false);
    }

    private void OnCreate()
    {
        string n  = _name.text.Trim();
        string a  = _appearance.text.Trim();
        string w  = _weapon.text.Trim();
        string c  = _concept.text.Trim();
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
                SceneManager.LoadScene("VillageScene");
            },
            err =>
            {
                _loadingPanel.SetActive(false);
                _errorText.text = "오류: " + err;
            }));
    }
}
