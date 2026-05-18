using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Gerçek zamanlı 3 grafik:
///   1) Hata e(t) — kırmızı
///   2) Kontrol çıkışı u(t) — mavi
///   3) Referans r(t) (yeşil) vs Sistem çıkışı y(t) (sarı) — yanal konum karşılaştırması
/// PID parametre slider'ları + P / PI / PID mod butonları
/// </summary>
public class UIGraphManager : MonoBehaviour
{
    public CarLaneTracker carTracker;

    // --- Grafik boyutları ---
    private int gw = 420;   // genişlik
    private int gh = 130;   // yükseklik

    // Texture & RawImage referansları
    private Texture2D texError, texControl, texPath, texSpeed;
    private RawImage  imgError, imgControl, imgPath, imgSpeed;
    private Color[] bgPixels;

    // Slider referansları
    private Slider slKp, slKi, slKd, slSpeed;
    private InputField inKp, inKi, inKd, inSpeed;

    // Grafik kalemi x konumu
    private int cursorX = 0;

    // Durdur/Devam
    private bool isPaused = false;
    private Button pauseBtn;
    private Text   pauseBtnText;

    // Mod butonları referansları (aktif mod vurgusu için)
    private Button[] modeButtons = new Button[3];
    private Image[]  modeBtnImages = new Image[3];

    // Hız kontrol UI
    private Button autoBrakeBtn;
    private Image  autoBrakeImg;
    private Text   autoBrakeTxt;
    private Text   speedLabel;

    // Kaydedilmiş PID parametreleri (mod geçişlerinde korunması için)
    private float savedKp = 2f;
    private float savedKi = 0.05f;
    private float savedKd = 1f;

    // Aktif mod etiketi
    private Text activeModeLabel;

    // ============================================================
    void Awake()
    {
        // Inspector'dan atanmamışsa sahnede otomatik olarak bul
        if (carTracker == null) 
        {
            carTracker = FindAnyObjectByType<CarLaneTracker>();
        }
    }

    void Start() { BuildUI(); InitTextures(); }

