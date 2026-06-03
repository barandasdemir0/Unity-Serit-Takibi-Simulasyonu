using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Arayüz (UI) Ana Orkestratörü.
/// BAĞLANTI: Unity'deki buton, slider gibi arayüz elemanlarını yönetir.
/// Kendisi doğrudan çizim yapmaz; UIElementFactory.cs ve çeşitli UIManager'lara (Örn: RestartUIManager.cs) işi devreder.
/// </summary>
[RequireComponent(typeof(GraphRenderer))]
public class SimulationUIController : MonoBehaviour
{
    // BAĞLANTI: Fizik ve hesaplama işlemlerini yürüten ana sınıf
    public CarLaneTracker carTracker;
    
    // BAĞLANTI: Grafik çizimlerini yürüten sınıf
    private GraphRenderer graphRenderer;
    
    // Arayüz (Sağ panel) bölümlerini yöneten alt yöneticiler
    private VehicleMassUIManager _massUI;
    private VehicleSpeedUIManager _speedUI;
    private PIDSettingsUIManager _pidUI;
    private PauseUIManager _pauseUI;
    
    // Arayüz (Sol panel - Grafikler) yöneticisi
    private GraphPanelUIManager _graphPanelUI;

    // Offset (Başlangıç hatası) için arayüz elemanları
    private Slider slOffset;
    private InputField inOffset;

    /// <summary>
    /// Obje ilk yaratıldığında çalışır (Start'tan önce).
    /// </summary>
    void Awake()
    {
        // Eğer CarLaneTracker elle atanmamışsa sahnede otomatik bul
        if (carTracker == null) carTracker = FindAnyObjectByType<CarLaneTracker>();
        
        // Kendi üzerindeki GraphRenderer bileşenini al
        graphRenderer = GetComponent<GraphRenderer>();
        if (graphRenderer.carTracker == null) graphRenderer.carTracker = carTracker;
        
        // Grafik paneli yöneticisini yarat
        _graphPanelUI = new GraphPanelUIManager();
    }

    /// <summary>
    /// Simülasyon başlarken bir kez çalışır.
    /// </summary>
    void Start()
    {
        BuildUI(); // Arayüzü inşa et
        graphRenderer.InitTextures(); // Grafik siyah ekranlarını (Texture) hazırla
    }

    /// <summary>
    /// Her kare (frame) çalışır.
    /// </summary>
    void Update()
    {
        // Aracın kütlesine göre değişen "Atalet (Inertia)" metnini arayüzde güncelle
        if (_massUI != null) _massUI.UpdateLabel();
    }

    /// <summary>
    /// Arayüzün temel çatısını (Canvas) kurar ve panelleri oluşturur.
    /// </summary>
    private void BuildUI()
    {
        // Tıklama olaylarının (Buton, Slider) çalışması için EventSystem yoksa oluştur
        EnsureEventSystem();

        // 1. Ana Tuval (Canvas) Oluştur
        Canvas canvas = new GameObject("SimCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Ekranın tam üstüne kapla
        
        // 2. Ekran çözünürlüğüne göre arayüzün otomatik boyutlanmasını (Scale) sağla
        CanvasScaler cs = canvas.gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); // 1080p referans al
        
        // 3. Tıklamaları algılaması için Raycaster ekle
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        Transform root = canvas.transform;
        
        // BAĞLANTI: Sol Paneli (Grafikleri) GraphPanelUIManager'a kurdur
        _graphPanelUI.Build(root, graphRenderer);
        
        // Sağ Paneli (Ayarlar ve Butonlar) kendimiz kur
        BuildRightPanel(root);
    }

