using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    private ApiClient _api;
    private CharacterData _player;
    private int _playerHp, _playerMp;
    private int _enemyHp, _enemyMaxHp, _enemyAtk, _enemyDef;
    private string _enemyName, _currentLocation;
    private int _currentWave;
    private const int MAX_WAVE = 5;

    // UI 참조
    private GameObject _locationPanel, _battlePanel, _resultPanel;
    private Transform _locationList;
    private TextMeshProUGUI _battleLog, _playerHpText, _playerMpText, _enemyNameText, _enemyHpText, _resultText;
    private Button _attackBtn, _skillBtn, _fleeBtn;

    private void Start()
    {
        _api = gameObject.AddComponent<ApiClient>();
        _player = GameState.CurrentCharacter;
        _playerHp = _player.generated.stats.hp;
        _playerMp = _player.generated.stats.mp;

        UIHelper.CreateCanvas(out _);
        var canvas = FindObjectOfType<Canvas>().transform;

        var bg = UIHelper.CreatePanel(canvas, new Color(0.05f, 0.05f, 0.1f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        BuildLocationPanel(canvas);
        BuildBattlePanel(canvas);
        BuildResultPanel(canvas);
        ShowLocationPanel();
    }

    // ───── 장소 선택 패널 ─────
    private void BuildLocationPanel(Transform canvas)
    {
        _locationPanel = UIHelper.CreatePanel(canvas, new Color(0, 0, 0, 0), "LocationPanel");
        UIHelper.Stretch(_locationPanel.GetComponent<RectTransform>());
        var lp = _locationPanel.transform;

        UIHelper.CreateText(lp, $"{_player.generated.name}\nHP {_playerHp}  MP {_playerMp}", 34,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));

        UIHelper.CreateText(lp, "장소를 선택하세요", 48,
            new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.88f));

        // 스크롤 리스트
        var scrollGO = new GameObject("LocationScroll");
        scrollGO.transform.SetParent(lp, false);
        UIHelper.SetAnchors(scrollGO.AddComponent<RectTransform>(),
            new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.80f));
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var scroll = scrollGO.AddComponent<ScrollRect>();

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewport.AddComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;
        _locationList = content.transform;

        // 커스텀 장소
        var customInput = UIHelper.CreateInputField(lp, "직접 입력 (예: 폐허 도시)",
            new Vector2(0.05f, 0.16f), new Vector2(0.72f, 0.26f));
        var goBtn = UIHelper.CreateButton(lp, "이동",
            new Vector2(0.74f, 0.16f), new Vector2(0.95f, 0.26f), new Color(0.5f, 0.3f, 0.7f));
        goBtn.onClick.AddListener(() =>
        {
            string loc = customInput.text.Trim();
            if (!string.IsNullOrEmpty(loc)) StartLocation(loc);
        });

        var backBtn = UIHelper.CreateButton(lp, "메인 메뉴",
            new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.13f), new Color(0.3f, 0.3f, 0.35f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }

    private void ShowLocationPanel()
    {
        _locationPanel.SetActive(true);
        _battlePanel.SetActive(false);
        _resultPanel.SetActive(false);

        foreach (Transform child in _locationList) Destroy(child.gameObject);

        foreach (var loc in _player.generated.locations)
        {
            var card = new GameObject("Loc_" + loc.name);
            card.transform.SetParent(_locationList, false);
            card.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f);
            var le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 110;
            var btn = card.AddComponent<Button>();
            string locName = loc.name;
            btn.onClick.AddListener(() => StartLocation(locName));

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(card.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{loc.name}</b>\n{loc.description}";
            tmp.fontSize = 28;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(16, 0, 16, 0);
            UIHelper.Stretch(textGO.GetComponent<RectTransform>());
        }
    }

    // ───── 전투 패널 ─────
    private void BuildBattlePanel(Transform canvas)
    {
        _battlePanel = UIHelper.CreatePanel(canvas, new Color(0, 0, 0, 0), "BattlePanel");
        UIHelper.Stretch(_battlePanel.GetComponent<RectTransform>());
        var bp = _battlePanel.transform;

        _playerHpText = UIHelper.CreateText(bp, "", 30,
            new Vector2(0.02f, 0.90f), new Vector2(0.50f, 0.97f), TextAlignmentOptions.MidlineLeft);
        _playerMpText = UIHelper.CreateText(bp, "", 30,
            new Vector2(0.02f, 0.84f), new Vector2(0.50f, 0.90f), TextAlignmentOptions.MidlineLeft);
        _enemyNameText = UIHelper.CreateText(bp, "", 30,
            new Vector2(0.50f, 0.90f), new Vector2(0.98f, 0.97f), TextAlignmentOptions.MidlineRight);
        _enemyHpText = UIHelper.CreateText(bp, "", 30,
            new Vector2(0.50f, 0.84f), new Vector2(0.98f, 0.90f), TextAlignmentOptions.MidlineRight);

        // 전투 로그
        var logBG = UIHelper.CreatePanel(bp, new Color(0.08f, 0.08f, 0.12f), "LogBG");
        UIHelper.SetAnchors(logBG.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.25f), new Vector2(0.98f, 0.83f));
        var scrollGO = new GameObject("LogScroll");
        scrollGO.transform.SetParent(logBG.transform, false);
        UIHelper.Stretch(scrollGO.AddComponent<RectTransform>());
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        UIHelper.Stretch(viewport.AddComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;

        var logTextGO = new GameObject("LogText");
        logTextGO.transform.SetParent(content.transform, false);
        _battleLog = logTextGO.AddComponent<TextMeshProUGUI>();
        _battleLog.fontSize = 28;
        _battleLog.color = Color.white;
        _battleLog.alignment = TextAlignmentOptions.TopLeft;
        _battleLog.margin = new Vector4(10, 10, 10, 10);
        var logRT = logTextGO.GetComponent<RectTransform>();
        logRT.anchorMin = new Vector2(0, 1);
        logRT.anchorMax = new Vector2(1, 1);
        logRT.pivot = new Vector2(0.5f, 1);
        logTextGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _attackBtn = UIHelper.CreateButton(bp, "공격",
            new Vector2(0.02f, 0.13f), new Vector2(0.32f, 0.23f), new Color(0.7f, 0.2f, 0.2f));
        _skillBtn = UIHelper.CreateButton(bp, "스킬",
            new Vector2(0.35f, 0.13f), new Vector2(0.65f, 0.23f), new Color(0.2f, 0.3f, 0.75f));
        _fleeBtn = UIHelper.CreateButton(bp, "도망",
            new Vector2(0.68f, 0.13f), new Vector2(0.98f, 0.23f), new Color(0.4f, 0.4f, 0.15f));
    }

    private void StartLocation(string locationName)
    {
        _currentLocation = locationName;
        _currentWave = 1;
        _battleLog.text = "";
        _locationPanel.SetActive(false);
        _battlePanel.SetActive(true);
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        _enemyName = $"{_currentLocation}의 몬스터";
        _enemyHp = _enemyMaxHp = 50 + _currentWave * 20;
        _enemyAtk = 10 + _currentWave * 5;
        _enemyDef = 3 + _currentWave * 2;

        RefreshBattleUI();
        AppendLog($"=== 웨이브 {_currentWave} / {MAX_WAVE} ===");
        AppendLog($"{_enemyName} 등장!");

        _attackBtn.onClick.RemoveAllListeners();
        _skillBtn.onClick.RemoveAllListeners();
        _fleeBtn.onClick.RemoveAllListeners();
        _attackBtn.onClick.AddListener(OnAttack);
        _skillBtn.onClick.AddListener(OnSkill);
        _fleeBtn.onClick.AddListener(OnFlee);
    }

    private void OnAttack()
    {
        SetButtonsInteractable(false);
        int dmg = Mathf.Max(1, _player.generated.stats.atk - _enemyDef);
        _enemyHp -= dmg;
        AppendLog($"{_player.generated.name}의 공격! {dmg} 피해!");
        AfterPlayerAction();
    }

    private void OnSkill()
    {
        if (_player.generated.abilities == null || _player.generated.abilities.Count == 0)
        { AppendLog("사용 가능한 스킬이 없습니다."); return; }

        int mpCost = 20;
        if (_playerMp < mpCost) { AppendLog("MP가 부족합니다!"); return; }

        SetButtonsInteractable(false);
        _playerMp -= mpCost;
        int dmg = Mathf.Max(1, (int)(_player.generated.stats.atk * 1.8f) - _enemyDef);
        _enemyHp -= dmg;
        AppendLog($"{_player.generated.abilities[0].name} 사용! {dmg} 피해!");
        AfterPlayerAction();
    }

    private void OnFlee()
    {
        AppendLog("전투에서 도망쳤습니다.");
        ShowLocationPanel();
    }

    private void AfterPlayerAction()
    {
        RefreshBattleUI();
        if (_enemyHp <= 0)
        {
            AppendLog($"{_enemyName} 처치!");
            if (_currentWave >= MAX_WAVE) { ShowResult(true); return; }
            _currentWave++;
            SpawnEnemy();
            return;
        }

        int dmg = Mathf.Max(1, _enemyAtk - _player.generated.stats.def);
        _playerHp -= dmg;
        AppendLog($"{_enemyName}의 반격! {dmg} 피해!");
        RefreshBattleUI();

        if (_playerHp <= 0)
        {
            AppendLog($"{_player.generated.name} 사망...");
            StartCoroutine(_api.DeleteCharacter(_player.id, () => { }, _ => { }));
            ShowResult(false);
            return;
        }
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        _attackBtn.interactable = value;
        _skillBtn.interactable = value;
        _fleeBtn.interactable = value;
    }

    private void RefreshBattleUI()
    {
        _playerHpText.text = $"HP: {Mathf.Max(0, _playerHp)} / {_player.generated.stats.hp}";
        _playerMpText.text = $"MP: {_playerMp} / {_player.generated.stats.mp}";
        _enemyNameText.text = _enemyName;
        _enemyHpText.text = $"HP: {Mathf.Max(0, _enemyHp)} / {_enemyMaxHp}";
    }

    private void AppendLog(string msg)
    {
        _battleLog.text += msg + "\n";
    }

    // ───── 결과 패널 ─────
    private void BuildResultPanel(Transform canvas)
    {
        _resultPanel = UIHelper.CreatePanel(canvas, new Color(0, 0, 0, 0.85f), "ResultPanel");
        UIHelper.Stretch(_resultPanel.GetComponent<RectTransform>());
        var rp = _resultPanel.transform;

        _resultText = UIHelper.CreateText(rp, "", 44,
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.65f));

        var backBtn = UIHelper.CreateButton(rp, "메인 메뉴로",
            new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.42f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }

    private void ShowResult(bool victory)
    {
        _battlePanel.SetActive(false);
        _resultPanel.SetActive(true);
        _resultText.text = victory
            ? $"{_currentLocation} 클리어!\n모든 웨이브 돌파!"
            : $"{_player.generated.name} 사망\n캐릭터가 삭제되었습니다.";
    }
}
