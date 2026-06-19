using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 마을 씬 매니저 — 캐릭터 선택 후 진입하는 메인 허브
// 상점/미션/우편/캐릭터정보 팝업 + 던전 입장 버튼 제공
public class VillageManager : MonoBehaviour
{
    // 아이템 정의표 (슬롯 0~3 고정)
    private static readonly (string name, string type, int sell)[] ItemDefs =
    {
        ("낡은 단검 조각", "material",  50),
        ("철제 갑옷 파편", "material",  80),
        ("회복 포션",      "consume",   -1),
        ("마나 포션",      "consume",   -1),
    };

    private CharacterData _ch;
    private ApiClient     _api;
    private Transform     _canvasTF;
    private GameObject    _shopPanel, _missionPanel, _mailPanel, _infoPanel, _bagPanel, _eventPanel, _expeditionPanel;
    private TextMeshProUGUI _goldText;
    private TextMeshProUGUI _levelText;
    private TextMeshProUGUI _hpTxt, _mpTxt, _atkTxt;
    private bool _attendanceCollected;
    private int[]                _bagQty      = new int[4];
    private TextMeshProUGUI[]    _bagQtyTexts = new TextMeshProUGUI[4];
    private GameObject[]         _bagRows     = new GameObject[4];

    private static readonly int[] _missionTargets  = { 1, 3, 10, 500 };
    private static readonly int[] _missionRewards  = { 1000, 200, 300, 100 };
    private const int _missionLevelUpReward = 5; // 0번 미션 보상: 레벨 5 즉시 상승
    private static readonly int[] _missionXpRewards = { 300, 100, 150, 80 };
    private TextMeshProUGUI[]     _missionProgTexts = new TextMeshProUGUI[4];
    private Button[]              _missionRewBtns   = new Button[4];

    private void Start()
    {
        _ch  = GameState.CurrentCharacter;
        _api = gameObject.AddComponent<ApiClient>();

        // 저장된 골드 복원 (던전 수익이 있으면 그 값 유지)
        GameState.Gold = Math.Max(GameState.Gold, _ch.gold);

        // 인벤토리 → 슬롯 수량 초기화
        if (_ch.inventory != null)
        {
            for (int i = 0; i < ItemDefs.Length; i++)
            {
                var item = _ch.inventory.Find(x => x.name == ItemDefs[i].name);
                _bagQty[i] = item != null ? item.qty : 0;
            }
        }

        // 퀘스트 진행도 복원 (GameState와 Firebase 중 더 큰 값 사용)
        if (_ch.questProgress != null)
        {
            GameState.DungeonCount = Math.Max(GameState.DungeonCount, _ch.questProgress.dungeonCount);
            GameState.MonsterCount = Math.Max(GameState.MonsterCount, _ch.questProgress.monsterCount);
            GameState.BossCount    = Math.Max(GameState.BossCount,    _ch.questProgress.bossCount);
        }

        // 출석 체크 복원 (오늘 날짜와 비교)
        _attendanceCollected = (_ch.lastAttendanceDate == DateTime.Now.ToString("yyyy-MM-dd"));

        // 원정 목록 초기화
        if (_ch.expeditions == null) _ch.expeditions = new List<ExpeditionState>();

        // XP / Level 복원
        GameState.Xp    = Math.Max(GameState.Xp, _ch.xp);
        GameState.Level = GameState.LevelFromXp(GameState.Xp);
        _ch.xp    = GameState.Xp;
        _ch.level = GameState.Level;

        // 스킬 초기화
        if (_ch.learnedSkills == null) _ch.learnedSkills = new List<SkillData>();
        if (_ch.learnedSkills.Count == 0)
        {
            if (_ch.generated.uniqueSkill != null)
                _ch.learnedSkills.Add(_ch.generated.uniqueSkill);
            for (int lv = 2; lv <= GameState.Level; lv++)
            {
                var sk = GameState.GetSkillForLevel(lv, _ch);
                if (sk != null) _ch.learnedSkills.Add(sk);
            }
        }

        // 손상된 스킬 데이터(이름 없음) 제거 후 필요 시 재저장
        int beforeCount = _ch.learnedSkills.Count;
        _ch.learnedSkills.RemoveAll(s => s == null || string.IsNullOrEmpty(s.name));
        if (_ch.learnedSkills.Count != beforeCount) SaveState();

        SetupCamera();
        SetupWorld();
        SetupUI();

        // GameScene에서 스토리가 갱신된 채로 돌아온 경우 알림
        if (GameState.StoryUpdated)
        {
            GameState.StoryUpdated = false;
            StartCoroutine(ShowStoryToastRoutine($"{_ch.generated.name}의 이야기가 갱신되었습니다"));
        }
    }

    // 인벤토리 + 골드를 Firebase에 저장
    private void SaveState(Action onDone = null)
    {
        if (_ch == null || _api == null) { onDone?.Invoke(); return; }

        if (_ch.inventory == null) _ch.inventory = new System.Collections.Generic.List<InventoryItem>();

        for (int i = 0; i < ItemDefs.Length; i++)
        {
            var existing = _ch.inventory.Find(x => x.name == ItemDefs[i].name);
            if (_bagQty[i] > 0)
            {
                if (existing != null) existing.qty = _bagQty[i];
                else _ch.inventory.Add(new InventoryItem
                {
                    name      = ItemDefs[i].name,
                    qty       = _bagQty[i],
                    itemType  = ItemDefs[i].type,
                    sellPrice = ItemDefs[i].sell,
                });
            }
            else
            {
                if (existing != null) _ch.inventory.Remove(existing);
            }
        }

        // 퀘스트 진행도 동기화
        if (_ch.questProgress == null) _ch.questProgress = new QuestProgress();
        _ch.questProgress.dungeonCount = GameState.DungeonCount;
        _ch.questProgress.monsterCount = GameState.MonsterCount;
        _ch.questProgress.bossCount    = GameState.BossCount;

        _ch.gold  = GameState.Gold;
        _ch.xp    = GameState.Xp;
        _ch.level = GameState.Level;
        if (_ch.learnedSkills == null) _ch.learnedSkills = new List<SkillData>();
        StartCoroutine(_api.UpdateCharacterState(_ch, onDone, err => onDone?.Invoke()));
    }

    // ── 카메라 ──────────────────────────────────────────────────
    private void SetupCamera()
    {
        Camera cam;
        if (Camera.main != null)
        {
            cam = Camera.main;
        }
        else
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.orthographic          = true;
        cam.orthographicSize      = 4.5f;
        cam.clearFlags            = CameraClearFlags.SolidColor;
        cam.backgroundColor       = new Color(0.04f, 0.06f, 0.12f);
        cam.transform.position    = new Vector3(0f, 0.3f, -10f);
    }