    /// <summary>
    /// Sağ paneldeki ayarları (Hız, Kütle, PID, Butonlar) inşa eder.
    /// </summary>
    private void BuildRightPanel(Transform root)
    {
        // Sağ panelin arka planını oluştur (Koyu lacivert)
        RectTransform rightPanel = UIElementFactory.MakeRect("RightPanel", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(460, 560));
        
        // Paneli içindeki tüm elementlerle (yazılar, butonlar, barlar) birlikte %50 (1.5x) orantılı olarak büyüt
        rightPanel.localScale = new Vector3(1.5f, 1.5f, 1f);
        
        Image bg = rightPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        // ALT YÖNETİCİLERİ (MANAGER) ÇAĞIRARAK ARAYÜZÜ MODÜLER OLARAK DİZ:
        
        // 1. PID Ayarları (P, I, D katsayıları) -> PIDSettingsUIManager.cs
        _pidUI = new PIDSettingsUIManager(rightPanel, new Vector2(0, -12), carTracker, this);

        // 2. Hız Ayarları (DesiredSpeed) -> VehicleSpeedUIManager.cs
        _speedUI = new VehicleSpeedUIManager(rightPanel, new Vector2(0, -210), carTracker, this);

        // 3. Kütle Ayarları (VehicleMass) -> VehicleMassUIManager.cs
        _massUI = new VehicleMassUIManager(rightPanel, new Vector2(0, -270), carTracker, this);

        // Başlangıç Hatası (Offset) bölümü (Sadece birkaç satır olduğu için burada bırakıldı)
        UIElementFactory.MakeLabel(rightPanel, "lblOffset", "─── Başlangıç Hatası ───", new Vector2(12, -354), new Vector2(360, 26), 15, new Color(1f, 0.6f, 0.8f), FontStyle.Bold, TextAnchor.MiddleCenter);
        UIElementFactory.MakeLabel(rightPanel, "Offset_lbl", "Offset:", new Vector2(14, -382), new Vector2(50, 32), 15, Color.white);
        inOffset = UIElementFactory.MakeInputField(rightPanel, "Offset_input", new Vector2(64, -378), new Vector2(60, 26));
        slOffset = UIElementFactory.MakeSliderAt(rightPanel, "Offset_sl", new Vector2(134, -378), new Vector2(224, 26), -5f, 5f, false);
        BindOffsetEvents();

        // 4. Yeniden Başlatma Yöneticisi -> RestartUIManager.cs
        RestartUIManager.Build(rightPanel, new Vector2(12, -414), new Vector2(356, 38), carTracker);
        
        // 5. Durdurma Yöneticisi -> PauseUIManager.cs
        _pauseUI = new PauseUIManager(rightPanel, new Vector2(0, -476), new Vector2(-20, 44));
        
        // 6. CSV Yöneticisi -> CSVExportUIManager.cs
        CSVExportUIManager.Build(rightPanel, new Vector2(12, -476), new Vector2(160, 44), carTracker);
    }

    /// <summary>
    /// Başlangıç hatası (Offset) slider'ına ve input'una olayları (Event) bağlar.
    /// </summary>
    private void BindOffsetEvents()
    {
        if (carTracker == null) return;
        
        // Mevcut değeri arayüze yansıt
        slOffset.value = carTracker.InitialLateralError;
        inOffset.text = carTracker.InitialLateralError.ToString("F1");
        
        // Slider kaydırıldığında arabadaki değeri ve input textini güncelle
        slOffset.onValueChanged.AddListener(v => { carTracker.InitialLateralError = v; if (!inOffset.isFocused) inOffset.text = v.ToString("F1"); });
        
        // Input textine elle sayı girildiğinde (Enter'a basılınca) slider'ı ve arabayı güncelle
        inOffset.onEndEdit.AddListener(s => { if (float.TryParse(s, out float val)) { val = Mathf.Clamp(val, -5f, 5f); slOffset.value = val; } });
    }

    /// <summary>
    /// Unity sahnesinde EventSystem yoksa otomatik yaratır (UI Tıklamaları için şarttır).
    /// </summary>
    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        
// Unity'nin yeni Input System'i mi yoksa eski Input Manager'ı mı kullanıldığına göre doğru modülü ekler
#if ENABLE_INPUT_SYSTEM
        esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esGo.AddComponent<StandaloneInputModule>();
#endif
    }
    
    // ─────────────────────────────────────────────
    // UI FABRİKA YÖNLENDİRMELERİ (UIElementFactory.cs)
    // Diğer sınıfların (UIManager'ların) bu ana sınıf üzerinden kolayca 
    // UI üretebilmesi için aracı metodlar (Wrapper).
    // ─────────────────────────────────────────────
    
    public RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        return UIElementFactory.MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
    }

    public Text MakeLabel(Transform parent, string name, string txt, Vector2 pos, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        return UIElementFactory.MakeLabel(parent, name, txt, pos, size, fontSize, color, style, anchor);
    }

    public InputField MakeInputField(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        return UIElementFactory.MakeInputField(parent, name, pos, size);
    }

    public Slider MakeSliderAt(Transform parent, string name, Vector2 pos, Vector2 size, float min, float max, bool isPID = false)
    {
        return UIElementFactory.MakeSliderAt(parent, name, pos, size, min, max, isPID);
    }
}
