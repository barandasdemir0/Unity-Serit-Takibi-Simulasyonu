using UnityEngine;

/// <summary>
/// Texture2D kullanarak yüksek performanslı gerçek zamanlı grafik çizici.
/// Kontrol sistemi sinyallerini (e(t), u(t), r(t), y(t)) doğrudan
/// texture üzerine kayan zaman serisi grafikleri olarak çizer.
/// 
/// İki grafik çizilir:
///   1. Kontrol Çıkışı u(t) — PID direksiyon sinyalini zaman içinde gösterir
///   2. Yanal Hata e(t) — izleme hatasını r(t)=0 referans çizgisiyle birlikte gösterir
/// 
/// Performans: Hızlı piksel işleme için SetPixels32 kullanılır,
/// yalnızca yeni veri geldiğinde yeniden çizim yapılır.
/// </summary>
public class GraphRenderer : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Grafik Boyutları
    // ─────────────────────────────────────────────
    [Header("Grafik Ayarları")]
    [Tooltip("Her grafik texturenin piksel cinsinden genişliği")]
    public int graphWidth = 400;

    [Tooltip("Her grafik texturenin piksel cinsinden yüksekliği")]
    public int graphHeight = 150;

    [Tooltip("Ekranda görüntülenen veri noktası sayısı")]
    public int visibleDataPoints = 300;

    // ─────────────────────────────────────────────
    // Renkler (AMOLED uyumlu koyu tema)
    // ─────────────────────────────────────────────
    [Header("Renkler")]
    public Color backgroundColor = new Color(0.06f, 0.06f, 0.12f, 0.9f);     // Neredeyse siyah
    public Color gridColor = new Color(0.15f, 0.15f, 0.25f, 0.5f);            // Hafif ızgara
    public Color axisColor = new Color(0.3f, 0.3f, 0.5f, 0.8f);               // Eksen çizgileri
    public Color controlOutputColor = new Color(0f, 0.9f, 1f, 1f);            // u(t) için camgöbeği
    public Color lateralErrorColor = new Color(1f, 0.3f, 0.3f, 1f);           // e(t) için kırmızı
    public Color referenceColor = new Color(0.2f, 1f, 0.4f, 0.6f);            // r(t)=0 için yeşil
    public Color pTermColor = new Color(1f, 0.6f, 0f, 1f);                    // P terimi için turuncu
    public Color iTermColor = new Color(0.8f, 0.2f, 1f, 1f);                  // I terimi için mor
    public Color dTermColor = new Color(1f, 1f, 0f, 1f);                      // D terimi için sarı

    // ─────────────────────────────────────────────
    // Ölçek (Scale)
    // ─────────────────────────────────────────────
    [Header("Ölçek")]
    [Tooltip("Kontrol çıkış grafiği için Y ekseni aralığı")]
    public float controlOutputRange = 1.5f;

    [Tooltip("Yanal hata grafiği için Y ekseni aralığı")]
    public float lateralErrorRange = 5f;

    [Tooltip("PID bileşen grafikleri için Y ekseni aralığı")]
    public float pidComponentRange = 5f;

    // ─────────────────────────────────────────────
    // Referanslar
    // ─────────────────────────────────────────────
    [Header("Referanslar")]
    public VehicleController vehicleController;

    // ─────────────────────────────────────────────
    // Çıkış Texture'ları (UI RawImage veya materyale atanır)
    // ─────────────────────────────────────────────
    private Texture2D _controlOutputTexture;
    private Texture2D _lateralErrorTexture;
    private Texture2D _pGraphTexture;
    private Texture2D _iGraphTexture;
    private Texture2D _dGraphTexture;
    private Texture2D _pathComparisonTexture;

    private Color32[] _controlPixels;
    private Color32[] _errorPixels;
    private Color32[] _pPixels;
    private Color32[] _iPixels;
    private Color32[] _dPixels;
    private Color32[] _pathPixels;

    /// <summary>Zaman içinde u(t) kontrol çıkışını gösteren texture</summary>
    public Texture2D ControlOutputTexture => _controlOutputTexture;

    /// <summary>Zaman içinde e(t) yanal hatasını gösteren texture</summary>
    public Texture2D LateralErrorTexture => _lateralErrorTexture;

    public Texture2D PGraphTexture => _pGraphTexture;
    public Texture2D IGraphTexture => _iGraphTexture;
    public Texture2D DGraphTexture => _dGraphTexture;

    /// <summary>r(t) ile y(t) karşılaştırmasını gösteren texture — Referans ve gerçek yanal konumun aynı eksende karşılaştırması</summary>
    public Texture2D PathComparisonTexture => _pathComparisonTexture;

    private void Awake()
    {
        CreateTextures();
    }

    private void Update()
    {
        if (vehicleController == null) return;

        RedrawControlOutputGraph();
        RedrawLateralErrorGraph();
        RedrawPIDGraphs();
        RedrawPathComparisonGraph();
    }

    /// <summary>
    /// Grafik texture'larını uygun ayarlarla başlatır.
    /// </summary>
    private void CreateTextures()
    {
        _controlOutputTexture  = CreateGraphTexture();
        _lateralErrorTexture   = CreateGraphTexture();
        _pGraphTexture         = CreateGraphTexture();
        _iGraphTexture         = CreateGraphTexture();
        _dGraphTexture         = CreateGraphTexture();
        _pathComparisonTexture = CreateGraphTexture();

        int pixelCount = graphWidth * graphHeight;
        _controlPixels = new Color32[pixelCount];
        _errorPixels   = new Color32[pixelCount];
        _pPixels       = new Color32[pixelCount];
        _iPixels       = new Color32[pixelCount];
        _dPixels       = new Color32[pixelCount];
        _pathPixels    = new Color32[pixelCount];
    }

    private Texture2D CreateGraphTexture()
    {
        var tex = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    /// <summary>
    /// En güncel verilerle u(t) kontrol çıkış grafiğini yeniden çizer.
    /// PID kontrolcüsünün zaman içindeki direksiyon komutunu gösterir.
    /// </summary>
    private void RedrawControlOutputGraph()
    {
        var data = vehicleController.RecordedData;
        if (data.Count < 2) return;

        // Arka planı temizle
        Color32 bg = backgroundColor;
        Color32 grid = gridColor;
        Color32 axis = axisColor;

        for (int i = 0; i < _controlPixels.Length; i++)
            _controlPixels[i] = bg;

        // Yatay ızgara çizgilerini çiz
        DrawHorizontalGrid(_controlPixels, grid, 5);

        // Sıfır eksenini çiz (orta yatay çizgi)
        int centerY = graphHeight / 2;
        DrawHorizontalLine(_controlPixels, centerY, axis);

        // Veri eğrisini çiz
        int startIdx = Mathf.Max(0, data.Count - visibleDataPoints);
        int count = data.Count - startIdx;

        // --- Otomatik Ölçekleme (Auto-scaling) ---
        float maxU = 0.1f;
        for (int i = 0; i < count; i++)
        {
            float v = Mathf.Abs(data[startIdx + i].controlOutput);
            if (v > maxU) maxU = v;
        }
        maxU *= 1.1f; // %10 boşluk

        for (int i = 1; i < count && i < graphWidth; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx >= data.Count) break;

            // Veri değerini piksel Y koordinatına dönüştür
            float value = data[dataIdx].controlOutput;
            float prevValue = data[dataIdx - 1].controlOutput;

            int x = (int)((float)i / count * (graphWidth - 1));
            int prevX = (int)((float)(i - 1) / count * (graphWidth - 1));

            int y = ValueToPixelY(value, maxU);
            int prevY = ValueToPixelY(prevValue, maxU);

            // Ardışık noktalar arasına çizgi çek
            DrawLine(_controlPixels, prevX, prevY, x, y, controlOutputColor);
        }

        _controlOutputTexture.SetPixels32(_controlPixels);
        _controlOutputTexture.Apply();
    }

    /// <summary>
    /// En güncel verilerle e(t) yanal hata grafiğini yeniden çizer.
    /// İzleme hatasını ve r(t) = 0 referans çizgisini gösterir.
    /// </summary>
    private void RedrawLateralErrorGraph()
    {
        var data = vehicleController.RecordedData;
        if (data.Count < 2) return;

        Color32 bg = backgroundColor;
        Color32 grid = gridColor;
        Color32 axis = axisColor;

        for (int i = 0; i < _errorPixels.Length; i++)
            _errorPixels[i] = bg;

        // Izgarayı çiz
        DrawHorizontalGrid(_errorPixels, grid, 5);

        // r(t) = 0 referans çizgisini çiz (orta)
        int centerY = graphHeight / 2;
        DrawHorizontalLine(_errorPixels, centerY, (Color32)referenceColor);

        // Hata eğrisini çiz
        int startIdx = Mathf.Max(0, data.Count - visibleDataPoints);
        int count = data.Count - startIdx;

        // --- Otomatik Ölçekleme (Auto-scaling) ---
        float maxE = 0.1f;
        for (int i = 0; i < count; i++)
        {
            float v = Mathf.Abs(data[startIdx + i].lateralError);
            if (v > maxE) maxE = v;
        }
        maxE *= 1.1f;

        for (int i = 1; i < count && i < graphWidth; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx >= data.Count) break;

            float value = data[dataIdx].lateralError;
            float prevValue = data[dataIdx - 1].lateralError;

            int x = (int)((float)i / count * (graphWidth - 1));
            int prevX = (int)((float)(i - 1) / count * (graphWidth - 1));

            int y = ValueToPixelY(value, maxE);
            int prevY = ValueToPixelY(prevValue, maxE);

            DrawLine(_errorPixels, prevX, prevY, x, y, lateralErrorColor);
        }

        _lateralErrorTexture.SetPixels32(_errorPixels);
        _lateralErrorTexture.Apply();
    }

    /// <summary>
    /// PID bileşen grafiklerini (P, I, D) yeniden çizer.
    /// </summary>
    private void RedrawPIDGraphs()
    {
        var data = vehicleController.RecordedData;
        if (data.Count < 2) return;

        Color32 bg = backgroundColor;
        Color32 grid = gridColor;
        Color32 axis = axisColor;

        for (int i = 0; i < _pPixels.Length; i++)
        {
            _pPixels[i] = bg;
            _iPixels[i] = bg;
            _dPixels[i] = bg;
        }

        DrawHorizontalGrid(_pPixels, grid, 3);
        DrawHorizontalGrid(_iPixels, grid, 3);
        DrawHorizontalGrid(_dPixels, grid, 3);

        int centerY = graphHeight / 2;
        DrawHorizontalLine(_pPixels, centerY, axis);
        DrawHorizontalLine(_iPixels, centerY, axis);
        DrawHorizontalLine(_dPixels, centerY, axis);

        int startIdx = Mathf.Max(0, data.Count - visibleDataPoints);
        int count = data.Count - startIdx;

        // --- Otomatik Ölçekleme (Auto-scaling) Hesaplaması ---
        float maxP = 0.1f, maxI = 0.1f, maxD = 0.1f;
        for (int i = 0; i < count; i++)
        {
            var d = data[startIdx + i];
            if (Mathf.Abs(d.pTerm) > maxP) maxP = Mathf.Abs(d.pTerm);
            if (Mathf.Abs(d.iTerm) > maxI) maxI = Mathf.Abs(d.iTerm);
            if (Mathf.Abs(d.dTerm) > maxD) maxD = Mathf.Abs(d.dTerm);
        }
        // Üstte ve altta biraz boşluk bırakmak için %10 pay (padding)
        maxP *= 1.1f;
        maxI *= 1.1f;
        maxD *= 1.1f;

        for (int i = 1; i < count && i < graphWidth; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx >= data.Count) break;

            var current = data[dataIdx];
            var prev = data[dataIdx - 1];

            int x = (int)((float)i / count * (graphWidth - 1));
            int prevX = (int)((float)(i - 1) / count * (graphWidth - 1));

            // P terimi (Dinamik Ölçekli)
            int yP = ValueToPixelY(current.pTerm, maxP);
            int prevYP = ValueToPixelY(prev.pTerm, maxP);
            DrawLine(_pPixels, prevX, prevYP, x, yP, pTermColor);

            // I terimi (Dinamik Ölçekli)
            int yI = ValueToPixelY(current.iTerm, maxI);
            int prevYI = ValueToPixelY(prev.iTerm, maxI);
            DrawLine(_iPixels, prevX, prevYI, x, yI, iTermColor);

            // D terimi (Dinamik Ölçekli)
            int yD = ValueToPixelY(current.dTerm, maxD);
            int prevYD = ValueToPixelY(prev.dTerm, maxD);
            DrawLine(_dPixels, prevX, prevYD, x, yD, dTermColor);
        }

        _pGraphTexture.SetPixels32(_pPixels);
        _pGraphTexture.Apply();

        _iGraphTexture.SetPixels32(_iPixels);
        _iGraphTexture.Apply();

        _dGraphTexture.SetPixels32(_dPixels);
        _dGraphTexture.Apply();
    }

    /// <summary>
    /// r(t) ve y(t) sinyallerini aynı grafik üzerinde çizer.
    /// Bu, referans merkez hattını (r(t)=0, yeşil) ile
    /// aracın gerçek yanal konumunu (y(t)=lateralError, turuncu) karşılaştırır.
    /// Raporda "referans ve gerçek izlenen yol karşılaştırması" olarak kullanılabilir.
    /// </summary>
    private void RedrawPathComparisonGraph()
    {
        var data = vehicleController.RecordedData;
        if (data.Count < 2) return;

        Color32 bg   = backgroundColor;
        Color32 grid = gridColor;
        Color32 axis = axisColor;

        // --- Arka planı temizle ---
        for (int i = 0; i < _pathPixels.Length; i++)
            _pathPixels[i] = bg;

        DrawHorizontalGrid(_pathPixels, grid, 5);

        // --- r(t) = 0 : Referans merkez hattı (yeşil, kalın) ---
        int refY = graphHeight / 2;
        DrawHorizontalLine(_pathPixels, refY,     (Color32)referenceColor);
        DrawHorizontalLine(_pathPixels, refY + 1, (Color32)referenceColor); // 2px kalınlık

        // --- y(t) : Gerçek yanal konum (turuncu) ---
        Color yColor = new Color(1f, 0.55f, 0.1f, 1f); // Turuncu — r(t)'den ayrışık renk

        int startIdx = Mathf.Max(0, data.Count - visibleDataPoints);
        int count    = data.Count - startIdx;

        // --- Otomatik Ölçekleme (Auto-scaling) ---
        float maxY = 0.1f;
        for (int i = 0; i < count; i++)
        {
            float v = Mathf.Abs(data[startIdx + i].vehiclePosX);
            if (v > maxY) maxY = v;
        }
        maxY *= 1.1f;

        for (int i = 1; i < count && i < graphWidth; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx >= data.Count) break;

            // y(t) = vehiclePosX = lateralError (r(t)=0 referansına göre yanal konum)
            float value     = data[dataIdx].vehiclePosX;
            float prevValue = data[dataIdx - 1].vehiclePosX;

            int x     = (int)((float)i / count * (graphWidth - 1));
            int prevX = (int)((float)(i - 1) / count * (graphWidth - 1));

            int y     = ValueToPixelY(value,     maxY);
            int prevY = ValueToPixelY(prevValue, maxY);

            DrawLine(_pathPixels, prevX, prevY, x, y, yColor);
        }

        _pathComparisonTexture.SetPixels32(_pathPixels);
        _pathComparisonTexture.Apply();
    }

    // ─────────────────────────────────────────────
    // Çizim Yardımcı Metotları (Drawing Helpers)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Bir sinyal değerini piksel Y koordinatına dönüştürür.
    /// Grafiğin ortası = 0, üst = +aralık, alt = -aralık.
    /// </summary>
    private int ValueToPixelY(float value, float range)
    {
        float normalized = (value / range) * 0.5f + 0.5f; // [-aralık, aralık] değerini [0, 1]'e eşler
        normalized = Mathf.Clamp01(normalized);
        return (int)(normalized * (graphHeight - 1));
    }

    /// <summary>
    /// Grafik genişliği boyunca tam bir yatay çizgi çizer.
    /// </summary>
    private void DrawHorizontalLine(Color32[] pixels, int y, Color32 color)
    {
        if (y < 0 || y >= graphHeight) return;
        for (int x = 0; x < graphWidth; x++)
        {
            pixels[y * graphWidth + x] = color;
        }
    }

    /// <summary>
    /// Eşit aralıklı yatay ızgara çizgileri çizer.
    /// </summary>
    private void DrawHorizontalGrid(Color32[] pixels, Color32 color, int divisions)
    {
        for (int d = 0; d <= divisions; d++)
        {
            int y = (int)((float)d / divisions * (graphHeight - 1));
            DrawHorizontalLine(pixels, y, color);
        }
    }

    /// <summary>
    /// Bresenham algoritmasını kullanarak iki nokta arasına çizgi çizer.
    /// Grafik eğrilerinde pürüzsüz, kenar yumuşatmaya (anti-aliasing) benzer bir görünüm sağlar.
    /// </summary>
    private void DrawLine(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
    {
        // Bresenham doğru çizme algoritması
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Sınır kontrolü ile pikseli ayarla
            if (x0 >= 0 && x0 < graphWidth && y0 >= 0 && y0 < graphHeight)
            {
                pixels[y0 * graphWidth + x0] = color;

                // Daha iyi görünürlük için kalın çizgi çiz (2px)
                if (y0 + 1 < graphHeight)
                    pixels[(y0 + 1) * graphWidth + x0] = color;
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx)  { err += dx; y0 += sy; }
        }
    }
}
