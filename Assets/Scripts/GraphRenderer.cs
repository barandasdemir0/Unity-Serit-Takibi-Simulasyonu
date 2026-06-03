using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sol paneldeki 6 adet sinyal grafiğinin verilerini alıp ekrana çizen yönetici sınıf.
/// BAĞLANTI: Sadece verileri yönetir, asıl çizim işlemini (matematiği) BresenhamLineDrawer.cs'e devreder.
/// </summary>
public class GraphRenderer : MonoBehaviour
{
    // BAĞLANTI: Verileri okuyacağımız ana orkestratör
    public CarLaneTracker carTracker;

    // Arayüzde (UI) grafiklerin bağlanacağı RawImage bileşenleri (GraphPanelUIManager.cs tarafından atanır)
    public RawImage imgError, imgControl, imgPath, imgP, imgI, imgD;
    
    // Grafiğin piksel cinsinden çözünürlüğü (Genişlik ve Yükseklik)
    public int graphWidth = 380;
    public int graphHeight = 100;

    // Grafiklerin arka plan (siyah) resimleri (Texture) ve bunların içindeki pikseller (Color32 dizileri)
    private Texture2D texError, texControl, texPath, texP, texI, texD;
    private Color32[] pixError, pixControl, pixPath, pixP, pixI, pixD;

    // Çizgi renkleri
    private Color32 lineColor = new Color32(0, 255, 0, 255); // Yeşil: Veri çizgisi
    private Color32 centerLineColor = new Color32(100, 100, 100, 255); // Gri: Orta 0 çizgisi
    private Color32 pathRefColor = new Color32(255, 255, 255, 255); // Beyaz: Referans çizgisi
    private Color32 vehiclePosColor = new Color32(255, 0, 0, 255); // Kırmızı: Aracın gerçek konumu çizgisi

    // Grafikte verinin yatay (X ekseni) düzlemde hangi pikselden çizilmeye başlanacağı
    private int currentX = 0;

    /// <summary>
    /// Siyah boş grafik zeminlerini (Texture2D) oluşturur.
    /// BAĞLANTI: SimulationUIController.cs Start() içinden çağırır.
    /// </summary>
    public void InitTextures()
    {
        // Eğer arayüz elemanları atanmamışsa işlem yapma
        if (!imgError) return;

        // 6 adet boş Texture yarat
        texError = NewTex(); texControl = NewTex(); texPath = NewTex();
        texP = NewTex(); texI = NewTex(); texD = NewTex();

        // Pikselleri hafızaya al
        pixError = texError.GetPixels32(); pixControl = texControl.GetPixels32();
        pixPath = texPath.GetPixels32(); pixP = texP.GetPixels32();
        pixI = texI.GetPixels32(); pixD = texD.GetPixels32();

        // Texture'ları UI elemanlarına bağla (Ekranda görünmesi için)
        imgError.texture = texError; imgControl.texture = texControl; imgPath.texture = texPath;
        imgP.texture = texP; imgI.texture = texI; imgD.texture = texD;
    }

    /// <summary>
    /// Her kare (frame) grafikleri bir adım daha çizer.
    /// </summary>
    void Update()
    {
        // Eğer sistem duraklatılmışsa veya veri yoksa çizim yapma
        if (carTracker == null || !carTracker.IsRunning) return;
        if (carTracker.Recorder.RecordedData.Count < 2) return;

        // X ekseninde (Zaman) sona geldiğinde grafiği 1 piksel sola kaydır (Sürekli akan grafik)
        if (currentX >= graphWidth)
        {
            ShiftLeft(pixError); ShiftLeft(pixControl); ShiftLeft(pixPath);
            ShiftLeft(pixP); ShiftLeft(pixI); ShiftLeft(pixD);
            currentX = graphWidth - 1;
        }

        // Son 2 karedeki veriyi al (Çizgi çekmek için iki noktaya ihtiyaç var)
        var dataList = carTracker.Recorder.RecordedData;
        var curr = dataList[dataList.Count - 1];
        var prev = dataList[dataList.Count - 2];

        // 1. GRAFİK: Hata (Error) Çizimi (Y ekseni -5 ile +5 arası)
        DrawSingle(pixError, texError, curr.lateralError, prev.lateralError, 5f);
        
        // 2. GRAFİK: PID Çıkışı (Control Output) Çizimi (Y ekseni -1 ile +1 arası)
        DrawSingle(pixControl, texControl, curr.controlOutput, prev.controlOutput, 1f);
        
        // 3. GRAFİK: Referans ve Araç Konumu Çizimi
        DrawPathGraph(pixPath, texPath, curr.vehiclePosX, curr.referencePosX, prev.vehiclePosX, prev.referencePosX, 5f);
        
        // 4, 5, 6. GRAFİKLER: P, I ve D terimlerinin bağımsız çizimleri (Maksimum 5 aralığında)
        DrawSingle(pixP, texP, curr.pTerm, prev.pTerm, 5f);
        DrawSingle(pixI, texI, curr.iTerm, prev.iTerm, 5f);
        DrawSingle(pixD, texD, curr.dTerm, prev.dTerm, 5f);

        // X ekseninde bir piksel ilerle
        currentX++;
    }

