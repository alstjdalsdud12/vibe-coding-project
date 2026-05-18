using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    private void Start()
    {
        UIHelper.CreateCanvas(out _);
        var canvas = FindObjectOfType<Canvas>().transform;

        var bg = UIHelper.CreatePanel(canvas, new Color(0.05f, 0.05f, 0.1f), "BG");
        UIHelper.Stretch(bg.GetComponent<RectTransform>());

        UIHelper.CreateText(canvas, "AI RPG", 100,
            new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.78f));

        UIHelper.CreateText(canvas, "AI가 만드는 나만의 RPG", 36,
            new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.6f));

        var startBtn = UIHelper.CreateButton(canvas, "게임 시작",
            new Vector2(0.2f, 0.38f), new Vector2(0.8f, 0.48f));
        startBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene"));
    }
}
