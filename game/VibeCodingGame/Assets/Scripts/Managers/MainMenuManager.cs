using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void OnNewCharacterButtonClick()
    {
        SceneManager.LoadScene("CharacterCreationScene");
    }

    public void OnCharacterSelected(CharacterData character)
    {
        GameState.CurrentCharacter = character;
        SceneManager.LoadScene("GameScene");
    }
}
