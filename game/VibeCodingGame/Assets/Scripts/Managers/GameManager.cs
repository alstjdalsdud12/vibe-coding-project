using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("장소 선택 UI")]
    public GameObject locationPanel;
    public Transform locationListParent;
    public GameObject locationButtonPrefab;
    public TMP_InputField customLocationInput;
    public Button customLocationButton;

    [Header("전투 UI")]
    public GameObject battlePanel;
    public TMP_Text battleLogText;
    public TMP_Text playerHpText;
    public TMP_Text playerMpText;
    public TMP_Text enemyNameText;
    public TMP_Text enemyHpText;
    public Button attackButton;
    public Button skillButton;
    public Button fleeButton;

    [Header("전투 결과 UI")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button resultBackButton;

    private CharacterData _player;
    private int _playerHp;
    private int _playerMp;
    private int _enemyHp;
    private int _enemyMaxHp;
    private string _enemyName;
    private int _enemyAtk;
    private int _enemyDef;
    private int _currentWave;
    private const int MAX_WAVE = 5;
    private string _currentLocation;

    private void Start()
    {
        _player = GameState.CurrentCharacter;
        _playerHp = _player.generated.stats.hp;
        _playerMp = _player.generated.stats.mp;

        ShowLocationPanel();
    }

    private void ShowLocationPanel()
    {
        locationPanel.SetActive(true);
        battlePanel.SetActive(false);
        resultPanel.SetActive(false);

        foreach (Transform child in locationListParent)
            Destroy(child.gameObject);

        foreach (var loc in _player.generated.locations)
        {
            var btn = Instantiate(locationButtonPrefab, locationListParent);
            btn.GetComponentInChildren<TMP_Text>().text = loc.name + "\n" + loc.description;
            string locName = loc.name;
            btn.GetComponent<Button>().onClick.AddListener(() => StartLocation(locName));
        }

        customLocationButton.onClick.RemoveAllListeners();
        customLocationButton.onClick.AddListener(() =>
        {
            string custom = customLocationInput.text.Trim();
            if (!string.IsNullOrEmpty(custom)) StartLocation(custom);
        });
    }

    private void StartLocation(string locationName)
    {
        _currentLocation = locationName;
        _currentWave = 1;
        locationPanel.SetActive(false);
        battlePanel.SetActive(true);
        StartWave();
    }

    private void StartWave()
    {
        // 임시 고정 몬스터 (나중에 AI 생성으로 교체)
        _enemyName = $"{_currentLocation}의 몬스터 (웨이브 {_currentWave})";
        _enemyHp = 50 + (_currentWave * 20);
        _enemyMaxHp = _enemyHp;
        _enemyAtk = 10 + (_currentWave * 5);
        _enemyDef = 3 + (_currentWave * 2);

        RefreshUI();
        AppendLog($"=== 웨이브 {_currentWave} / {MAX_WAVE} ===");
        AppendLog($"{_enemyName} 등장!");

        attackButton.onClick.RemoveAllListeners();
        skillButton.onClick.RemoveAllListeners();
        fleeButton.onClick.RemoveAllListeners();

        attackButton.onClick.AddListener(OnAttack);
        skillButton.onClick.AddListener(OnSkill);
        fleeButton.onClick.AddListener(OnFlee);
    }

    private void OnAttack()
    {
        int dmg = Mathf.Max(1, _player.generated.stats.atk - _enemyDef);
        _enemyHp -= dmg;
        AppendLog($"{_player.generated.name}의 공격! {_enemyName}에게 {dmg} 피해!");
        CheckBattleResult();
    }

    private void OnSkill()
    {
        if (_player.generated.abilities == null || _player.generated.abilities.Count == 0)
        {
            AppendLog("사용 가능한 스킬이 없습니다.");
            return;
        }

        var skill = _player.generated.abilities[0];
        int mpCost = 20;
        if (_playerMp < mpCost)
        {
            AppendLog("MP가 부족합니다!");
            return;
        }

        _playerMp -= mpCost;
        int dmg = Mathf.Max(1, (int)(_player.generated.stats.atk * 1.8f) - _enemyDef);
        _enemyHp -= dmg;
        AppendLog($"{skill.name} 사용! {_enemyName}에게 {dmg} 피해!");
        RefreshUI();
        CheckBattleResult();
    }

    private void OnFlee()
    {
        AppendLog("전투에서 도망쳤습니다.");
        ShowLocationPanel();
    }

    private void CheckBattleResult()
    {
        RefreshUI();

        if (_enemyHp <= 0)
        {
            AppendLog($"{_enemyName} 처치!");

            if (_currentWave >= MAX_WAVE)
            {
                AppendLog($"{_currentLocation} 클리어!");
                ShowResult(true);
            }
            else
            {
                _currentWave++;
                EnemyAttack();
            }
            return;
        }

        EnemyAttack();
    }

    private void EnemyAttack()
    {
        int dmg = Mathf.Max(1, _enemyAtk - _player.generated.stats.def);
        _playerHp -= dmg;
        AppendLog($"{_enemyName}의 반격! {_player.generated.name}에게 {dmg} 피해!");
        RefreshUI();

        if (_playerHp <= 0)
        {
            AppendLog($"{_player.generated.name} 사망...");
            ShowResult(false);
            return;
        }

        if (_enemyHp <= 0 && _currentWave < MAX_WAVE)
        {
            _currentWave++;
            StartWave();
        }
    }

    private void ShowResult(bool isVictory)
    {
        battlePanel.SetActive(false);
        resultPanel.SetActive(true);
        resultText.text = isVictory
            ? $"{_currentLocation} 클리어!\n모든 웨이브를 돌파했습니다."
            : $"{_player.generated.name}이(가) 쓰러졌습니다.\n캐릭터가 삭제됩니다.";

        resultBackButton.onClick.RemoveAllListeners();
        resultBackButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }

    private void EnemyAttack(bool dummy) { }

    private void RefreshUI()
    {
        playerHpText.text = $"HP: {_playerHp} / {_player.generated.stats.hp}";
        playerMpText.text = $"MP: {_playerMp} / {_player.generated.stats.mp}";
        enemyNameText.text = _enemyName;
        enemyHpText.text = $"HP: {Mathf.Max(0, _enemyHp)} / {_enemyMaxHp}";
    }

    private void AppendLog(string message)
    {
        battleLogText.text += message + "\n";
    }
}
