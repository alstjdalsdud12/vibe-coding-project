using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterCreationManager : MonoBehaviour
{
    [Header("입력 필드")]
    public TMP_InputField appearanceInput;
    public TMP_InputField weaponInput;
    public TMP_InputField conceptInput;
    public TMP_InputField worldviewInput;

    [Header("UI")]
    public Button createButton;
    public GameObject loadingPanel;
    public TMP_Text errorText;

    private ApiClient _api;

    private void Start()
    {
        _api = GetComponent<ApiClient>();
        loadingPanel.SetActive(false);
        errorText.text = "";
    }

    public void OnCreateButtonClick()
    {
        string appearance = appearanceInput.text.Trim();
        string weapon = weaponInput.text.Trim();
        string concept = conceptInput.text.Trim();
        string worldview = worldviewInput.text.Trim();

        if (string.IsNullOrEmpty(appearance) || string.IsNullOrEmpty(weapon) ||
            string.IsNullOrEmpty(concept) || string.IsNullOrEmpty(worldview))
        {
            errorText.text = "모든 항목을 입력해주세요.";
            return;
        }

        errorText.text = "";
        loadingPanel.SetActive(true);
        createButton.interactable = false;

        StartCoroutine(_api.CreateCharacter(appearance, weapon, concept, worldview, OnSuccess, OnError));
    }

    private void OnSuccess(CharacterData character)
    {
        loadingPanel.SetActive(false);
        GameState.CurrentCharacter = character;
        SceneManager.LoadScene("GameScene");
    }

    private void OnError(string message)
    {
        loadingPanel.SetActive(false);
        createButton.interactable = true;
        errorText.text = "오류: " + message;
    }
}