    // ── 월드 (배경 + 캐릭터 대형 표시) ──────────────────────────
    private void SetupWorld()
    {
        var sq = MakeSquare();

        // 하늘 (전체 배경)
        Decor(sq, new Vector3(0f, 0f, 5f), new Vector3(20f, 20f, 1f),
              new Color(0.04f, 0.06f, 0.13f), -3);

        // 캐릭터 뒤 연보라 글로우
        Decor(sq, new Vector3(0f, 0.8f, 2f), new Vector3(2.5f, 6.0f, 1f),
              new Color(0.12f, 0.08f, 0.28f, 0.30f), -2);

        // 지면
        Decor(sq, new Vector3(0f, -3.0f, 1f), new Vector3(20f, 1.2f, 1f),
              new Color(0.08f, 0.18f, 0.07f), -1);

        // 발 아래 그림자
        Decor(sq, new Vector3(0f, -2.85f, 0.5f), new Vector3(2.5f, 0.10f, 1f),
              new Color(0f, 0f, 0f, 0.40f), 0);

        // 캐릭터 (전신이 보이도록 스케일 조정)
        int idx = _ch.generated.characterIndex;
        if (idx < 1 || idx > 8) idx = 1;
        var heroGO = new GameObject("VillageHero");
        heroGO.transform.position = new Vector3(0f, -0.3f, 0f);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(heroGO.transform, false);
        visual.transform.localScale = Vector3.one * 2.0f;
        LayerLabCharacter.AttachPlayer(visual, idx).SetWalking(false);
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
        BuildBottomBar();

        // 팝업 미리 생성 (비활성 상태)
        _shopPanel    = BuildShopPopup();
        _missionPanel = BuildMissionPopup();
        _mailPanel    = BuildMailPopup();
        _infoPanel    = BuildInfoPopup();
        _bagPanel        = BuildBagPopup();
        _eventPanel      = BuildEventPopup();
        _expeditionPanel = BuildExpeditionPopup();
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
        _levelText = UIHelper.CreateText(lv.transform, $"Lv.{CalcLevel()}", 24, Vector2.zero, Vector2.one);

        // HP 바
        var hpBG = UIHelper.CreatePanel(t, new Color(0.28f, 0.05f, 0.05f), "HPBG");
        UIHelper.SetAnchors(hpBG.GetComponent<RectTransform>(),
            new Vector2(0.14f, 0.950f), new Vector2(0.47f, 0.990f));
        var hpFill = UIHelper.CreatePanel(hpBG.transform, new Color(0.85f, 0.14f, 0.14f), "HPFill");
        UIHelper.SetAnchors(hpFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        _hpTxt = UIHelper.CreateText(hpBG.transform, $"HP  {GameState.CurrentHp}/{_ch.generated.stats.hp}",
            20, new Vector2(0.03f, 0f), Vector2.one, TextAlignmentOptions.Left);
        _hpTxt.color = new Color(1f, 0.8f, 0.8f);

        // ATK
        var atkBG = UIHelper.CreatePanel(t, new Color(0.55f, 0.26f, 0.04f), "AtkBG");
        UIHelper.SetAnchors(atkBG.GetComponent<RectTransform>(),
            new Vector2(0.49f, 0.950f), new Vector2(0.63f, 0.990f));
        _atkTxt = UIHelper.CreateText(atkBG.transform, $"ATK  {_ch.generated.stats.atk + _ch.bonusAtk}", 20,
            Vector2.zero, Vector2.one);
        _atkTxt.color = new Color(1f, 0.85f, 0.5f);

        // 골드
        var goldBG = UIHelper.CreatePanel(t, new Color(0.40f, 0.30f, 0.02f), "GoldBG");
        UIHelper.SetAnchors(goldBG.GetComponent<RectTransform>(),
            new Vector2(0.65f, 0.950f), new Vector2(0.82f, 0.990f));
        _goldText = UIHelper.CreateText(goldBG.transform, $"G  {GameState.Gold}", 20,
            Vector2.zero, Vector2.one);
        _goldText.color = new Color(1f, 0.88f, 0.20f);

        // MP 바
        var mpBG = UIHelper.CreatePanel(t, new Color(0.05f, 0.07f, 0.28f), "MPBG");
        UIHelper.SetAnchors(mpBG.GetComponent<RectTransform>(),
            new Vector2(0.84f, 0.950f), new Vector2(0.99f, 0.990f));
        var mpFill = UIHelper.CreatePanel(mpBG.transform, new Color(0.14f, 0.32f, 0.85f), "MPFill");
        UIHelper.SetAnchors(mpFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        _mpTxt = UIHelper.CreateText(mpBG.transform, $"MP  {GameState.CurrentMp}/{_ch.generated.stats.mp}",
            20, new Vector2(0.03f, 0f), Vector2.one, TextAlignmentOptions.Left);
        _mpTxt.color = new Color(0.7f, 0.8f, 1f);

        // 구분선
        var line = UIHelper.CreatePanel(t, new Color(0.5f, 0.28f, 0.9f, 0.55f), "TopLine");
        UIHelper.SetAnchors(line.GetComponent<RectTransform>(),
            new Vector2(0f, 0.926f), new Vector2(1f, 0.929f));
    }

    private int CalcLevel() => GameState.Level;

    private void GainXp(int amount)
    {
        int oldLevel = GameState.Level;
        GameState.Xp += amount;
        GameState.Level = GameState.LevelFromXp(GameState.Xp);
        _ch.xp    = GameState.Xp;
        _ch.level = GameState.Level;
        if (_levelText != null) _levelText.text = $"Lv.{GameState.Level}";

        for (int lv = oldLevel + 1; lv <= GameState.Level; lv++)
        {
            var skill = GameState.GetSkillForLevel(lv, _ch);
            if (skill != null)
            {
                if (_ch.learnedSkills == null) _ch.learnedSkills = new List<SkillData>();
                _ch.learnedSkills.Add(skill);
            }
            StartCoroutine(ShowLevelUpBanner(lv, skill));
        }
    }

    private IEnumerator ShowLevelUpBanner(int newLevel, SkillData newSkill)
    {
        var panel = new GameObject("LvBanner");
        panel.transform.SetParent(_canvasTF, false);
        panel.transform.SetAsLastSibling();
        var border = panel.AddComponent<Image>();
        border.color = new Color(0.80f, 0.65f, 0.10f, 0.85f);

        var inner = UIHelper.CreatePanel(panel.transform, new Color(0.05f, 0.03f, 0.16f, 1f), "I");
        UIHelper.SetAnchors(inner.GetComponent<RectTransform>(), new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.94f));

        UIHelper.CreateText(inner.transform, "LEVEL  UP!", 26,
            new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.97f))
            .color = new Color(1f, 0.85f, 0.20f);
        UIHelper.CreateText(inner.transform, $"Lv. {newLevel}", 38,
            new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.66f))
            .color = Color.white;
        if (newSkill != null)
        {
            string skillType = newSkill.isPassive ? "패시브 스킬" : "액티브 스킬";
            UIHelper.CreateText(inner.transform, $"{skillType}  '{newSkill.name}'  획득!", 20,
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.31f))
                .color = new Color(0.5f, 0.9f, 1f);
        }

        var rt = panel.GetComponent<RectTransform>();
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            float a = Mathf.SmoothStep(0, 1, t / 0.35f);
            rt.anchorMin = new Vector2(Mathf.Lerp(1.02f, 0.53f, a), 0.38f);
            rt.anchorMax = new Vector2(Mathf.Lerp(1.50f, 1.01f, a), 0.62f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            yield return null;
        }
        rt.anchorMin = new Vector2(0.53f, 0.38f); rt.anchorMax = new Vector2(1.01f, 0.62f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        yield return new WaitForSeconds(3.0f);
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            float a = Mathf.SmoothStep(0, 1, t / 0.35f);
            rt.anchorMin = new Vector2(Mathf.Lerp(0.53f, 1.02f, a), 0.38f);
            rt.anchorMax = new Vector2(Mathf.Lerp(1.01f, 1.50f, a), 0.62f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            yield return null;
        }
        Destroy(panel);
    }

    private void RefreshGold()
    {
        if (_goldText != null) _goldText.text = $"G  {GameState.Gold}";
    }

    private void AddToBag(int slotIdx, int amount)
    {
        _bagQty[slotIdx] += amount;
        if (_bagQtyTexts[slotIdx] != null) _bagQtyTexts[slotIdx].text = $"x {_bagQty[slotIdx]}";
        if (_bagRows[slotIdx] != null) _bagRows[slotIdx].SetActive(true);
        RefreshBagLayout();
        SaveState();
    }

    private void RefreshBagLayout()
    {
        int visible = 0;
        for (int i = 0; i < _bagRows.Length; i++)
        {
            if (_bagRows[i] == null || !_bagRows[i].activeSelf) continue;
            float y2 = 0.84f - visible * 0.17f;
            float y1 = y2 - 0.14f;
            UIHelper.SetAnchors(_bagRows[i].GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));
            visible++;
        }
    }

    private void AddStoryEntry(string text)
    {
        if (_ch.storyLog == null) _ch.storyLog = new List<string>();
        _ch.storyLog.Add($"[{DateTime.Now:yyyy.MM.dd}] {text}");
        GameState.StoryUpdated = true;
        SaveState();
        StartCoroutine(ShowStoryToastRoutine($"{_ch.generated.name}의 이야기가 갱신되었습니다"));
    }

    private IEnumerator ShowStoryToastRoutine(string msg)
    {
        var panel = new GameObject("StoryToast");
        panel.transform.SetParent(_canvasTF, false);
        panel.transform.SetAsLastSibling();
        panel.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.18f, 0f);
        UIHelper.SetAnchors(panel.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.84f));
        var cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        var txt = UIHelper.CreateText(panel.transform, msg, 26,
            new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));
        txt.color = new Color(1f, 0.88f, 0.40f);

        for (float t = 0; t < 0.25f; t += Time.deltaTime) { cg.alpha = t / 0.25f; yield return null; }
        cg.alpha = 1f;
        yield return new WaitForSeconds(2.2f);
        for (float t = 0; t < 0.4f; t += Time.deltaTime) { cg.alpha = 1f - t / 0.4f; yield return null; }
        Destroy(panel);
    }

    private const int NovelCharsPerPage = 450;

    // 너무 긴 소설을 문단/문장 경계 기준으로 페이지 단위로 나눈다
    private static List<string> SplitNovelIntoPages(string text, int maxChars)
    {
        var pages = new List<string>();
        if (string.IsNullOrEmpty(text)) { pages.Add(""); return pages; }

        var paragraphs = text.Replace("\r\n", "\n").Split('\n');
        var current = new System.Text.StringBuilder();

        void FlushIfAny()
        {
            if (current.Length > 0) { pages.Add(current.ToString().Trim()); current.Clear(); }
        }

        foreach (var para in paragraphs)
        {
            if (para.Length == 0) continue;

            if (para.Length > maxChars)
            {
                // 문단 자체가 너무 길면 문장 단위(. / 다. / 요.)로 쪼개서 채운다
                int start = 0;
                while (start < para.Length)
                {
                    int take = Mathf.Min(maxChars - current.Length, para.Length - start);
                    if (take <= 0) { FlushIfAny(); continue; }
                    current.Append(para.Substring(start, take));
                    start += take;
                    if (current.Length >= maxChars) FlushIfAny();
                }
                current.Append('\n');
                continue;
            }

            if (current.Length + para.Length > maxChars) FlushIfAny();
            current.Append(para).Append('\n');
        }
        FlushIfAny();

        return pages.Count > 0 ? pages : new List<string> { text };
    }

    private void OpenNovelView()
    {
        var overlay = UIHelper.CreatePanel(_canvasTF, new Color(0f, 0f, 0f, 0.97f), "NovelOverlay");
        UIHelper.Stretch(overlay.GetComponent<RectTransform>());
        overlay.transform.SetAsLastSibling();
        var nt = overlay.transform;

        var title = UIHelper.CreateText(nt, _ch.generated.name + "의 이야기", 36,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));
        title.color     = new Color(1f, 0.85f, 0.4f);
        title.fontStyle = FontStyles.Bold;

        var loadingTxt = UIHelper.CreateText(nt, "소설을 생성하는 중...", 28,
            new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.56f));
        loadingTxt.color = new Color(0.70f, 0.70f, 0.90f);

        var novelTxt = UIHelper.CreateText(nt, "", 22,
            new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.86f), TextAlignmentOptions.TopLeft);
        novelTxt.color              = new Color(0.88f, 0.88f, 0.98f);
        novelTxt.enableWordWrapping = true;
        novelTxt.gameObject.SetActive(false);

        var pageTxt = UIHelper.CreateText(nt, "", 18,
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.135f), TextAlignmentOptions.Center);
        pageTxt.color = new Color(0.6f, 0.6f, 0.8f);
        pageTxt.gameObject.SetActive(false);

        var prevBtn = UIHelper.CreateButton(nt, "◀ 이전",
            new Vector2(0.04f, 0.085f), new Vector2(0.24f, 0.135f), new Color(0.18f, 0.18f, 0.30f));
        var nextBtn = UIHelper.CreateButton(nt, "다음 ▶",
            new Vector2(0.76f, 0.085f), new Vector2(0.96f, 0.135f), new Color(0.18f, 0.18f, 0.30f));
        prevBtn.gameObject.SetActive(false);
        nextBtn.gameObject.SetActive(false);

        var closeBtn = UIHelper.CreateButton(nt, "닫기",
            new Vector2(0.20f, 0.01f), new Vector2(0.80f, 0.07f),
            new Color(0.30f, 0.10f, 0.10f));
        closeBtn.onClick.AddListener(() => Destroy(overlay));

        StartCoroutine(_api.GenerateNovel(_ch,
            novel =>
            {
                loadingTxt.gameObject.SetActive(false);

                var pages = SplitNovelIntoPages(novel, NovelCharsPerPage);
                int pageIdx = 0;

                void Render()
                {
                    novelTxt.text = pages[pageIdx];
                    pageTxt.text  = $"{pageIdx + 1} / {pages.Count}";
                    prevBtn.interactable = pageIdx > 0;
                    nextBtn.interactable = pageIdx < pages.Count - 1;
                }

                novelTxt.gameObject.SetActive(true);
                if (pages.Count > 1)
                {
                    pageTxt.gameObject.SetActive(true);
                    prevBtn.gameObject.SetActive(true);
                    nextBtn.gameObject.SetActive(true);
                    prevBtn.onClick.AddListener(() => { if (pageIdx > 0) { pageIdx--; Render(); } });
                    nextBtn.onClick.AddListener(() => { if (pageIdx < pages.Count - 1) { pageIdx++; Render(); } });
                }
                Render();
            },
            err =>
            {
                loadingTxt.text  = "오류: " + err;
                loadingTxt.color = new Color(1f, 0.4f, 0.4f);
            }));
    }

    private void ShowMsg(string msg) => StartCoroutine(ShowMsgRoutine(msg));

    private IEnumerator ShowMsgRoutine(string msg)
    {
        var toast = new GameObject("Toast");
        toast.transform.SetParent(_canvasTF, false);
        toast.transform.SetAsLastSibling();
        UIHelper.SetAnchors(toast.AddComponent<RectTransform>(),
            new Vector2(0.10f, 0.43f), new Vector2(0.90f, 0.57f));
        toast.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.92f);
        var cg = toast.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        UIHelper.CreateText(toast.transform, msg, 28,
            new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f)).color = Color.white;
        float t = 0f;
        while (t < 0.2f) { t += Time.deltaTime; cg.alpha = t / 0.2f; yield return null; }
        cg.alpha = 1f;
        yield return new WaitForSeconds(1.2f);
        t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; cg.alpha = 1f - t / 0.3f; yield return null; }
        Destroy(toast);
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

        var settingsBtn = UIHelper.CreateButton(t, "설정",
            new Vector2(0.878f, 0.881f), new Vector2(0.994f, 0.922f),
            new Color(0.18f, 0.14f, 0.30f));
        settingsBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }

    // ── 좌우 아이콘 버튼 ────────────────────────────────────────
    private void BuildSideIcons()
    {
        var t = _canvasTF;

        // 좌측 3개
        MakeSideIcon(t, "가방", new Vector2(0.01f, 0.768f), new Vector2(0.12f, 0.868f),
            new Color(0.12f, 0.09f, 0.24f), () => TogglePanel(_bagPanel));
        MakeSideIcon(t, "이벤트", new Vector2(0.01f, 0.648f), new Vector2(0.12f, 0.748f),
            new Color(0.10f, 0.18f, 0.12f), () => TogglePanel(_eventPanel));
        MakeSideIcon(t, "미션", new Vector2(0.01f, 0.528f), new Vector2(0.12f, 0.628f),
            new Color(0.12f, 0.09f, 0.24f), () => { RefreshMissionPopup(); TogglePanel(_missionPanel); });

        // 우측 — 우편
        MakeSideIcon(t, "우편", new Vector2(0.88f, 0.768f), new Vector2(0.99f, 0.868f),
            new Color(0.12f, 0.09f, 0.24f), () => TogglePanel(_mailPanel));

        // 알림 배지 (빨간 점)
        var badge = UIHelper.CreatePanel(t, new Color(0.88f, 0.14f, 0.14f), "MailBadge");
        UIHelper.SetAnchors(badge.GetComponent<RectTransform>(),
            new Vector2(0.944f, 0.852f), new Vector2(0.992f, 0.876f));
        UIHelper.CreateText(badge.transform, "1", 14, Vector2.zero, Vector2.one);

        // 우측 — 캐릭터 정보 (매번 재빌드하여 레벨/스킬 최신 상태 반영)
        MakeSideIcon(t, "정보", new Vector2(0.88f, 0.648f), new Vector2(0.99f, 0.748f),
            new Color(0.10f, 0.18f, 0.28f), () =>
            {
                if (_infoPanel != null) { _infoPanel.SetActive(false); Destroy(_infoPanel); }
                _infoPanel = BuildInfoPopup();
                TogglePanel(_infoPanel);
            });

        // 우측 — 상점 (정보 아래)
        MakeSideIcon(t, "상점", new Vector2(0.88f, 0.528f), new Vector2(0.99f, 0.628f),
            new Color(0.12f, 0.09f, 0.24f), () => TogglePanel(_shopPanel));
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

    // ── 마을 이름 (진입 연출: 페이드인 → 유지 → 페이드아웃) ─────
    private void BuildVillageName()
    {
        // 전체를 하나의 CanvasGroup으로 감싸 fade 처리
        var container = new GameObject("VillageNameFade");
        container.transform.SetParent(_canvasTF, false);
        UIHelper.Stretch(container.AddComponent<RectTransform>());
        var cg = container.AddComponent<CanvasGroup>();
        cg.alpha           = 0f;
        cg.blocksRaycasts  = false;  // 클릭 투과
        var ct = container.transform;

        var title = UIHelper.CreateText(ct, $"{_ch.generated.name}의 마을", 54,
            new Vector2(0.05f, 0.615f), new Vector2(0.95f, 0.745f));
        title.color         = new Color(1f, 0.92f, 0.55f);
        title.fontStyle     = FontStyles.Bold;
        title.raycastTarget = false;
        var ol = title.gameObject.AddComponent<Outline>();
        ol.effectColor    = new Color(0.35f, 0.08f, 0.0f, 0.85f);
        ol.effectDistance = new Vector2(2, -2);

        var sub = UIHelper.CreateText(ct, "— 안전 구역 —", 28,
            new Vector2(0.15f, 0.578f), new Vector2(0.85f, 0.612f));
        sub.color         = new Color(0.65f, 0.85f, 0.65f, 0.9f);
        sub.raycastTarget = false;

        var lineL = UIHelper.CreatePanel(ct, new Color(0.6f, 0.45f, 0.10f, 0.7f), "LineL");
        UIHelper.SetAnchors(lineL.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.626f), new Vector2(0.28f, 0.630f));
        lineL.GetComponent<Image>().raycastTarget = false;

        var lineR = UIHelper.CreatePanel(ct, new Color(0.6f, 0.45f, 0.10f, 0.7f), "LineR");
        UIHelper.SetAnchors(lineR.GetComponent<RectTransform>(),
            new Vector2(0.72f, 0.626f), new Vector2(0.95f, 0.630f));
        lineR.GetComponent<Image>().raycastTarget = false;

        StartCoroutine(FadeVillageName(cg));
    }

    private IEnumerator FadeVillageName(CanvasGroup cg)
    {
        // 페이드 인 (0.6초)
        float t = 0f;
        while (t < 0.6f) { t += Time.deltaTime; cg.alpha = t / 0.6f; yield return null; }
        cg.alpha = 1f;

        // 유지 (2.5초)
        yield return new WaitForSeconds(2.5f);

        // 페이드 아웃 (1초)
        t = 0f;
        while (t < 1f) { t += Time.deltaTime; cg.alpha = 1f - t; yield return null; }
        cg.gameObject.SetActive(false);
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

        var shopBtn = UIHelper.CreateButton(t, "상점",
            new Vector2(0.02f, 0.273f), new Vector2(0.48f, 0.374f),
            new Color(0.50f, 0.36f, 0.04f));
        shopBtn.onClick.AddListener(() => TogglePanel(_shopPanel));

        var dungBtn = UIHelper.CreateButton(t, "던전",
            new Vector2(0.52f, 0.273f), new Vector2(0.98f, 0.374f),
            new Color(0.58f, 0.08f, 0.08f));
        dungBtn.onClick.AddListener(() => SceneManager.LoadScene("GameScene"));
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

        var dungeonBtn = UIHelper.CreateButton(t, "던전 입장!",
            new Vector2(0.32f, 0.183f), new Vector2(0.68f, 0.260f),
            new Color(0.68f, 0.38f, 0.04f));
        dungeonBtn.onClick.AddListener(() =>
        {
            GameState.DungeonCount++;
            SaveState(() => SceneManager.LoadScene("GameScene"));
        });
        var dTmp = dungeonBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (dTmp != null) dTmp.fontSize = 42;

        var expBtn = UIHelper.CreateButton(t, "원정 →",
            new Vector2(0.70f, 0.188f), new Vector2(0.98f, 0.256f),
            new Color(0.10f, 0.30f, 0.42f));
        expBtn.onClick.AddListener(() =>
        {
            if (_expeditionPanel != null) { _expeditionPanel.SetActive(false); Destroy(_expeditionPanel); }
            _expeditionPanel = BuildExpeditionPopup();
            TogglePanel(_expeditionPanel);
        });

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

        var closeBtn = UIHelper.CreateButton(panel.transform, "X",
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

        var items = new (string name, int price, Color strip, int bagIdx)[]
        {
            ("회복 포션",    500,  new Color(0.7f, 0.1f, 0.1f),   2),
            ("마나 포션",    300,  new Color(0.1f, 0.2f, 0.7f),   3),
            ("방어구 강화", 1000,  new Color(0.3f, 0.3f, 0.1f),  -1),
            ("무기 강화",   1500,  new Color(0.5f, 0.2f, 0.05f), -1),
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
                Vector2.zero, new Vector2(0.04f, 1f));

            UIHelper.CreateText(row.transform, it.name, 24,
                new Vector2(0.07f, 0.1f), new Vector2(0.50f, 0.9f), TextAlignmentOptions.Left)
                .color = Color.white;

            UIHelper.CreateText(row.transform, $"{it.price} G", 22,
                new Vector2(0.51f, 0.1f), new Vector2(0.72f, 0.9f), TextAlignmentOptions.Right)
                .color = new Color(1f, 0.85f, 0.3f);

            int capturedPrice = it.price;
            string capturedName = it.name;
            int capturedBagIdx = it.bagIdx;
            var buyBtn = UIHelper.CreateButton(row.transform, "구매",
                new Vector2(0.74f, 0.12f), new Vector2(0.98f, 0.88f),
                new Color(0.18f, 0.42f, 0.18f));
            var buyTmp = buyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (buyTmp != null) buyTmp.fontSize = 22;
            buyBtn.onClick.AddListener(() =>
            {
                if (GameState.Gold >= capturedPrice)
                {
                    GameState.Gold -= capturedPrice;
                    RefreshGold();
                    if (capturedBagIdx >= 0)
                    {
                        ShowMsg($"{capturedName} 구매 완료!");
                        AddToBag(capturedBagIdx, 1);
                    }
                    else
                    {
                        if (capturedName == "방어구 강화")
                        {
                            _ch.bonusDef += 5;
                            ShowMsg("방어구 강화 완료! DEF +5");
                        }
                        else if (capturedName == "무기 강화")
                        {
                            _ch.bonusAtk += 8;
                            ShowMsg("무기 강화 완료! ATK +8");
                            if (_atkTxt != null) _atkTxt.text = $"ATK  {_ch.generated.stats.atk + _ch.bonusAtk}";
                        }
                        SaveState();
                    }
                    AddStoryEntry($"{_ch.generated.name}은(는) 마을 상점에서 {capturedName}을(를) 구입했다.");
                }
                else ShowMsg("골드가 부족합니다");
            });
        }

        return overlay;
    }

    // ── 미션 팝업 ────────────────────────────────────────────────
    private GameObject BuildMissionPopup()
    {
        var overlay = BuildPopupShell("미션", out Transform tf);

        var missionDefs = new (string tag, string name)[]
        {
            ("[주]", "던전 1회 입장하기"),
            ("[일]", "던전 3회 입장하기"),
            ("[일]", "몬스터 10마리 처치"),
            ("[일]", "골드 500 획득하기"),
        };

        for (int i = 0; i < missionDefs.Length; i++)
        {
            float y2 = 0.84f - i * 0.19f;
            float y1 = y2 - 0.16f;
            var m = missionDefs[i];
            int capturedIdx = i;

            var row = UIHelper.CreatePanel(tf, new Color(0.08f, 0.10f, 0.18f), $"M{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));

            Color tagCol = m.tag == "[주]" ? new Color(0.60f, 0.10f, 0.10f)
                                           : new Color(0.18f, 0.30f, 0.55f);
            var tagPanel = UIHelper.CreatePanel(row.transform, tagCol, "Tag");
            UIHelper.SetAnchors(tagPanel.GetComponent<RectTransform>(),
                new Vector2(0f, 0.1f), new Vector2(0.17f, 0.9f));
            UIHelper.CreateText(tagPanel.transform, m.tag, 18, Vector2.zero, Vector2.one)
                .color = Color.white;

            UIHelper.CreateText(row.transform, m.name, 22,
                new Vector2(0.19f, 0.50f), new Vector2(0.97f, 0.94f), TextAlignmentOptions.Left)
                .color = Color.white;

            var progT = UIHelper.CreateText(row.transform, "0 / 0", 20,
                new Vector2(0.19f, 0.05f), new Vector2(0.72f, 0.46f), TextAlignmentOptions.Left);
            progT.color = new Color(0.75f, 0.75f, 0.90f);
            _missionProgTexts[capturedIdx] = progT;

            var rewBtn = UIHelper.CreateButton(row.transform, "보상 수령",
                new Vector2(0.74f, 0.08f), new Vector2(0.97f, 0.46f),
                new Color(0.18f, 0.45f, 0.18f));
            var rewTmp = rewBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (rewTmp != null) rewTmp.fontSize = 18;
            rewBtn.gameObject.SetActive(false);
            _missionRewBtns[capturedIdx] = rewBtn;

            string capturedMissionName = m.name;
            rewBtn.onClick.AddListener(() =>
            {
                if (GameState.MissionRewarded[capturedIdx]) return;
                GameState.MissionRewarded[capturedIdx] = true;
                GameState.Gold += _missionRewards[capturedIdx];
                RefreshGold();
                if (capturedIdx == 0)
                {
                    int targetLevel = GameState.Level + _missionLevelUpReward;
                    int xpNeeded = GameState.TotalXpForLevel(targetLevel) - GameState.Xp;
                    GainXp(xpNeeded);
                    ShowMsg($"미션 보상 획득! +{_missionRewards[capturedIdx]}G  레벨 +{_missionLevelUpReward}");
                }
                else
                {
                    GainXp(_missionXpRewards[capturedIdx]);
                    ShowMsg($"미션 보상 획득! +{_missionRewards[capturedIdx]}G  +{_missionXpRewards[capturedIdx]}XP");
                }
                if (rewTmp != null) rewTmp.text = "수령됨";
                rewBtn.image.color  = new Color(0.25f, 0.25f, 0.32f);
                rewBtn.interactable = false;
                SaveState();
                AddStoryEntry($"{_ch.generated.name}은(는) '{capturedMissionName}' 임무를 완수하고 보상을 받았다.");
            });
        }

        return overlay;
    }

    private void RefreshMissionPopup()
    {
        int[] currents = { GameState.DungeonCount, GameState.DungeonCount, GameState.MonsterCount, GameState.Gold };
        for (int i = 0; i < 4; i++)
        {
            if (_missionProgTexts[i] == null) continue;
            int  cur  = Mathf.Min(currents[i], _missionTargets[i]);
            bool done = currents[i] >= _missionTargets[i];
            _missionProgTexts[i].text  = $"{cur} / {_missionTargets[i]}";
            _missionProgTexts[i].color = done ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.75f, 0.75f, 0.90f);

            if (_missionRewBtns[i] == null) continue;
            _missionRewBtns[i].gameObject.SetActive(done);
            if (done && !GameState.MissionRewarded[i])
            {
                _missionRewBtns[i].interactable = true;
                _missionRewBtns[i].image.color  = new Color(0.18f, 0.45f, 0.18f);
                var tmp = _missionRewBtns[i].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "보상 수령";
            }
        }
    }

    // ── 우편함 팝업 (목록 → 상세 네비게이션) ────────────────────
    private GameObject BuildMailPopup()
    {
        var overlay = BuildPopupShell("우편함", out Transform tf);

        string story = _ch.generated.story ?? "이 모험가의 이야기는 아직 쓰여지지 않았습니다.";
        string locTitle   = "탐험 지역";
        string locContent = "탐험할 지역 정보가 없습니다.";
        if (_ch.generated.locations != null && _ch.generated.locations.Count > 0)
        {
            locTitle   = _ch.generated.locations[0].name ?? "탐험 지역";
            locContent = _ch.generated.locations[0].description ?? "";
        }

        var mails = new (string tag, Color tagCol, string title, string content, int goldReward)[]
        {
            ("공지", new Color(0.45f, 0.10f, 0.10f),
             "정기 점검 안내",
             "매주 화요일 04:00~06:00 서버 점검이 진행됩니다.\n점검 중에는 접속이 불가합니다.\n점검 보상: 골드 200G",
             200),
            ("보상", new Color(0.14f, 0.40f, 0.18f),
             "v1.2 패치 보상",
             "v1.2 업데이트를 기념하여 보상이 지급됩니다.\n골드 1,000G + 회복 포션 x5\n7일 이내에 수령하세요.",
             1000),
            ("편지", new Color(0.25f, 0.18f, 0.50f),
             _ch.generated.name + "의 이야기",
             story,
             0),
            ("지도", new Color(0.10f, 0.25f, 0.50f),
             locTitle,
             locContent,
             0),
        };

        bool[] rewarded   = new bool[4];
        int[]  selectedIdx = { -1 };
        var    mailRowGOs  = new GameObject[4];

        // ── 목록 행 (tf 직접 자식) ─────────────────────────────
        for (int i = 0; i < mails.Length; i++)
        {
            float y2 = 0.84f - i * 0.185f;
            float y1 = y2 - 0.155f;
            var m = mails[i];
            int capturedIdx = i;

            var row = UIHelper.CreatePanel(tf, new Color(0.08f, 0.06f, 0.20f), $"Mail{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));
            mailRowGOs[capturedIdx] = row;

            var tagPanel = UIHelper.CreatePanel(row.transform, m.tagCol, "Tag");
            UIHelper.SetAnchors(tagPanel.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(0.18f, 1f));
            var tagTxt = UIHelper.CreateText(tagPanel.transform, m.tag, 30, Vector2.zero, Vector2.one);
            tagTxt.color     = Color.white;
            tagTxt.fontStyle = FontStyles.Bold;

            var titleTxt = UIHelper.CreateText(row.transform, m.title, 34,
                new Vector2(0.20f, 0.48f), new Vector2(0.97f, 0.95f), TextAlignmentOptions.Left);
            titleTxt.color     = new Color(1f, 0.93f, 0.60f);
            titleTxt.fontStyle = FontStyles.Bold;

            string preview = m.content.Replace("\n", " ");
            if (preview.Length > 26) preview = preview.Substring(0, 26) + "...";
            UIHelper.CreateText(row.transform, preview, 24,
                new Vector2(0.20f, 0.05f), new Vector2(0.97f, 0.46f), TextAlignmentOptions.Left)
                .color = new Color(0.82f, 0.82f, 0.95f);
        }

        // ── 상세 패널 (행보다 나중에 추가 → 위 z-order) ─────────
        var detailPanel = UIHelper.CreatePanel(tf, new Color(0.06f, 0.05f, 0.14f), "MailDetail");
        UIHelper.SetAnchors(detailPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0.87f));
        detailPanel.SetActive(false);

        var detailTitle = UIHelper.CreateText(detailPanel.transform, "", 40,
            new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.96f), TextAlignmentOptions.Left);
        detailTitle.color     = new Color(1f, 0.93f, 0.58f);
        detailTitle.fontStyle = FontStyles.Bold;

        var detailContent = UIHelper.CreateText(detailPanel.transform, "", 28,
            new Vector2(0.03f, 0.24f), new Vector2(0.97f, 0.80f), TextAlignmentOptions.Left);
        detailContent.color              = new Color(0.90f, 0.90f, 0.98f);
        detailContent.enableWordWrapping = true;

        var detailRewardInfo = UIHelper.CreateText(detailPanel.transform, "", 30,
            new Vector2(0.03f, 0.15f), new Vector2(0.97f, 0.23f), TextAlignmentOptions.Left);
        detailRewardInfo.color = new Color(1f, 0.88f, 0.28f);

        var rewardBtn = UIHelper.CreateButton(detailPanel.transform, "보상 받기",
            new Vector2(0.03f, 0.03f), new Vector2(0.54f, 0.14f),
            new Color(0.18f, 0.48f, 0.18f));
        var rewardTmp = rewardBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (rewardTmp != null) rewardTmp.fontSize = 28;
        rewardBtn.gameObject.SetActive(false);

        rewardBtn.onClick.AddListener(() =>
        {
            int idx = selectedIdx[0];
            if (idx < 0 || rewarded[idx]) return;
            rewarded[idx] = true;
            GameState.Gold += mails[idx].goldReward;
            RefreshGold();
            ShowMsg($"보상 수령! +{mails[idx].goldReward}G");
            if (rewardTmp != null) rewardTmp.text = "수령됨";
            rewardBtn.image.color  = new Color(0.25f, 0.25f, 0.32f);
            rewardBtn.interactable = false;
        });

        var backBtn = UIHelper.CreateButton(detailPanel.transform, "← 목록",
            new Vector2(0.60f, 0.03f), new Vector2(0.97f, 0.14f),
            new Color(0.18f, 0.14f, 0.30f));
        backBtn.onClick.AddListener(() =>
        {
            detailPanel.SetActive(false);
            foreach (var r in mailRowGOs) if (r != null) r.SetActive(true);
        });

        // ── 행 클릭 → 상세보기
        // 히트 버튼을 tf 직계 자식(row의 형제)으로 배치해야
        // row 내부 TMP 텍스트의 raycast 간섭을 피할 수 있음
        for (int i = 0; i < mails.Length; i++)
        {
            float y2 = 0.84f - i * 0.185f;
            float y1 = y2 - 0.155f;
            int capturedIdx = i;

            var hitGO  = new GameObject($"MailBtn{i}");
            hitGO.transform.SetParent(tf, false);
            var hitImg = hitGO.AddComponent<Image>();
            hitImg.color = Color.clear;
            UIHelper.SetAnchors(hitGO.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));
            var rowBtn = hitGO.AddComponent<Button>();
            rowBtn.transition    = Selectable.Transition.None;
            rowBtn.targetGraphic = hitImg;

            rowBtn.onClick.AddListener(() =>
            {
                selectedIdx[0]     = capturedIdx;
                detailTitle.text   = mails[capturedIdx].title;
                detailContent.text = mails[capturedIdx].content;

                bool hasReward = mails[capturedIdx].goldReward > 0;
                rewardBtn.gameObject.SetActive(hasReward);
                if (hasReward)
                {
                    bool alreadyClaimed = rewarded[capturedIdx];
                    if (rewardTmp != null) rewardTmp.text = alreadyClaimed ? "수령됨" : "보상 받기";
                    rewardBtn.image.color  = alreadyClaimed ? new Color(0.25f, 0.25f, 0.32f) : new Color(0.18f, 0.48f, 0.18f);
                    rewardBtn.interactable = !alreadyClaimed;
                    detailRewardInfo.text  = $"보상: 골드 {mails[capturedIdx].goldReward}G";
                }
                else
                {
                    detailRewardInfo.text = "";
                }

                foreach (var r in mailRowGOs) if (r != null) r.SetActive(false);
                detailPanel.SetActive(true);
            });
        }

        // detailPanel을 맨 마지막 자식으로 이동 → 히트 버튼보다 위에 렌더링
        // (detailPanel 활성화 시 히트 버튼 클릭이 뚫리지 않도록)
        detailPanel.transform.SetAsLastSibling();

        return overlay;
    }

    // ── 캐릭터 정보 팝업 ─────────────────────────────────────────
    private GameObject BuildInfoPopup()
    {
        var overlay = BuildPopupShell($"{_ch.generated.name}  Lv.{GameState.Level}", out Transform tf);
        var s = _ch.generated.stats;

        // 스탯 2×2 그리드 (공간 절약)
        var statData = new (string label, int val, Color col)[]
        {
            ("HP",  s.hp,  new Color(0.9f, 0.2f, 0.2f)),
            ("ATK", s.atk, new Color(0.9f, 0.5f, 0.1f)),
            ("DEF", s.def, new Color(0.3f, 0.6f, 0.9f)),
            ("MP",  s.mp,  new Color(0.5f, 0.3f, 0.9f)),
        };
        for (int i = 0; i < statData.Length; i++)
        {
            int r = i / 2; int c = i % 2;
            float x1 = 0.03f + c * 0.485f;
            float x2 = x1 + 0.465f;
            float y2 = 0.875f - r * 0.097f;
            float y1 = y2 - 0.085f;
            var sd = statData[i];

            var row = UIHelper.CreatePanel(tf, new Color(0.10f, 0.09f, 0.20f), $"S{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(x1, y1), new Vector2(x2, y2));

            var lbl = UIHelper.CreateText(row.transform, sd.label, 30,
                new Vector2(0.03f, 0.1f), new Vector2(0.48f, 0.9f), TextAlignmentOptions.Left);
            lbl.color = sd.col; lbl.fontStyle = FontStyles.Bold;

            var val = UIHelper.CreateText(row.transform, sd.val.ToString(), 34,
                new Vector2(0.48f, 0.1f), new Vector2(0.97f, 0.9f), TextAlignmentOptions.Right);
            val.color = Color.white; val.fontStyle = FontStyles.Bold;
        }

        // XP 바 (스탯 바로 아래)
        {
            int curXp   = GameState.Xp;
            int xpNeeds = GameState.TotalXpForLevel(GameState.Level + 1);
            int xpBase  = GameState.TotalXpForLevel(GameState.Level);
            float ratio = xpNeeds > xpBase ? (float)(curXp - xpBase) / (xpNeeds - xpBase) : 1f;
            ratio = Mathf.Clamp01(ratio);

            var xpBG = UIHelper.CreatePanel(tf, new Color(0.10f, 0.08f, 0.20f), "XpBG");
            UIHelper.SetAnchors(xpBG.GetComponent<RectTransform>(),
                new Vector2(0.03f, 0.668f), new Vector2(0.97f, 0.693f));
            var xpFill = UIHelper.CreatePanel(xpBG.transform, new Color(0.28f, 0.72f, 0.28f), "XpFill");
            UIHelper.SetAnchors(xpFill.GetComponent<RectTransform>(),
                Vector2.zero, new Vector2(ratio, 1f));
            var xpTxt = UIHelper.CreateText(xpBG.transform,
                $"XP  {curXp - xpBase} / {xpNeeds - xpBase}", 15,
                new Vector2(0.02f, 0f), Vector2.one, TextAlignmentOptions.Left);
            xpTxt.color = Color.white;
        }

        // ── 보유 스킬 섹션 ──────────────────────────────────────
        var skillSep = UIHelper.CreatePanel(tf, new Color(0.5f, 0.4f, 0.8f, 0.35f), "SkSep");
        UIHelper.SetAnchors(skillSep.GetComponent<RectTransform>(),
            new Vector2(0.03f, 0.657f), new Vector2(0.97f, 0.661f));

        var skills = _ch.learnedSkills ?? new List<SkillData>();
        var skHdr = UIHelper.CreateText(tf, $"보유 스킬  ({skills.Count})", 21,
            new Vector2(0.03f, 0.624f), new Vector2(0.97f, 0.656f), TextAlignmentOptions.Left);
        skHdr.color = new Color(1f, 0.85f, 0.3f); skHdr.fontStyle = FontStyles.Bold;

        float skillBottom;
        if (skills.Count == 0)
        {
            var noSk = UIHelper.CreateText(tf, "스킬 없음  (레벨업 시 해금)", 19,
                new Vector2(0.03f, 0.583f), new Vector2(0.97f, 0.622f), TextAlignmentOptions.Left);
            noSk.color = new Color(0.55f, 0.55f, 0.70f);
            skillBottom = 0.580f;
        }
        else
        {
            SkillData uniqueRef = _ch.generated?.uniqueSkill;
            for (int i = 0; i < skills.Count; i++)
            {
                var sk       = skills[i];
                float y2     = 0.620f - i * 0.092f;
                float y1     = y2 - 0.082f;
                bool isUniq  = uniqueRef != null && sk.name == uniqueRef.name;

                var row = UIHelper.CreatePanel(tf,
                    isUniq ? new Color(0.18f, 0.12f, 0.05f) : new Color(0.06f, 0.09f, 0.18f), $"Sk{i}");
                UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                    new Vector2(0.03f, y1), new Vector2(0.97f, y2));

                // 태그 (고유/일반)
                var tag = UIHelper.CreatePanel(row.transform,
                    isUniq ? new Color(0.65f, 0.40f, 0.04f) : new Color(0.18f, 0.32f, 0.60f), "Tag");
                UIHelper.SetAnchors(tag.GetComponent<RectTransform>(),
                    new Vector2(0f, 0.1f), new Vector2(0.16f, 0.9f));
                UIHelper.CreateText(tag.transform, isUniq ? "고유" : "일반", 16,
                    Vector2.zero, Vector2.one).color = Color.white;

                // 스킬 이름
                var skName = UIHelper.CreateText(row.transform, sk.name, 22,
                    new Vector2(0.18f, 0.50f), new Vector2(0.72f, 0.96f), TextAlignmentOptions.Left);
                skName.color     = isUniq ? new Color(1f, 0.80f, 0.30f) : new Color(0.70f, 0.88f, 1f);
                skName.fontStyle = FontStyles.Bold;

                // 설명
                var skDesc = UIHelper.CreateText(row.transform, sk.description, 16,
                    new Vector2(0.18f, 0.04f), new Vector2(0.72f, 0.48f), TextAlignmentOptions.Left);
                skDesc.color = new Color(0.72f, 0.72f, 0.86f);

                // MP 비용 or 패시브
                string costLbl = sk.isPassive ? "패시브" : $"MP {sk.mpCost}";
                var costTxt = UIHelper.CreateText(row.transform, costLbl, 18,
                    new Vector2(0.73f, 0.1f), new Vector2(0.98f, 0.9f), TextAlignmentOptions.Right);
                costTxt.color = sk.isPassive ? new Color(0.55f, 0.92f, 0.55f) : new Color(0.55f, 0.75f, 1f);
            }
            skillBottom = 0.620f - skills.Count * 0.092f;
        }

        // ── 스토리 + 모험 기록 ──────────────────────────────────
        bool hasLog = _ch.storyLog != null && _ch.storyLog.Count > 0;
        float storyTop = skillBottom - 0.012f;
        float storyBot = hasLog ? 0.245f : 0.03f;

        if (!string.IsNullOrEmpty(_ch.generated.story) && storyTop - storyBot > 0.05f)
        {
            var storyTxt = UIHelper.CreateText(tf, _ch.generated.story,
                hasLog ? 18 : 22,
                new Vector2(0.03f, storyBot), new Vector2(0.97f, storyTop), TextAlignmentOptions.Left);
            storyTxt.color              = new Color(0.82f, 0.82f, 0.96f);
            storyTxt.enableWordWrapping = true;
            storyTxt.overflowMode       = TMPro.TextOverflowModes.Ellipsis;
        }

        if (hasLog)
        {
            var sep = UIHelper.CreatePanel(tf, new Color(0.5f, 0.4f, 0.8f, 0.35f), "LogSep");
            UIHelper.SetAnchors(sep.GetComponent<RectTransform>(),
                new Vector2(0.03f, 0.233f), new Vector2(0.97f, 0.237f));

            var logLabel = UIHelper.CreateText(tf, $"모험 기록  ({_ch.storyLog.Count})",
                22, new Vector2(0.03f, 0.19f), new Vector2(0.65f, 0.232f), TextAlignmentOptions.Left);
            logLabel.color = new Color(1f, 0.85f, 0.3f); logLabel.fontStyle = FontStyles.Bold;

            var novelBtn = UIHelper.CreateButton(tf, "소설로 보기",
                new Vector2(0.66f, 0.187f), new Vector2(0.97f, 0.235f),
                new Color(0.25f, 0.10f, 0.40f));
            var novelBtnTmp = novelBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (novelBtnTmp != null) novelBtnTmp.fontSize = 18;
            novelBtn.onClick.AddListener(OpenNovelView);

            int showCount = Mathf.Min(2, _ch.storyLog.Count);
            for (int i = 0; i < showCount; i++)
            {
                string entry = _ch.storyLog[_ch.storyLog.Count - 1 - i];
                float y2 = 0.185f - i * 0.095f;
                float y1 = y2 - 0.085f;
                var et = UIHelper.CreateText(tf, entry, 17,
                    new Vector2(0.03f, y1), new Vector2(0.97f, y2), TextAlignmentOptions.Left);
                et.color = i == 0 ? new Color(0.78f, 0.84f, 0.98f) : new Color(0.55f, 0.60f, 0.75f);
                et.enableWordWrapping = true;
            }
        }

        return overlay;
    }

    // ── 팝업 토글 ────────────────────────────────────────────────
    private void TogglePanel(GameObject panel)
    {
        bool wasActive = panel != null && panel.activeSelf;
        if (_shopPanel)       _shopPanel.SetActive(false);
        if (_missionPanel)    _missionPanel.SetActive(false);
        if (_mailPanel)       _mailPanel.SetActive(false);
        if (_infoPanel)       _infoPanel.SetActive(false);
        if (_bagPanel)        _bagPanel.SetActive(false);
        if (_eventPanel)      _eventPanel.SetActive(false);
        if (_expeditionPanel) _expeditionPanel.SetActive(false);
        if (!wasActive && panel != null) panel.SetActive(true);
    }

    // ── 원정 팝업 (매번 재빌드하여 최신 상태 반영) ──────────────
    private GameObject BuildExpeditionPopup()
    {
        var overlay = BuildPopupShell("원정", out Transform tf);

        UIHelper.CreateText(tf, "원정대를 파견해 보상을 획득하세요", 22,
            new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.93f))
            .color = new Color(0.70f, 0.82f, 1.0f);

        var slotDefs = new (string place, string reward, Color col, long durMs, int goldReward, int bagItem)[]
        {
            ("어두운 숲", "골드 300G + 재료 아이템", new Color(0.08f, 0.20f, 0.08f),  7_200_000L, 300,  0),
            ("고대 유적", "골드 500G + 장비 파편",   new Color(0.18f, 0.15f, 0.06f), 10_800_000L, 500,  1),
            ("화산 지대", "골드 800G + 희귀 아이템", new Color(0.16f, 0.06f, 0.04f), 14_400_000L, 800, -1),
        };

        int myLevel = CalcLevel();

        for (int i = 0; i < slotDefs.Length; i++)
        {
            float y2 = 0.81f - i * 0.23f;
            float y1 = y2 - 0.20f;
            var def = slotDefs[i];
            int capturedIdx = i;
            bool locked = (i == 2 && myLevel < 10);

            var row = UIHelper.CreatePanel(tf, def.col, $"E{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));

            var placeT = UIHelper.CreateText(row.transform, def.place, 26,
                new Vector2(0.04f, 0.58f), new Vector2(0.65f, 0.96f), TextAlignmentOptions.Left);
            placeT.color     = Color.white;
            placeT.fontStyle = FontStyles.Bold;

            var statusT = UIHelper.CreateText(row.transform, "", 18,
                new Vector2(0.04f, 0.30f), new Vector2(0.65f, 0.57f), TextAlignmentOptions.Left);

            UIHelper.CreateText(row.transform, def.reward, 16,
                new Vector2(0.04f, 0.04f), new Vector2(0.65f, 0.28f), TextAlignmentOptions.Left)
                .color = new Color(0.75f, 0.75f, 0.90f);

            if (locked)
            {
                statusT.text  = "Lv.10 해금";
                statusT.color = new Color(0.5f, 0.5f, 0.6f);
                var lockLbl = UIHelper.CreatePanel(row.transform, new Color(0.20f, 0.18f, 0.25f), "Lock");
                UIHelper.SetAnchors(lockLbl.GetComponent<RectTransform>(),
                    new Vector2(0.67f, 0.15f), new Vector2(0.98f, 0.85f));
                UIHelper.CreateText(lockLbl.transform, "잠금", 20, Vector2.zero, Vector2.one)
                    .color = new Color(0.5f, 0.5f, 0.6f);
            }
            else
            {
                var exp   = _ch.expeditions.Find(x => x.slotIdx == capturedIdx);
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (exp == null)
                {
                    // ① 파견 가능
                    statusT.text  = "파견 가능";
                    statusT.color = new Color(1.0f, 0.85f, 0.3f);

                    var dispBtn = UIHelper.CreateButton(row.transform, "파견하기",
                        new Vector2(0.67f, 0.15f), new Vector2(0.98f, 0.85f),
                        new Color(0.38f, 0.28f, 0.06f));
                    var dTmp = dispBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (dTmp != null) dTmp.fontSize = 20;
                    dispBtn.onClick.AddListener(() =>
                    {
                        long rt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + def.durMs;
                        _ch.expeditions.RemoveAll(x => x.slotIdx == capturedIdx);
                        _ch.expeditions.Add(new ExpeditionState { slotIdx = capturedIdx, returnTime = rt });
                        SaveState();
                        ShowMsg($"원정대 파견! ({def.place})");
                        statusT.text  = "파견 중...";
                        statusT.color = new Color(0.4f, 0.9f, 0.4f);
                        if (dTmp != null) dTmp.text = "파견 중";
                        dispBtn.image.color  = new Color(0.25f, 0.25f, 0.32f);
                        dispBtn.interactable = false;
                        StartCoroutine(ExpeditionCountdown(rt, statusT));
                    });
                }
                else if (nowMs < exp.returnTime)
                {
                    // ② 파견 중 (복귀 대기)
                    statusT.text  = "파견 중...";
                    statusT.color = new Color(0.4f, 0.9f, 0.4f);

                    var dispBtn = UIHelper.CreateButton(row.transform, "파견 중",
                        new Vector2(0.67f, 0.15f), new Vector2(0.98f, 0.85f),
                        new Color(0.25f, 0.25f, 0.32f));
                    var dTmp = dispBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (dTmp != null) dTmp.fontSize = 20;
                    dispBtn.interactable = false;
                    StartCoroutine(ExpeditionCountdown(exp.returnTime, statusT));
                }
                else
                {
                    // ③ 수령 가능
                    statusT.text  = "귀환 완료!";
                    statusT.color = new Color(0.4f, 0.9f, 0.4f);

                    var collectBtn = UIHelper.CreateButton(row.transform, "수령하기",
                        new Vector2(0.67f, 0.15f), new Vector2(0.98f, 0.85f),
                        new Color(0.18f, 0.42f, 0.18f));
                    var cTmp = collectBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (cTmp != null) cTmp.fontSize = 20;
                    string capturedPlace = def.place;
                    collectBtn.onClick.AddListener(() =>
                    {
                        int xpGain = 50 + capturedIdx * 50;
                        _ch.expeditions.RemoveAll(x => x.slotIdx == capturedIdx);
                        GameState.Gold += def.goldReward;
                        RefreshGold();
                        GainXp(xpGain);
                        ShowMsg($"원정 보상 획득! +{def.goldReward}G  +{xpGain}XP");
                        if (def.bagItem >= 0) AddToBag(def.bagItem, 1);
                        else SaveState();
                        if (cTmp != null) cTmp.text = "수령됨";
                        collectBtn.image.color  = new Color(0.25f, 0.25f, 0.32f);
                        collectBtn.interactable = false;
                        statusT.text  = "파견 가능 (팝업 재열기)";
                        statusT.color = new Color(0.75f, 0.75f, 0.90f);
                        AddStoryEntry($"{_ch.generated.name}의 원정대가 {capturedPlace}에서 보상을 가져왔다.");
                    });
                }
            }
        }

        return overlay;
    }

    private IEnumerator ExpeditionCountdown(long returnTimeMs, TextMeshProUGUI statusText)
    {
        while (true)
        {
            if (statusText == null) yield break;
            long remaining = returnTimeMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (remaining <= 0)
            {
                statusText.text  = "귀환 완료!";
                statusText.color = new Color(0.4f, 0.9f, 0.4f);
                yield break;
            }
            long totalSec = remaining / 1000;
            long h = totalSec / 3600;
            long m = (totalSec % 3600) / 60;
            long s = totalSec % 60;
            statusText.text = $"복귀까지  {h:D2}:{m:D2}:{s:D2}";
            yield return new WaitForSeconds(1f);
        }
    }

    // ── 가방 팝업 (드랍템 + 소비 아이템) ────────────────────────
    private GameObject BuildBagPopup()
    {
        var overlay = BuildPopupShell("가방", out Transform tf);

        var items = new (string name, Color col, int sellPrice)[]
        {
            ("낡은 단검 조각", new Color(0.50f, 0.38f, 0.04f),  50),
            ("철제 갑옷 파편", new Color(0.30f, 0.34f, 0.40f),  80),
            ("회복 포션",      new Color(0.60f, 0.10f, 0.10f),  -1),
            ("마나 포션",      new Color(0.10f, 0.20f, 0.65f),  -1),
        };

        for (int i = 0; i < items.Length; i++)
        {
            float y2 = 0.84f - i * 0.17f;
            float y1 = y2 - 0.14f;
            var it = items[i];
            int capturedIdx = i;

            var row = UIHelper.CreatePanel(tf, new Color(0.10f, 0.09f, 0.20f), $"B{i}");
            UIHelper.SetAnchors(row.GetComponent<RectTransform>(),
                new Vector2(0.03f, y1), new Vector2(0.97f, y2));
            _bagRows[capturedIdx] = row;
            if (_bagQty[capturedIdx] == 0) row.SetActive(false); // 수량 0이면 처음부터 숨김

            var strip = UIHelper.CreatePanel(row.transform, it.col, "Strip");
            UIHelper.SetAnchors(strip.GetComponent<RectTransform>(),
                Vector2.zero, new Vector2(0.04f, 1f));

            UIHelper.CreateText(row.transform, it.name, 24,
                new Vector2(0.07f, 0.1f), new Vector2(0.52f, 0.9f), TextAlignmentOptions.Left)
                .color = Color.white;

            var qtyTxt = UIHelper.CreateText(row.transform, $"x {_bagQty[capturedIdx]}", 24,
                new Vector2(0.53f, 0.1f), new Vector2(0.72f, 0.9f), TextAlignmentOptions.Right);
            qtyTxt.color = new Color(1f, 0.85f, 0.3f);
            _bagQtyTexts[capturedIdx] = qtyTxt;

            bool isConsume = it.sellPrice < 0;
            int capturedPrice = it.sellPrice;
            string capturedName = it.name;

            var actionBtn = UIHelper.CreateButton(row.transform,
                isConsume ? "사용" : "판매",
                new Vector2(0.74f, 0.12f), new Vector2(0.98f, 0.88f),
                isConsume ? new Color(0.14f, 0.28f, 0.50f) : new Color(0.40f, 0.28f, 0.06f));
            var aTmp = actionBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (aTmp != null) aTmp.fontSize = 22;

            actionBtn.onClick.AddListener(() =>
            {
                if (_bagQty[capturedIdx] <= 0) return;
                _bagQty[capturedIdx]--;
                qtyTxt.text = $"x {_bagQty[capturedIdx]}";
                if (isConsume)
                {
                    if (capturedName == "회복 포션")
                    {
                        int maxHp = _ch.generated.stats.hp;
                        int heal  = Mathf.Min(50, maxHp - GameState.CurrentHp);
                        GameState.CurrentHp = Mathf.Min(GameState.CurrentHp + 50, maxHp);
                        if (_hpTxt != null) _hpTxt.text = $"HP  {GameState.CurrentHp}/{maxHp}";
                        ShowMsg($"회복 포션 사용! HP +{heal}");
                    }
                    else if (capturedName == "마나 포션")
                    {
                        int maxMp   = _ch.generated.stats.mp;
                        int restore = Mathf.Min(40, maxMp - GameState.CurrentMp);
                        GameState.CurrentMp = Mathf.Min(GameState.CurrentMp + 40, maxMp);
                        if (_mpTxt != null) _mpTxt.text = $"MP  {GameState.CurrentMp}/{maxMp}";
                        ShowMsg($"마나 포션 사용! MP +{restore}");
                    }
                    else ShowMsg($"{capturedName} 사용!");
                }
                else
                {
                    GameState.Gold += capturedPrice;
                    RefreshGold();
                    ShowMsg($"{capturedName} 판매! +{capturedPrice}G");
                }
                SaveState();
                if (_bagQty[capturedIdx] == 0)
                {
                    _bagRows[capturedIdx].SetActive(false);
                    RefreshBagLayout();
                }
            });
        }

        RefreshBagLayout(); // 초기 가시 행 정렬
        return overlay;
    }

    // ── 이벤트 팝업 (출석 체크) ─────────────────────────────────
    private GameObject BuildEventPopup()
    {
        var overlay = BuildPopupShell("이벤트", out Transform tf);

        UIHelper.CreateText(tf, "출석 체크", 30,
            new Vector2(0.03f, 0.84f), new Vector2(0.97f, 0.93f))
            .color = new Color(1f, 0.88f, 0.30f);

        string[] dayLabels = { "1일", "2일", "3일", "4일", "5일", "6일", "7일" };
        string[] rewards   = { "G100", "포션", "G200", "포션x2", "G300", "장비", "G500" };
        int todayIdx = (int)DateTime.Now.DayOfWeek; // 0=일, 1=월, ..., 6=토

        Image todayBoxImg = null;
        TextMeshProUGUI todayRewardTxt = null;

        // 윗줄: 1~4일
        for (int i = 0; i < 4; i++)
        {
            float x1 = 0.03f + i * 0.235f;
            float x2 = x1 + 0.215f;
            bool claimed = i < todayIdx;
            bool isToday = i == todayIdx;
            Color col = claimed ? new Color(0.14f, 0.35f, 0.14f)
                      : isToday ? new Color(0.45f, 0.28f, 0.04f)
                      :           new Color(0.12f, 0.10f, 0.22f);
            var box = UIHelper.CreatePanel(tf, col, $"D{i}");
            UIHelper.SetAnchors(box.GetComponent<RectTransform>(),
                new Vector2(x1, 0.62f), new Vector2(x2, 0.80f));
            UIHelper.CreateText(box.transform, dayLabels[i], 20,
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.95f))
                .color = isToday ? new Color(1f, 0.85f, 0.2f) : Color.white;
            var rwdTxt = UIHelper.CreateText(box.transform, claimed ? "완료" : rewards[i], 18,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.56f));
            rwdTxt.color = claimed ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
            if (isToday) { todayBoxImg = box.GetComponent<Image>(); todayRewardTxt = rwdTxt; }
        }

        // 아랫줄: 5~7일
        for (int i = 4; i < 7; i++)
        {
            int col = i - 4;
            float x1 = 0.03f + col * 0.315f;
            float x2 = x1 + 0.295f;
            bool claimed = i < todayIdx;
            bool isToday = i == todayIdx;
            Color boxCol = claimed ? new Color(0.14f, 0.35f, 0.14f)
                         : isToday ? new Color(0.45f, 0.28f, 0.04f)
                         :           new Color(0.12f, 0.10f, 0.22f);
            var box = UIHelper.CreatePanel(tf, boxCol, $"D{i}");
            UIHelper.SetAnchors(box.GetComponent<RectTransform>(),
                new Vector2(x1, 0.44f), new Vector2(x2, 0.60f));
            UIHelper.CreateText(box.transform, dayLabels[i], 20,
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.95f))
                .color = isToday ? new Color(1f, 0.85f, 0.2f) : Color.white;
            var rwdTxt = UIHelper.CreateText(box.transform, claimed ? "완료" : rewards[i], 18,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.56f));
            rwdTxt.color = claimed ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
            if (isToday) { todayBoxImg = box.GetComponent<Image>(); todayRewardTxt = rwdTxt; }
        }

        UIHelper.CreateText(tf, $"오늘의 보상: {rewards[todayIdx]}", 26,
            new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.41f))
            .color = new Color(1f, 0.85f, 0.3f);

        UIHelper.CreateText(tf, "7일 연속 출석 시 특별 보상 지급!", 22,
            new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.30f))
            .color = new Color(0.65f, 0.85f, 0.65f);

        int[] goldRewards = { 100, 0, 200, 0, 300, 300, 500 };
        int todayGold = goldRewards[todayIdx];

        var claimBtn = UIHelper.CreateButton(tf, _attendanceCollected ? "수령 완료" : "수령하기",
            new Vector2(0.20f, 0.06f), new Vector2(0.80f, 0.18f),
            _attendanceCollected ? new Color(0.25f, 0.25f, 0.32f) : new Color(0.18f, 0.48f, 0.18f));
        if (_attendanceCollected) claimBtn.interactable = false;
        var claimTmp = claimBtn.GetComponentInChildren<TextMeshProUGUI>();
        claimBtn.onClick.AddListener(() =>
        {
            _attendanceCollected = true;
            _ch.lastAttendanceDate = DateTime.Now.ToString("yyyy-MM-dd");
            GameState.Gold += todayGold;
            RefreshGold();
            if (todayIdx == 1)      { AddToBag(2, 1); ShowMsg("출석 보상 획득! 회복 포션 x1"); }
            else if (todayIdx == 3) { AddToBag(2, 2); ShowMsg("출석 보상 획득! 회복 포션 x2"); }
            else ShowMsg(todayGold > 0 ? $"출석 보상 획득! +{todayGold}G" : "출석 보상 획득!");
            if (claimTmp != null) claimTmp.text = "수령 완료";
            claimBtn.image.color = new Color(0.25f, 0.25f, 0.32f);
            claimBtn.interactable = false;
            AddStoryEntry($"오늘도 변함없이 {_ch.generated.name}은(는) 모험을 이어가고 있다.");
            if (todayBoxImg != null) todayBoxImg.color = new Color(0.14f, 0.35f, 0.14f);
            if (todayRewardTxt != null) { todayRewardTxt.text = "완료"; todayRewardTxt.color = new Color(0.5f, 0.9f, 0.5f); }
        });

        return overlay;
    }
}