    // ============================================================
    void BuildUI()
    {
        EnsureEventSystem();

        // Ana canvas
        Canvas canvas = new GameObject("SimCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = canvas.gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        Transform root = canvas.transform;

        // ---- Sol Panel: 3 grafik ----
        RectTransform leftPanel = MakeRect("LeftPanel", root,
            new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
            new Vector2(16, -16), new Vector2(gw, gh * 4 + 140));

        // Başlık
        MakeLabel(leftPanel, "title",
            "Gerçek Zamanlı Sinyaller    Kırmızı: e(t)  |  Mavi: u(t)  |  Yeşil: r(t)  Sarı: y(t)",
            new Vector2(4, -4), new Vector2(gw - 4, 48), 17, Color.white);

        // Grafik 1 — Hata e(t)
        MakeLabel(leftPanel, "lbl1", "\u25cf  Hata  e(t) = r(t) - y(t)",
            new Vector2(4, -54), new Vector2(gw, 22), 16, new Color(1f, 0.4f, 0.4f), FontStyle.Bold);
        imgError = MakeGraph(leftPanel, "GrError", new Vector2(0, -76), gw, gh);

        // Grafik 2 — Kontrol u(t)
        MakeLabel(leftPanel, "lbl2", "\u25cf  Kontrol Çıkışı  u(t)",
            new Vector2(4, -76 - gh - 6), new Vector2(gw, 22), 16, new Color(0.4f, 0.85f, 1f), FontStyle.Bold);
        imgControl = MakeGraph(leftPanel, "GrControl", new Vector2(0, -102 - gh), gw, gh);

        // Grafik 3 — Referans r(t) vs Sistem Çıkışı y(t) — Yanal Konum Karşılaştırması
        MakeLabel(leftPanel, "lbl3", "\u25cf  r(t) Referans (yeşil) vs y(t) Gerçek (sarı)",
            new Vector2(4, -102 - gh * 2 - 6), new Vector2(gw, 22), 16, new Color(0.7f, 1f, 0.4f), FontStyle.Bold);
        imgPath = MakeGraph(leftPanel, "GrPath", new Vector2(0, -128 - gh * 2), gw, gh);

        // Grafik 4 — Hız v(t)
        MakeLabel(leftPanel, "lbl4", "\u25cf  Hız v(t) (m/s)  —  Turuncu: gerçek  |  Beyaz: hedef",
            new Vector2(4, -128 - gh * 3 - 6), new Vector2(gw, 22), 16, new Color(1f, 0.65f, 0.2f), FontStyle.Bold);
        imgSpeed = MakeGraph(leftPanel, "GrSpeed", new Vector2(0, -154 - gh * 3), gw, gh);

        // ---- Sağ Panel: PID ayarları + mod seçimi ----
        RectTransform rightPanel = MakeRect("RightPanel", root,
            new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
            new Vector2(-16, -16), new Vector2(400, 520));

        Image bg = rightPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        MakeLabel(rightPanel, "title", "PID Parametre Ayarları",
            new Vector2(0, -12), new Vector2(360, 34), 19, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);

        slKp = MakeSlider(rightPanel, "Kp", 0, 0f, 50f, out inKp);
        slKi = MakeSlider(rightPanel, "Ki", 1, 0f, 10f,  out inKi);
        slKd = MakeSlider(rightPanel, "Kd", 2, 0f, 30f, out inKd);

        // P / PI / PID mod butonları
        MakeLabel(rightPanel, "lblMode", "Kontrolcü Modu:",
            new Vector2(12, -195), new Vector2(360, 26), 15, Color.white);
        MakeModeButton(rightPanel, "P",   0, PIDController.ControllerMode.P);
        MakeModeButton(rightPanel, "PI",  1, PIDController.ControllerMode.PI);
        MakeModeButton(rightPanel, "PID", 2, PIDController.ControllerMode.PID);

        // Aktif mod etiketi
        activeModeLabel = MakeLabel(rightPanel, "activeModeLbl", "Aktif Mod: PID",
            new Vector2(12, -266), new Vector2(360, 26), 16, new Color(0.4f, 1f, 0.5f), FontStyle.Bold);

        // === HIZ KONTROL BÖLÜMÜ ===
        MakeLabel(rightPanel, "lblSpeed", "─── Hız Kontrolü ───",
            new Vector2(12, -298), new Vector2(360, 26), 15, new Color(1f, 0.7f, 0.3f), FontStyle.Bold, TextAnchor.MiddleCenter);

        // Hız slider (idx=3 → y offset hesaplanır ayrıca)
        MakeLabel(rightPanel, "Speed_lbl", "Hız:",
            new Vector2(14, -326), new Vector2(40, 32), 15, Color.white);
        inSpeed = MakeInputField(rightPanel, "Speed_input", new Vector2(54, -322), new Vector2(60, 26));
        slSpeed = MakeSliderAt(rightPanel, "Speed_sl", new Vector2(124, -322), new Vector2(234, 26), 2f, 30f);

        speedLabel = MakeLabel(rightPanel, "speedInfo", "Gerçek: 0.0 m/s | Hedef: 14.0 m/s",
            new Vector2(12, -354), new Vector2(360, 22), 14, new Color(1f, 0.8f, 0.4f));

        // Otomatik fren butonu
        BuildAutoBrakeButton(rightPanel, new Vector2(12, -382), new Vector2(356, 38));

        // Hız slider dinleyicileri
        if (carTracker != null)
        {
            slSpeed.value = carTracker.targetSpeed;
            inSpeed.text = carTracker.targetSpeed.ToString("F1");
            slSpeed.onValueChanged.AddListener(v => {
                carTracker.targetSpeed = v;
                if (!inSpeed.isFocused) inSpeed.text = v.ToString("F1");
            });
            inSpeed.onEndEdit.AddListener(s => {
                if (float.TryParse(s, out float val)) {
                    val = Mathf.Clamp(val, 2f, 30f);
                    slSpeed.value = val;
                }
            });
        }

        // Durdur / Devam butonu
        rightPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 500);
        var pauseGo = new GameObject("PauseBtn");
        pauseGo.transform.SetParent(rightPanel, false);
        var pauseImg = pauseGo.AddComponent<Image>();
        pauseImg.color = new Color(0.7f, 0.2f, 0.1f);
        var pauseRt = pauseGo.GetComponent<RectTransform>();
        pauseRt.anchorMin = new Vector2(0,1); pauseRt.anchorMax = new Vector2(1,1);
        pauseRt.pivot = new Vector2(0.5f, 1);
        pauseRt.anchoredPosition = new Vector2(0, -430); pauseRt.sizeDelta = new Vector2(-20, 44);
        var pauseTxtGo = new GameObject("PauseTxt"); pauseTxtGo.transform.SetParent(pauseGo.transform, false);
        pauseBtnText = pauseTxtGo.AddComponent<Text>();
        pauseBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pauseBtnText.text = "⏸  DURDUR";
        pauseBtnText.color = Color.white; pauseBtnText.fontSize = 18;
        pauseBtnText.fontStyle = FontStyle.Bold;
        pauseBtnText.alignment = TextAnchor.MiddleCenter;
        var pTrt = pauseTxtGo.GetComponent<RectTransform>();
        pTrt.anchorMin = Vector2.zero; pTrt.anchorMax = Vector2.one; pTrt.sizeDelta = Vector2.zero;
        pauseBtn = pauseGo.AddComponent<Button>();
        pauseBtn.targetGraphic = pauseImg;
        pauseBtn.onClick.AddListener(TogglePause);

        // Slider değerlerini PID'den yükle ve dinleyicileri bağla
        if (carTracker != null && carTracker.pid != null)
        {
            savedKp = carTracker.pid.Kp;
            savedKi = carTracker.pid.Ki;
            savedKd = carTracker.pid.Kd;

            slKp.value = carTracker.pid.Kp;
            slKi.value = carTracker.pid.Ki;
            slKd.value = carTracker.pid.Kd;

            slKp.onValueChanged.AddListener(v => {
                carTracker.pid.Kp = v;
                savedKp = v;
                if (!inKp.isFocused) inKp.text = v.ToString("F2");
            });
            inKp.onEndEdit.AddListener(s => {
                if (float.TryParse(s, out float val)) {
                    val = Mathf.Clamp(val, 0f, 50f);
                    slKp.value = val;
                }
            });

            slKi.onValueChanged.AddListener(v => {
                carTracker.pid.Ki = v;
                if (carTracker.pid.mode != PIDController.ControllerMode.P) savedKi = v;
                if (!inKi.isFocused) inKi.text = v.ToString("F2");
            });
            inKi.onEndEdit.AddListener(s => {
                if (float.TryParse(s, out float val)) {
                    val = Mathf.Clamp(val, 0f, 10f);
                    slKi.value = val;
                }
            });

            slKd.onValueChanged.AddListener(v => {
                carTracker.pid.Kd = v;
                if (carTracker.pid.mode == PIDController.ControllerMode.PID) savedKd = v;
                if (!inKd.isFocused) inKd.text = v.ToString("F2");
            });
            inKd.onEndEdit.AddListener(s => {
                if (float.TryParse(s, out float val)) {
                    val = Mathf.Clamp(val, 0f, 30f);
                    slKd.value = val;
                }
            });

            RefreshSliderTexts();
            UpdateModeButtonHighlights(carTracker.pid.mode);
        }
    }

