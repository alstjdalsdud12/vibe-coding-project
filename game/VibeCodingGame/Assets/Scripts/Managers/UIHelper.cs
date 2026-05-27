using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 모든 Manager에서 공통으로 쓰는 UI 생성 유틸리티
public static class UIHelper
{
    public static void CreateCamera()
    {
        if (Object.FindObjectOfType<Camera>() != null) return;
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        camGO.tag = "MainCamera";
    }

    public static GameObject CreateCanvas(out Canvas canvas)
    {
        CreateCamera();
        var go = new GameObject("Canvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        go.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return go;
    }

    public static GameObject CreatePanel(Transform parent, Color color, string name = "Panel")
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static TMPro.TMP_FontAsset GetFont()
        => Resources.Load<TMPro.TMP_FontAsset>("malgun SDF") ?? TMP_Settings.defaultFontAsset;

    public static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Text_" + text.Replace("\n", ""));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        var font = GetFont();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return tmp;
    }

    public static Button CreateButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color? color = null)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color ?? new Color(0.2f, 0.45f, 0.85f);
        var btn = go.AddComponent<Button>();
        SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        var font = GetFont();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 40;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        Stretch(textGO.GetComponent<RectTransform>());
        return btn;
    }

    public static TMP_InputField CreateInputField(Transform parent, string placeholder,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Input_" + placeholder);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        var field = go.AddComponent<TMP_InputField>();
        SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

        var textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(go.transform, false);
        var textAreaRT = textAreaGO.AddComponent<RectTransform>();
        textAreaGO.AddComponent<RectMask2D>();
        SetAnchors(textAreaRT, new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.9f));

        var font = GetFont();

        // TMP_InputField 내부용 (투명 - 실제 입력 처리만 담당)
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textAreaGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        var text = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = 34;
        text.color = new Color(1, 1, 1, 0); // 투명
        text.enableWordWrapping = false;
        Stretch(textRT);

        // 실제 화면에 보이는 텍스트 (한글 IME 버그 우회)
        var displayGO = new GameObject("Display");
        displayGO.transform.SetParent(textAreaGO.transform, false);
        var displayRT = displayGO.AddComponent<RectTransform>();
        var displayText = displayGO.AddComponent<TextMeshProUGUI>();
        if (font != null) displayText.font = font;
        displayText.fontSize = 34;
        displayText.color = Color.white;
        displayText.enableWordWrapping = false;
        Stretch(displayRT);

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textAreaGO.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        if (font != null) ph.font = font;
        ph.text = placeholder;
        ph.fontSize = 34;
        ph.color = new Color(0.6f, 0.6f, 0.6f);
        ph.fontStyle = FontStyles.Italic;
        ph.enableWordWrapping = false;
        Stretch(phRT);

        field.textViewport = textAreaRT;
        field.textComponent = text;
        field.placeholder = ph;
        field.customCaretColor = true;
        field.caretColor = new Color(0, 0, 0, 0); // 기본 커서 숨김

        KoreanInputRefresher.Attach(go, field, displayText, ph);
        return field;
    }

    public static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 offset = default)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = offset;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private class KoreanInputRefresher : MonoBehaviour
    {
        private TMP_InputField _field;
        private TextMeshProUGUI _display;
        private TMP_Text _placeholder;
        private float _caretTimer;

        public static void Attach(GameObject go, TMP_InputField field, TextMeshProUGUI display, TMP_Text placeholder)
        {
            var r = go.AddComponent<KoreanInputRefresher>();
            r._field = field;
            r._display = display;
            r._placeholder = placeholder;
        }

        private void Update()
        {
            if (_field == null || _display == null) return;

            bool hasText = !string.IsNullOrEmpty(_field.text);
            if (_placeholder != null)
                _placeholder.gameObject.SetActive(!hasText && !_field.isFocused);

            if (_field.isFocused)
            {
                _caretTimer += Time.deltaTime;
                bool caretOn = (int)(_caretTimer * 2) % 2 == 0;
                string comp = Input.compositionString;
                _display.text = _field.text + comp + (caretOn ? "|" : " ");
            }
            else
            {
                _caretTimer = 0;
                _display.text = _field.text;
            }
        }
    }
}