    /// <summary>
    /// Tek bir veri (Örn: Hata) için piksel dizisine çizgi çizer.
    /// </summary>
    private void DrawSingle(Color32[] pixels, Texture2D tex, float value, float prevValue, float maxVal)
    {
        // Orta sıfır çizgisini çiz (Gri)
        int centerY = graphHeight / 2;
        pixels[centerY * graphWidth + currentX] = centerLineColor;

        int x = currentX;
        int prevX = currentX - 1;

        // -maxVal ile +maxVal arasındaki float değeri 0 ile graphHeight piksel aralığına çevir
        int y = ValueToPixelY(value, maxVal);
        int prevY = ValueToPixelY(prevValue, maxVal);

        // Önceki noktadan mevcut noktaya (PrevX,PrevY -> X,Y) çizgi çek
        if (prevX >= 0 && prevY >= 0)
        {
            // BAĞLANTI: Asıl piksel boyama işlemini matematik sınıfına devret
            BresenhamLineDrawer.DrawLine(pixels, graphWidth, graphHeight, prevX, prevY, x, y, lineColor);
        }

        // Değişiklikleri texture'a (ekrana) yansıt
        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    /// <summary>
    /// Referans (Beyaz) ve Gerçek (Kırmızı) konumları aynı anda çizen özel metod.
    /// </summary>
    private void DrawPathGraph(Color32[] pixels, Texture2D tex, float value, float refVal, float prevValue, float prevRef, float maxY)
    {
        int centerY = graphHeight / 2;
        pixels[centerY * graphWidth + currentX] = centerLineColor;

        int x = currentX;
        int prevX = currentX - 1;

        // Kırmızı Çizgi (Araç)
        int y = ValueToPixelY(value, maxY);
        int prevY = ValueToPixelY(prevValue, maxY);
        if (prevX >= 0 && prevY >= 0)
        {
            BresenhamLineDrawer.DrawLine(pixels, graphWidth, graphHeight, prevX, prevY, x, y, vehiclePosColor);
        }

        // Beyaz Çizgi (Hedef Yol)
        int ry = ValueToPixelY(refVal, maxY);
        int preyY = ValueToPixelY(prevRef, maxY);
        if (prevX >= 0 && preyY >= 0)
        {
            BresenhamLineDrawer.DrawLine(pixels, graphWidth, graphHeight, prevX, preyY, x, ry, pathRefColor);
        }

        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    /// <summary>
    /// Float formatındaki (Örn: -2.3) bir değeri grafiğin Y ekseninde kaçıncı piksele denk geldiğine çevirir.
    /// </summary>
    private int ValueToPixelY(float val, float maxVal)
    {
        // Değeri -maxVal ile +maxVal arasına sınırla (Grafikten taşmasın)
        val = Mathf.Clamp(val, -maxVal, maxVal);
        
        // [-maxVal, maxVal] aralığındaki değeri [0, 1] aralığına normalize et
        float normalized = (val + maxVal) / (2f * maxVal);
        
        // 0-1 arasını grafiğin yüksekliği ile (Örn: 100 piksel) çarp
        return Mathf.Clamp(Mathf.RoundToInt(normalized * graphHeight), 0, graphHeight - 1);
    }

    /// <summary>
    /// Yeni bir Texture2D yaratır.
    /// </summary>
    private Texture2D NewTex()
    {
        return new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
    }

    /// <summary>
    /// Piksel dizisinin içini tamamen şeffaf siyaha boyayarak (Clear) temizler.
    /// </summary>
    private void ClearTex(Color32[] pixels, Texture2D tex)
    {
        Color32 bg = new Color32(10, 10, 10, 200);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    /// <summary>
    /// Grafiği bir piksel sola kaydırır (Sürekli akış hissi için).
    /// </summary>
    private void ShiftLeft(Color32[] pixels)
    {
        Color32 bg = new Color32(10, 10, 10, 200);
        for (int y = 0; y < graphHeight; y++)
        {
            int rowStart = y * graphWidth;
            // Tüm satırı 1 piksel sola kaydır
            System.Array.Copy(pixels, rowStart + 1, pixels, rowStart, graphWidth - 1);
            // Satırın en sonundaki pikseli arka plan rengiyle temizle
            pixels[rowStart + graphWidth - 1] = bg;
        }
    }
}