    // ============================================================
    void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pauseBtnText != null)
        {
            pauseBtnText.text = isPaused ? "▶  DEVAM" : "⏸  DURDUR";
            if (pauseBtn != null)
                pauseBtn.GetComponent<Image>().color = isPaused
                    ? new Color(0.1f, 0.55f, 0.15f)
                    : new Color(0.7f, 0.2f, 0.1f);
        }
    }

    // ============================================================
    void ClearGraphs()
    {
        cursorX = 0;
        ApplyBg(texError);   if (imgError   != null) imgError.texture   = texError;
        ApplyBg(texControl); if (imgControl != null) imgControl.texture = texControl;
        ApplyBg(texPath);    if (imgPath    != null) imgPath.texture    = texPath;
        ApplyBg(texSpeed);   if (imgSpeed   != null) imgSpeed.texture   = texSpeed;
    }

    // ============================================================
    void UpdateModeButtonHighlights(PIDController.ControllerMode activeMode)
    {
        Color activeColor   = new Color(0.1f, 0.6f, 0.3f);
        Color inactiveColor = new Color(0.18f, 0.38f, 0.6f);

        for (int i = 0; i < 3; i++)
        {
            if (modeBtnImages[i] != null)
            {
                bool isActive = (i == 0 && activeMode == PIDController.ControllerMode.P)
                             || (i == 1 && activeMode == PIDController.ControllerMode.PI)
                             || (i == 2 && activeMode == PIDController.ControllerMode.PID);
                modeBtnImages[i].color = isActive ? activeColor : inactiveColor;
            }
        }

        if (activeModeLabel != null)
            activeModeLabel.text = $"Aktif Mod: {activeMode}";
    }

    // ============================================================
    void InitTextures()
    {
        texError   = NewTex();
        texControl = NewTex();
        texPath    = NewTex();
        texSpeed   = NewTex();

        bgPixels = new Color[gw * gh];
        for (int i = 0; i < bgPixels.Length; i++)
            bgPixels[i] = new Color(0.05f, 0.05f, 0.1f, 0.9f);
        // Orta yatay çizgi (sıfır çizgisi)
        for (int x = 0; x < gw; x++)
            bgPixels[(gh / 2) * gw + x] = new Color(0.35f, 0.35f, 0.35f);

        ApplyBg(texError);   if (imgError   != null) imgError.texture   = texError;
        ApplyBg(texControl); if (imgControl != null) imgControl.texture = texControl;
        ApplyBg(texPath);    if (imgPath    != null) imgPath.texture    = texPath;
        ApplyBg(texSpeed);   if (imgSpeed   != null) imgSpeed.texture   = texSpeed;
    }

    // ============================================================
    void Update()
    {
        if (carTracker == null) return;

        float err  = carTracker.currentError;          // e(t) = r(t) - y(t)
        float ctrl = carTracker.currentControlSignal;  // u(t)
        float refLateral = carTracker.currentRefLateral;  // r(t)
        float carLateral = carTracker.currentCarLateral;  // y(t)
        float actualSpeed = carTracker.currentActualSpeed; // v(t)
        float tgtSpeed    = carTracker.targetSpeed;        // hedef hız

        // --- Grafik 1: Hata e(t) — ölçek ±8 birim ---
        int ey = WorldToPixel(err, 8f);
        // --- Grafik 2: Kontrol u(t) — ölçek ±maxSteer derece ---
        int cy = WorldToPixel(ctrl, carTracker.maxSteerDeg);
        // --- Grafik 3: r(t) vs y(t) — Yanal konum karşılaştırması ---
        int ry = WorldToPixel(refLateral, 4f);
        int ay = WorldToPixel(carLateral, 4f);
        // --- Grafik 4: Hız v(t) — ölçek 0..30 m/s (ortası=15) ---
        int sy = WorldToPixelUnsigned(actualSpeed, 30f);
        int ty = WorldToPixelUnsigned(tgtSpeed, 30f);

        // Kaydır veya yaz
        if (cursorX >= gw)
        {
            ShiftLeft(texError);
            ShiftLeft(texControl);
            ShiftLeft(texPath);
            ShiftLeft(texSpeed);
            cursorX = gw - 1;
        }

        Plot(texError,   cursorX, ey, Color.red);
        Plot(texControl, cursorX, cy, new Color(0.3f, 0.8f, 1f));
        Plot(texPath,    cursorX, ry, new Color(0.3f, 1f, 0.3f));
        Plot(texPath,    cursorX, ay, new Color(1f, 1f, 0.2f));
        Plot(texSpeed,   cursorX, sy, new Color(1f, 0.55f, 0.1f));  // turuncu: gerçek hız
        Plot(texSpeed,   cursorX, ty, new Color(0.9f, 0.9f, 0.9f)); // beyaz: hedef hız

        texError.Apply(); texControl.Apply(); texPath.Apply(); texSpeed.Apply();

        // Hız bilgi etiketi güncelle
        if (speedLabel != null)
            speedLabel.text = $"Gerçek: {actualSpeed:F1} m/s | Hedef: {tgtSpeed:F1} m/s | Eğrilik: {carTracker.currentCurvature:F3}";

        cursorX++;
    }

    // ============================================================
    #region Yardımcı fonksiyonlar

    int WorldToPixel(float val, float range)
        => Mathf.Clamp(Mathf.RoundToInt((val / range) * (gh / 2f)) + gh / 2, 0, gh - 1);

    // 0..max aralığında (unsigned) pixel pozisyonu — hız grafiği için
    int WorldToPixelUnsigned(float val, float max)
        => Mathf.Clamp(Mathf.RoundToInt((val / max) * (gh - 1)), 0, gh - 1);

    void Plot(Texture2D t, int x, int y, Color c)
    {
        if (x < 0 || x >= gw) return;
        for (int dy = -1; dy <= 1; dy++)
        {
            int py = y + dy;
            if (py >= 0 && py < gh) t.SetPixel(x, py, c);
        }
    }

    void ShiftLeft(Texture2D t)
    {
        Color[] px = t.GetPixels();
        Color[] shifted = new Color[gw * gh];
        for (int y = 0; y < gh; y++)
        {
            for (int x = 1; x < gw; x++)
                shifted[y * gw + (x - 1)] = px[y * gw + x];
            Color bg2 = (y == gh / 2) ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.05f, 0.05f, 0.1f, 0.9f);
            shifted[y * gw + (gw - 1)] = bg2;
        }
        t.SetPixels(shifted);
    }

    Texture2D NewTex()
    {
        var t = new Texture2D(gw, gh);
        t.filterMode = FilterMode.Point;
        return t;
    }

    void ApplyBg(Texture2D t) { t.SetPixels(bgPixels); t.Apply(); }

    void RefreshSliderTexts()
    {
        if (carTracker?.pid == null) return;
        if (inKp != null && !inKp.isFocused) inKp.text = carTracker.pid.Kp.ToString("F2");
        if (inKi != null && !inKi.isFocused) inKi.text = carTracker.pid.Ki.ToString("F2");
        if (inKd != null && !inKd.isFocused) inKd.text = carTracker.pid.Kd.ToString("F2");
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esGo.AddComponent<StandaloneInputModule>();
#endif
    }

    // ---- UI fabrika fonksiyonları ----

    RectTransform MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    Text MakeLabel(Transform parent, string name, string txt,
        Vector2 pos, Vector2 size, int fontSize,
        Color color, FontStyle style = FontStyle.Normal,
        TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = txt; t.color = color;
        t.fontSize = fontSize; t.fontStyle = style;
        t.alignment = anchor;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return t;
    }

    RawImage MakeGraph(Transform parent, string name, Vector2 pos, int w, int h)
    {
        var rt = MakeRect(name, parent,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            pos, new Vector2(w, h));
        return rt.gameObject.AddComponent<RawImage>();
    }

    InputField MakeInputField(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var txt = textGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white;
        txt.fontSize = 15;
        txt.alignment = TextAnchor.MiddleCenter;
        var txtRt = textGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(2, 0); txtRt.offsetMax = new Vector2(-2, 0);

        var input = go.AddComponent<InputField>();
        input.textComponent = txt;
        input.characterValidation = InputField.CharacterValidation.Decimal;
        return input;
    }

    Slider MakeSlider(Transform parent, string label, int idx, float min, float max, out InputField valInput)
    {
        float y = -58 - idx * 46f;

        MakeLabel(parent, label + "_lbl", label + ":",
            new Vector2(14, y), new Vector2(40, 32), 15, Color.white);

        valInput = MakeInputField(parent, label + "_input", new Vector2(54, y + 4), new Vector2(60, 26));

        var slGo = new GameObject(label + "_sl");
        slGo.transform.SetParent(parent, false);
        var sl = slGo.AddComponent<Slider>();
        var srt = slGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(0, 1);
        srt.pivot = new Vector2(0, 1);
        srt.anchoredPosition = new Vector2(124, y + 4); srt.sizeDelta = new Vector2(234, 26);

        // Background
        var bgObj = new GameObject("Bg"); bgObj.transform.SetParent(slGo.transform, false);
        var bgImg = bgObj.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;

        // Fill area
        var fa = new GameObject("FillArea"); fa.transform.SetParent(slGo.transform, false);
        var faRt = fa.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(0.2f, 0.75f, 1f);
        var fillRt = fill.GetComponent<RectTransform>(); fillRt.sizeDelta = Vector2.zero;

        // Handle
        var ha = new GameObject("HandleArea"); ha.transform.SetParent(slGo.transform, false);
        var haRt = ha.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.sizeDelta = Vector2.zero;

        var handle = new GameObject("Handle"); handle.transform.SetParent(ha.transform, false);
        var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>(); handleRt.sizeDelta = new Vector2(18, 0);

        sl.targetGraphic = handleImg;
        sl.fillRect = fillRt;
        sl.handleRect = handleRt;
        sl.minValue = min; sl.maxValue = max;

        return sl;
    }

    void MakeModeButton(Transform parent, string label, int idx, PIDController.ControllerMode targetMode)
    {
        float x = 12 + idx * 116f;
        float y = -222f;

        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.38f, 0.6f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(106, 38);

        var textGo = new GameObject("Txt"); textGo.transform.SetParent(go.transform, false);
        var t = textGo.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label; t.color = Color.white;
        t.fontSize = 17; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Buton referanslarını kaydet
        modeButtons[idx] = btn;
        modeBtnImages[idx] = img;

        btn.onClick.AddListener(() =>
        {
            if (carTracker?.pid != null)
            {
                carTracker.pid.mode = targetMode;
                carTracker.pid.ResetController();

                // Mod geçişlerinde slider'ları güncelle
                if (targetMode == PIDController.ControllerMode.P)
                {
                    // P modu: Ki ve Kd devre dışı — slider'ları 0'a çek
                    carTracker.pid.Ki = 0f; carTracker.pid.Kd = 0f;
                    if (slKi) slKi.value = 0f;
                    if (slKd) slKd.value = 0f;
                }
                else if (targetMode == PIDController.ControllerMode.PI)
                {
                    // PI modu: Ki'yi geri yükle, Kd devre dışı
                    carTracker.pid.Ki = savedKi;
                    carTracker.pid.Kd = 0f;
                    if (slKi) slKi.value = savedKi;
                    if (slKd) slKd.value = 0f;
                }
                else // PID
                {
                    // PID modu: tüm parametreleri geri yükle
                    carTracker.pid.Ki = savedKi;
                    carTracker.pid.Kd = savedKd;
                    if (slKi) slKi.value = savedKi;
                    if (slKd) slKd.value = savedKd;
                }

                // Grafikleri sıfırla (temiz karşılaştırma için)
                ClearGraphs();

                // Aktif mod butonunu vurgula
                UpdateModeButtonHighlights(targetMode);

                Debug.Log($"Kontrolcü modu değiştirildi: {targetMode}");
            }
        });
    }

    // ---- Hız kontrol yardımcı fonksiyonlar ----

    Slider MakeSliderAt(Transform parent, string name, Vector2 pos, Vector2 size, float min, float max)
    {
        var slGo = new GameObject(name);
        slGo.transform.SetParent(parent, false);
        var sl = slGo.AddComponent<Slider>();
        var srt = slGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(0, 1);
        srt.pivot = new Vector2(0, 1);
        srt.anchoredPosition = pos; srt.sizeDelta = size;

        var bgObj = new GameObject("Bg"); bgObj.transform.SetParent(slGo.transform, false);
        var bgImg = bgObj.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;

        var fa = new GameObject("FillArea"); fa.transform.SetParent(slGo.transform, false);
        var faRt = fa.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(1f, 0.6f, 0.15f);
        var fillRt = fill.GetComponent<RectTransform>(); fillRt.sizeDelta = Vector2.zero;

        var ha = new GameObject("HandleArea"); ha.transform.SetParent(slGo.transform, false);
        var haRt = ha.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.sizeDelta = Vector2.zero;

        var handle = new GameObject("Handle"); handle.transform.SetParent(ha.transform, false);
        var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>(); handleRt.sizeDelta = new Vector2(18, 0);

        sl.targetGraphic = handleImg;
        sl.fillRect = fillRt;
        sl.handleRect = handleRt;
        sl.minValue = min; sl.maxValue = max;

        return sl;
    }

    void BuildAutoBrakeButton(Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("AutoBrakeBtn");
        go.transform.SetParent(parent, false);
        autoBrakeImg = go.AddComponent<Image>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var txtGo = new GameObject("Txt"); txtGo.transform.SetParent(go.transform, false);
        autoBrakeTxt = txtGo.AddComponent<Text>();
        autoBrakeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        autoBrakeTxt.fontSize = 16; autoBrakeTxt.fontStyle = FontStyle.Bold;
        autoBrakeTxt.color = Color.white;
        autoBrakeTxt.alignment = TextAnchor.MiddleCenter;
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        autoBrakeBtn = go.AddComponent<Button>();
        autoBrakeBtn.targetGraphic = autoBrakeImg;

        // Başlangıç durumu
        bool isOn = carTracker != null && carTracker.autoBrakeOnCurves;
        UpdateAutoBrakeVisual(isOn);

        autoBrakeBtn.onClick.AddListener(() => {
            if (carTracker != null)
            {
                carTracker.autoBrakeOnCurves = !carTracker.autoBrakeOnCurves;
                UpdateAutoBrakeVisual(carTracker.autoBrakeOnCurves);
            }
        });
    }

    void UpdateAutoBrakeVisual(bool isOn)
    {
        if (autoBrakeImg != null)
            autoBrakeImg.color = isOn ? new Color(0.1f, 0.55f, 0.15f) : new Color(0.5f, 0.2f, 0.1f);
        if (autoBrakeTxt != null)
            autoBrakeTxt.text = isOn ? "🟢  Viraj Freni: AÇIK" : "🔴  Viraj Freni: KAPALI";
    }

    #endregion
}
