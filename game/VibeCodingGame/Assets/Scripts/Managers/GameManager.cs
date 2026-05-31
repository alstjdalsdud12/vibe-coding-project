using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private ApiClient _api;
    private CharacterData _player;
    private int _playerHp, _playerMp;
    private int _enemyHp, _enemyMaxHp, _enemyAtk, _enemyDef;
    private string _enemyName;

    private MapPlayerController _playerController;
    private MonsterController _currentMonster;

    // HUD
    private TextMeshProUGUI _hudHpText, _hudMpText, _zoneNameText;
    private RectTransform _hpBarFill, _mpBarFill;
    private GameObject _zoneBox;

    // Battle panel
    private GameObject _battlePanel, _resultPanel;
    private TextMeshProUGUI _battleLog, _playerHpText, _playerMpText,
                            _enemyNameText, _enemyHpText, _resultText;
    private Button _attackBtn, _skillBtn, _fleeBtn;

    private Sprite _squareSprite;

    // 구역 배치 (center position, size)
    private static readonly Vector2[] ZonePositions = {
        new Vector2(0,  12), new Vector2(9,  30), new Vector2(-8, 48),
        new Vector2(5,  66), new Vector2(0,  82),
    };
    private static readonly Vector2[] ZoneSizes = {
        new Vector2(28, 24), new Vector2(24, 22), new Vector2(24, 22),
        new Vector2(20, 20), new Vector2(16, 16),
    };
    private static readonly Color[] ZoneColors = {
        new Color(0.12f, 0.38f, 0.22f), new Color(0.36f, 0.40f, 0.12f),
        new Color(0.32f, 0.22f, 0.08f), new Color(0.12f, 0.12f, 0.28f),
        new Color(0.28f, 0.04f, 0.08f),
    };

    private void Start()
    {
        _api = gameObject.AddComponent<ApiClient>();
        _player = GameState.CurrentCharacter;
        _playerHp = _player.generated.stats.hp;
        _playerMp = _player.generated.stats.mp;
        _squareSprite = CreateSquareSprite();

        SetupCamera();
        SetupMap();
        SetupUI();
    }

    // ─── 카메라 ────────────────────────────────────────
    private void SetupCamera()
    {
        if (Camera.main != null) return;
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 10;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.06f);
    }

    // ─── 맵 생성 ──────────────────────────────────────
    private void SetupMap()
    {
        var locs = _player.generated.locations;
        for (int i = 0; i < Mathf.Min(5, locs.Count); i++)
            CreateZone(locs[i].name, locs[i].description, i,
                ZonePositions[i], ZoneSizes[i], ZoneColors[i]);

        CreatePlayer(new Vector3(0, 3, 0));
    }

    private void CreateZone(string name, string desc, int index, Vector2 pos, Vector2 size, Color color)
    {
        // 구역 배경
        var zoneGO = new GameObject("Zone_" + name);
        zoneGO.transform.position = new Vector3(pos.x, pos.y, 0);
        zoneGO.transform.localScale = new Vector3(size.x, size.y, 1);
        var sr = zoneGO.AddComponent<SpriteRenderer>();
        sr.sprite = _squareSprite;
        sr.color = color;
        sr.sortingOrder = 0;
        var col = zoneGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var zone = zoneGO.AddComponent<ZoneController>();
        zone.ZoneName = name;
        zone.ZoneDescription = desc;
        zone.ZoneIndex = index;
        zone.OnPlayerEnter = OnPlayerEnterZone;

        // 구역 이름 라벨 (월드 스페이스 TMP)
        var labelGO = new GameObject("Label_" + name);
        labelGO.transform.position = new Vector3(pos.x, pos.y + size.y * 0.3f, -1);
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.font = UIHelper.GetFont();
        tmp.text = "<b>" + name + "</b>";
        tmp.fontSize = 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1, 1, 1, 0.85f);
        tmp.sortingOrder = 5;
        tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(14, 5);
        tmp.ForceMeshUpdate();

        // 구역당 몬스터 2마리 생성
        for (int m = 0; m < 2; m++)
        {
            float ox = (m == 0 ? -1 : 1) * size.x * 0.22f;
            var a = new Vector2(pos.x + ox - 2f, pos.y - size.y * 0.12f);
            var b = new Vector2(pos.x + ox + 2f, pos.y + size.y * 0.12f);
            CreateMonster(name, index, a, b);
        }
    }

    private void CreateMonster(string zoneName, int zoneIndex, Vector2 patrolA, Vector2 patrolB)
    {
        var monGO = new GameObject("Monster_" + zoneName);
        monGO.transform.position = new Vector3(patrolA.x, patrolA.y, 0);
        var sr = monGO.AddComponent<SpriteRenderer>();
        sr.sprite = _squareSprite;
        sr.color = Color.Lerp(new Color(0.85f, 0.25f, 0.1f), new Color(0.55f, 0.0f, 0.75f), zoneIndex / 4f);
        sr.sortingOrder = 8;

        var rb = monGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.isKinematic = true;
        rb.freezeRotation = true;

        var col = monGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.5f, 2.5f);

        var mon = monGO.AddComponent<MonsterController>();
        mon.ZoneName = zoneName;
        mon.ZoneIndex = zoneIndex;
        mon.OnPlayerContact = OnMonsterEncountered;
        mon.Init(patrolA, patrolB, 1.5f + zoneIndex * 0.4f);
    }

    private void CreatePlayer(Vector3 startPos)
    {
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        var sr = playerGO.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(0.95f, 0.88f, 0.45f);
        sr.sortingOrder = 10;
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        playerGO.AddComponent<CircleCollider2D>().radius = 0.5f;
        playerGO.transform.position = startPos;
        _playerController = playerGO.AddComponent<MapPlayerController>();
    }

    // ─── UI ───────────────────────────────────────────
    private void SetupUI()
    {
        UIHelper.CreateCanvas(out _);
        var canvas = FindObjectOfType<Canvas>().transform;

        // 상단 HUD 배경
        var hudBG = UIHelper.CreatePanel(canvas, new Color(0.05f, 0.04f, 0.12f, 0.88f), "HUD");
        UIHelper.SetAnchors(hudBG.GetComponent<RectTransform>(), new Vector2(0f, 0.905f), Vector2.one);

        // HUD 하단 라인
        var hudLine = UIHelper.CreatePanel(canvas, new Color(0.5f, 0.30f, 0.9f, 0.6f), "HudLine");
        UIHelper.SetAnchors(hudLine.GetComponent<RectTransform>(), new Vector2(0f, 0.903f), new Vector2(1f, 0.906f));

        // 캐릭터 이름
        var nameText = UIHelper.CreateText(hudBG.transform, _player.generated.name, 22,
            new Vector2(0.02f, 0.55f), new Vector2(0.60f, 0.98f), TextAlignmentOptions.Left);
        nameText.color = new Color(1f, 0.88f, 0.50f);

        // HP 바
        var hpBarBG = UIHelper.CreatePanel(hudBG.transform, new Color(0.20f, 0.04f, 0.04f), "HPBarBG");
        UIHelper.SetAnchors(hpBarBG.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.08f), new Vector2(0.60f, 0.50f));
        var hpFill = UIHelper.CreatePanel(hpBarBG.transform, new Color(0.80f, 0.15f, 0.15f), "HPFill");
        _hpBarFill = hpFill.GetComponent<RectTransform>();
        UIHelper.SetAnchors(_hpBarFill, Vector2.zero, Vector2.one);
        _hudHpText = UIHelper.CreateText(hpBarBG.transform, "", 20,
            new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), TextAlignmentOptions.Left);

        // MP 바
        var mpBarBG = UIHelper.CreatePanel(hudBG.transform, new Color(0.04f, 0.08f, 0.28f), "MPBarBG");
        UIHelper.SetAnchors(mpBarBG.GetComponent<RectTransform>(),
            new Vector2(0.62f, 0.08f), new Vector2(0.80f, 0.50f));
        var mpFill = UIHelper.CreatePanel(mpBarBG.transform, new Color(0.15f, 0.35f, 0.85f), "MPFill");
        _mpBarFill = mpFill.GetComponent<RectTransform>();
        UIHelper.SetAnchors(_mpBarFill, Vector2.zero, Vector2.one);
        _hudMpText = UIHelper.CreateText(mpBarBG.transform, "", 20,
            new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), TextAlignmentOptions.Left);

        // 메뉴 버튼
        var menuBtn = UIHelper.CreateButton(canvas, "메뉴",
            new Vector2(0.82f, 0.913f), new Vector2(0.98f, 0.993f),
            new Color(0.22f, 0.18f, 0.35f));
        menuBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));

        // 구역 진입 알림 (배경 박스 포함)
        _zoneBox = UIHelper.CreatePanel(canvas, new Color(0.06f, 0.04f, 0.16f, 0.82f), "ZoneBox");
        UIHelper.SetAnchors(_zoneBox.GetComponent<RectTransform>(),
            new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.63f));
        _zoneNameText = UIHelper.CreateText(_zoneBox.transform, "", 32,
            new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f));
        _zoneNameText.color = new Color(1f, 0.88f, 0.50f);
        _zoneBox.SetActive(false);

        BuildMinimap(canvas);
        BuildBattlePanel(canvas);
        BuildResultPanel(canvas);
        _battlePanel.SetActive(false);
        _resultPanel.SetActive(false);
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        float hpRatio = (float)Mathf.Max(0, _playerHp) / _player.generated.stats.hp;
        float mpRatio = (float)_playerMp / _player.generated.stats.mp;
        if (_hpBarFill) _hpBarFill.anchorMax = new Vector2(hpRatio, 1f);
        if (_mpBarFill) _mpBarFill.anchorMax = new Vector2(mpRatio, 1f);
        _hudHpText.text = $"HP {Mathf.Max(0, _playerHp)}/{_player.generated.stats.hp}";
        _hudMpText.text = $"MP {_playerMp}/{_player.generated.stats.mp}";
    }

    // ─── 구역 / 몬스터 이벤트 ─────────────────────────
    private void OnPlayerEnterZone(ZoneController zone)
    {
        _zoneNameText.text = zone.ZoneName;
        _zoneBox.SetActive(true);
        StopCoroutine("ClearZoneBox");
        StartCoroutine("ClearZoneBox");
    }

    private IEnumerator ClearZoneBox()
    {
        yield return new WaitForSeconds(2.5f);
        _zoneBox.SetActive(false);
    }

    private void OnMonsterEncountered(MonsterController monster)
    {
        if (_battlePanel.activeSelf) return;
        _currentMonster = monster;
        monster.SetPaused(true);
        _playerController.SetMovementEnabled(false);

        _enemyName = monster.ZoneName + "의 몬스터";
        int idx = monster.ZoneIndex;
        _enemyHp = _enemyMaxHp = 40 + idx * 25;
        _enemyAtk = 8 + idx * 6;
        _enemyDef = 2 + idx * 2;

        _battleLog.text = $"[{monster.ZoneName}]\n{_enemyName} 출현!\n";
        _battlePanel.SetActive(true);
        RefreshBattleUI();
        SetButtonsInteractable(true);
    }

    // ─── 미니맵 ───────────────────────────────────────
    private void BuildMinimap(Transform canvas)
    {
        var worldMin  = new Vector2(-22f, -5f);
        var worldSize = new Vector2(45f, 100f);

        // 테두리
        var border = UIHelper.CreatePanel(canvas, new Color(0.55f, 0.55f, 0.6f, 0.75f), "MinimapBorder");
        UIHelper.SetAnchors(border.GetComponent<RectTransform>(),
            new Vector2(0.005f, 0.645f), new Vector2(0.227f, 0.895f));

        // 배경
        var minimapPanel = UIHelper.CreatePanel(canvas, new Color(0.04f, 0.04f, 0.09f, 0.92f), "Minimap");
        UIHelper.SetAnchors(minimapPanel.GetComponent<RectTransform>(),
            new Vector2(0.01f, 0.65f), new Vector2(0.222f, 0.89f));
        var mapT = minimapPanel.transform;

        // 구역 표시
        var locs = _player.generated.locations;
        for (int i = 0; i < Mathf.Min(5, locs.Count); i++)
        {
            var uvCenter = new Vector2(
                (ZonePositions[i].x - worldMin.x) / worldSize.x,
                (ZonePositions[i].y - worldMin.y) / worldSize.y);
            var uvSize = new Vector2(
                ZoneSizes[i].x / worldSize.x,
                ZoneSizes[i].y / worldSize.y);

            var zoneImg = new GameObject("MZ_" + i);
            zoneImg.transform.SetParent(mapT, false);
            var c = ZoneColors[i];
            zoneImg.AddComponent<UnityEngine.UI.Image>().color =
                new Color(Mathf.Min(c.r * 2.2f, 1), Mathf.Min(c.g * 2.2f, 1), Mathf.Min(c.b * 2.2f, 1));
            var rt = zoneImg.GetComponent<RectTransform>();
            rt.anchorMin = uvCenter - uvSize * 0.5f;
            rt.anchorMax = uvCenter + uvSize * 0.5f;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // 플레이어 위치 점
        var dotGO = new GameObject("PlayerDot");
        dotGO.transform.SetParent(mapT, false);
        dotGO.AddComponent<UnityEngine.UI.Image>().color = new Color(1f, 0.95f, 0.25f);
        var dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.1f, 0.01f);
        dotRT.sizeDelta = new Vector2(7, 7);
        var dot = dotGO.AddComponent<MinimapDot>();
        dot.WorldMin  = worldMin;
        dot.WorldSize = worldSize;
    }

    // ─── 전투 패널 ────────────────────────────────────
    private void BuildBattlePanel(Transform canvas)
    {
        _battlePanel = UIHelper.CreatePanel(canvas, new Color(0.04f, 0.03f, 0.10f, 0.96f), "BattlePanel");
        UIHelper.Stretch(_battlePanel.GetComponent<RectTransform>());
        var bp = _battlePanel.transform;

        // 상단 구분선
        var topLine = UIHelper.CreatePanel(bp, new Color(0.5f, 0.30f, 0.9f, 0.6f), "TopLine");
        UIHelper.SetAnchors(topLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.820f), new Vector2(1f, 0.823f));

        // 플레이어 정보 (좌측)
        var playerBG = UIHelper.CreatePanel(bp, new Color(0.08f, 0.06f, 0.18f), "PlayerBG");
        UIHelper.SetAnchors(playerBG.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.825f), new Vector2(0.48f, 0.975f));
        _playerHpText = UIHelper.CreateText(playerBG.transform, "", 24,
            new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Left);
        _playerHpText.color = new Color(1f, 0.45f, 0.45f);
        _playerMpText = UIHelper.CreateText(playerBG.transform, "", 24,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.50f), TextAlignmentOptions.Left);
        _playerMpText.color = new Color(0.45f, 0.65f, 1.0f);

        // 적 정보 (우측)
        var enemyBG = UIHelper.CreatePanel(bp, new Color(0.18f, 0.06f, 0.06f), "EnemyBG");
        UIHelper.SetAnchors(enemyBG.GetComponent<RectTransform>(),
            new Vector2(0.52f, 0.825f), new Vector2(0.98f, 0.975f));
        _enemyNameText = UIHelper.CreateText(enemyBG.transform, "", 24,
            new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Right);
        _enemyNameText.color = new Color(1f, 0.70f, 0.30f);
        _enemyHpText = UIHelper.CreateText(enemyBG.transform, "", 24,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.50f), TextAlignmentOptions.Right);
        _enemyHpText.color = new Color(1f, 0.45f, 0.45f);

        // 전투 로그
        var logBG = UIHelper.CreatePanel(bp, new Color(0.06f, 0.05f, 0.14f), "LogBG");
        UIHelper.SetAnchors(logBG.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.295f), new Vector2(0.98f, 0.818f));
        var logBorder = UIHelper.CreatePanel(bp, new Color(0.5f, 0.30f, 0.9f, 0.35f), "LogBorder");
        UIHelper.SetAnchors(logBorder.GetComponent<RectTransform>(),
            new Vector2(0.02f, 0.293f), new Vector2(0.98f, 0.820f));

        var logGO = new GameObject("LogText");
        logGO.transform.SetParent(logBG.transform, false);
        _battleLog = logGO.AddComponent<TextMeshProUGUI>();
        var font = UIHelper.GetFont();
        if (font != null) _battleLog.font = font;
        _battleLog.fontSize = 26;
        _battleLog.color = new Color(0.88f, 0.85f, 1.0f);
        _battleLog.alignment = TextAlignmentOptions.TopLeft;
        _battleLog.margin = new Vector4(14, 10, 14, 10);
        UIHelper.Stretch(logGO.GetComponent<RectTransform>());

        // 하단 구분선
        var botLine = UIHelper.CreatePanel(bp, new Color(0.5f, 0.30f, 0.9f, 0.6f), "BotLine");
        UIHelper.SetAnchors(botLine.GetComponent<RectTransform>(),
            new Vector2(0f, 0.291f), new Vector2(1f, 0.294f));

        // 전투 버튼
        _attackBtn = UIHelper.CreateButton(bp, "공격",
            new Vector2(0.02f, 0.160f), new Vector2(0.31f, 0.278f), new Color(0.65f, 0.12f, 0.12f));
        _skillBtn = UIHelper.CreateButton(bp, "스킬",
            new Vector2(0.34f, 0.160f), new Vector2(0.66f, 0.278f), new Color(0.15f, 0.25f, 0.72f));
        _fleeBtn  = UIHelper.CreateButton(bp, "도망",
            new Vector2(0.69f, 0.160f), new Vector2(0.98f, 0.278f), new Color(0.28f, 0.28f, 0.18f));

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
        AppendLog("도망쳤습니다.");
        StartCoroutine(DelayEndBattle(0.8f));
    }

    private void AfterPlayerAction()
    {
        RefreshBattleUI();
        if (_enemyHp <= 0)
        {
            AppendLog($"{_enemyName} 처치!");
            StartCoroutine(DelayEndBattle(1.5f));
            return;
        }
        int dmg = Mathf.Max(1, _enemyAtk - _player.generated.stats.def);
        _playerHp -= dmg;
        AppendLog($"{_enemyName}의 반격! {dmg} 피해!");
        RefreshBattleUI();
        if (_playerHp <= 0)
        {
            AppendLog($"{_player.generated.name} 사망...");
            StartCoroutine(_api.DeleteCharacter(_player.id,
                () => Debug.Log("[DELETE] 삭제 완료"),
                err => Debug.LogError("[DELETE] 실패: " + err)));
            ShowResult();
            return;
        }
        SetButtonsInteractable(true);
    }

    private IEnumerator DelayEndBattle(float delay)
    {
        SetButtonsInteractable(false);
        yield return new WaitForSeconds(delay);
        EndBattle();
    }

    private void EndBattle()
    {
        _battlePanel.SetActive(false);
        if (_currentMonster != null)
        {
            StartCoroutine(RespawnMonster(_currentMonster));
            _currentMonster = null;
        }
        _playerController.SetMovementEnabled(true);
        RefreshHUD();
    }

    private IEnumerator RespawnMonster(MonsterController monster)
    {
        monster.gameObject.SetActive(false);
        yield return new WaitForSeconds(8f);
        if (monster != null) monster.gameObject.SetActive(true);
    }

    private void RefreshBattleUI()
    {
        _playerHpText.text = $"HP: {Mathf.Max(0, _playerHp)} / {_player.generated.stats.hp}";
        _playerMpText.text = $"MP: {_playerMp} / {_player.generated.stats.mp}";
        _enemyNameText.text = _enemyName;
        _enemyHpText.text = $"HP: {Mathf.Max(0, _enemyHp)} / {_enemyMaxHp}";
    }

    private void SetButtonsInteractable(bool v)
    {
        _attackBtn.interactable = v;
        _skillBtn.interactable = v;
        _fleeBtn.interactable = v;
    }

    private void AppendLog(string msg) => _battleLog.text += msg + "\n";

    // ─── 결과 패널 ────────────────────────────────────
    private void BuildResultPanel(Transform canvas)
    {
        _resultPanel = UIHelper.CreatePanel(canvas, new Color(0, 0, 0, 0.9f), "ResultPanel");
        UIHelper.Stretch(_resultPanel.GetComponent<RectTransform>());
        var rp = _resultPanel.transform;
        _resultText = UIHelper.CreateText(rp, "", 44,
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.65f));
        var backBtn = UIHelper.CreateButton(rp, "메인 메뉴로",
            new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.42f));
        backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }

    private void ShowResult()
    {
        _battlePanel.SetActive(false);
        _resultPanel.SetActive(true);
        _resultText.text = $"{_player.generated.name} 사망\n캐릭터가 삭제되었습니다.";
    }

    // ─── 스프라이트 생성 ──────────────────────────────
    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    private Sprite CreateCircleSprite(int res = 128)
    {
        var tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        for (int x = 0; x < res; x++)
            for (int y = 0; y < res; y++)
            {
                float dx = x - r, dy = y - r;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }
}
