using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PID Katsayıları (Kp, Ki, Kd) ve Kontrolcü Modlarını (P, PI, PID) yöneten arayüz bileşeni.
/// BAĞLANTI: SimulationUIController.cs tarafından çağrılır.
/// Aracın (CarLaneTracker) içindeki PIDController bileşenine doğrudan müdahale eder.
/// </summary>
public class PIDSettingsUIManager
{
    private CarLaneTracker _tracker;

    // Kp, Ki, Kd ayarları için Slider ve Input (Girdi kutusu) referansları
    private Slider _slP, _slI, _slD;
    private InputField _inP, _inI, _inD;
    
    // Mod seçim (P-Only, PI-Only, PID) butonları ve renk değiştirecek olan Image'leri
    private Button _btnP, _btnPI, _btnPID;
    private Image _imgP, _imgPI, _imgPID;

    // Butonların normal (pasif) ve seçili (aktif) durumlarındaki renkleri
    private Color _btnActiveColor = new Color(0.2f, 0.7f, 1f);
    private Color _btnInactiveColor = new Color(0.2f, 0.2f, 0.25f);

    /// <summary>
    /// PID kontrol arayüzünü (Başlık, Butonlar, Slider'lar) sağ panele (parent) inşa eder.
    /// </summary>
    public PIDSettingsUIManager(Transform parent, Vector2 posBase, CarLaneTracker tracker, SimulationUIController mainUI)
    {
        _tracker = tracker;

        // Başlık
        mainUI.MakeLabel(parent, "lblPID", "─── PID Kontrolörü ───", posBase, new Vector2(360, 26), 15, new Color(0.2f, 0.75f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter);

        // --- 1. KONTROLCÜ MOD BUTONLARI (P, PI, PID) ---
        float bY = posBase.y - 28;
        
        // P-Only Butonu
        _btnP = BuildModeBtn(parent, "BtnP", "P - ONLY", new Vector2(10, bY), new Vector2(110, 30), mainUI, out _imgP);
        _btnP.onClick.AddListener(() => SetMode(PIDController.ControllerMode.P_Only));

        // PI-Only Butonu
        _btnPI = BuildModeBtn(parent, "BtnPI", "PI - ONLY", new Vector2(130, bY), new Vector2(110, 30), mainUI, out _imgPI);
        _btnPI.onClick.AddListener(() => SetMode(PIDController.ControllerMode.PI_Only));

        // Tam PID Butonu
        _btnPID = BuildModeBtn(parent, "BtnPID", "PID (FULL)", new Vector2(250, bY), new Vector2(110, 30), mainUI, out _imgPID);
        _btnPID.onClick.AddListener(() => SetMode(PIDController.ControllerMode.PID));

        // --- 2. KATSAYI AYAR (SLIDER & INPUT) ALANLARI ---
        float y = bY - 42;
        
        // Kp (Oransal) Ayarları (0 ile 20 arası)
        mainUI.MakeLabel(parent, "lblKp", "Kp:", new Vector2(10, y - 4), new Vector2(30, 30), 15, Color.white, FontStyle.Bold);
        _inP = mainUI.MakeInputField(parent, "inKp", new Vector2(40, y), new Vector2(50, 26));
        _slP = mainUI.MakeSliderAt(parent, "slKp", new Vector2(100, y), new Vector2(260, 26), 0f, 20f, true);
        
        y -= 34; // Aşağı kay

        // Ki (İntegral) Ayarları (0 ile 5 arası)
        mainUI.MakeLabel(parent, "lblKi", "Ki:", new Vector2(10, y - 4), new Vector2(30, 30), 15, Color.white, FontStyle.Bold);
        _inI = mainUI.MakeInputField(parent, "inKi", new Vector2(40, y), new Vector2(50, 26));
        _slI = mainUI.MakeSliderAt(parent, "slKi", new Vector2(100, y), new Vector2(260, 26), 0f, 5f, true);

        y -= 34; // Aşağı kay

        // Kd (Türevsel) Ayarları (0 ile 10 arası)
        mainUI.MakeLabel(parent, "lblKd", "Kd:", new Vector2(10, y - 4), new Vector2(30, 30), 15, Color.white, FontStyle.Bold);
        _inD = mainUI.MakeInputField(parent, "inKd", new Vector2(40, y), new Vector2(50, 26));
        _slD = mainUI.MakeSliderAt(parent, "slKd", new Vector2(100, y), new Vector2(260, 26), 0f, 10f, true);

        // Form işlemlerini (Slider kaydırma vs) sisteme bağla
        BindEvents();
    }

    /// <summary>
    /// Mod seçim butonlarını yaratan pratik yardımcı fonksiyon.
    /// </summary>
    private Button BuildModeBtn(Transform parent, string name, string text, Vector2 pos, Vector2 size, SimulationUIController mainUI, out Image img)
    {
        // Buton objesi oluştur
        var go = new GameObject(name); 
        go.transform.SetParent(parent, false);
        
        // Arka plan rengini (Image) ayarla
        img = go.AddComponent<Image>(); 
        img.color = _btnInactiveColor;
        
        // Pozisyonla
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // İçindeki yazıyı oluştur (Anchor'ları genişleyecek şekilde ayarla)
        var txtObj = mainUI.MakeLabel(go.transform, "txt", text, Vector2.zero, Vector2.zero, 13, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        var txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; 
        txtRt.sizeDelta = Vector2.zero;

        // Son olarak objeye tıklanabilir Button bileşenini ekle ve gönder
        return go.AddComponent<Button>();
    }

    /// <summary>
    /// Slider ve Input alanlarının araca bağlanması (Değişken senkronizasyonu).
    /// </summary>
    private void BindEvents()
    {
        if (_tracker == null || _tracker.pid == null) return;
        var p = _tracker.pid;

        // 1. MEVCUT DEĞERLERİ UI'YA YANSIT
        _slP.value = p.Kp; _inP.text = p.Kp.ToString("F2");
        _slI.value = p.Ki; _inI.text = p.Ki.ToString("F2");
        _slD.value = p.Kd; _inD.text = p.Kd.ToString("F2");

        // UI açıldığında arabanın mevcut moduna göre buton renklerini yak
        SetMode(p.mode);

        // 2. SLIDER DEĞİŞTİĞİNDE ARABAYI VE INPUT KUTUSUNU GÜNCELLE
        _slP.onValueChanged.AddListener(v => { p.Kp = v; if (!_inP.isFocused) _inP.text = v.ToString("F2"); });
        _slI.onValueChanged.AddListener(v => { p.Ki = v; if (!_inI.isFocused) _inI.text = v.ToString("F2"); });
        _slD.onValueChanged.AddListener(v => { p.Kd = v; if (!_inD.isFocused) _inD.text = v.ToString("F2"); });

        // 3. INPUT KUTUSUNA YAZI YAZILDIĞINDA SLIDER'I GÜNCELLE (Sınırlandırarak)
        _inP.onEndEdit.AddListener(s => { if (float.TryParse(s, out float v)) _slP.value = Mathf.Clamp(v, 0, 20); });
        _inI.onEndEdit.AddListener(s => { if (float.TryParse(s, out float v)) _slI.value = Mathf.Clamp(v, 0, 5); });
        _inD.onEndEdit.AddListener(s => { if (float.TryParse(s, out float v)) _slD.value = Mathf.Clamp(v, 0, 10); });
    }

    /// <summary>
    /// Kullanıcı butonlara tıkladığında kontrolcü modunu değiştirir ve grafikleri/geçmişi sıfırlar.
    /// </summary>
    private void SetMode(PIDController.ControllerMode mode)
    {
        if (_tracker == null) return;
        
        // Arabanın PID modunu değiştir
        _tracker.pid.mode = mode;
        
        // Mod değiştiği için geçmiş integral/türev hesaplarını sıfırla ki araç aniden sapıtmasın
        _tracker.pid.Reset();

        // Buton renklerini ayarla (Seçilen Mavi, Diğerleri Gri)
        _imgP.color = (mode == PIDController.ControllerMode.P_Only) ? _btnActiveColor : _btnInactiveColor;
        _imgPI.color = (mode == PIDController.ControllerMode.PI_Only) ? _btnActiveColor : _btnInactiveColor;
        _imgPID.color = (mode == PIDController.ControllerMode.PID) ? _btnActiveColor : _btnInactiveColor;

        // Eğer o modda kullanılmayan katsayılar varsa arayüzde kilitle (Interactable = false)
        // Örneğin P modundaysak Integral ve Derivative ayarları kapatılmalı.
        bool enableI = (mode == PIDController.ControllerMode.PI_Only || mode == PIDController.ControllerMode.PID);
        bool enableD = (mode == PIDController.ControllerMode.PID);

        _slI.interactable = enableI; _inI.interactable = enableI;
        _slD.interactable = enableD; _inD.interactable = enableD;
    }
}
