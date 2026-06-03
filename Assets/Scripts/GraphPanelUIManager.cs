using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sol paneli (Koyu arkaplan) ve o panelin içindeki 6 adet gerçek zamanlı sinyal grafiğinin 
/// iskeletlerini (Çerçeve ve Başlıklarını) oluşturan arayüz yöneticisi.
/// BAĞLANTI: Grafikler için boş çerçeveleri oluşturup GraphRenderer'a (Pixelleri boyayan sınıfa) teslim eder.
/// </summary>
public class GraphPanelUIManager
{
    /// <summary>
    /// Sol paneli inşa eder ve içindeki grafik elemanlarını (RawImage) döndürür.
    /// </summary>
    /// <param name="root">Grafiklerin bağlanacağı ana Canvas objesi</param>
    /// <param name="graphRenderer">Kendi ayarladığı resim (RawImage) çerçevelerini teslim edeceği sınıf</param>
    public void Build(Transform root, GraphRenderer graphRenderer)
    {
        // GraphRenderer'dan grafik genişlik ve yükseklik ayarlarını çek
        int gw = graphRenderer.graphWidth; 
        int gh = graphRenderer.graphHeight;
        
        // Sol panelin toplam yüksekliği (6 adet grafik + aralarındaki boşluklar)
        float panelHeight = gh * 6 + 200;

        // 1. ANA SOL PANELİ (ARKAPLAN) OLUŞTUR
        RectTransform leftPanel = UIElementFactory.MakeRect("LeftPanel", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(gw, panelHeight));
        Image bg = leftPanel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.7f); // Yarı saydam koyu arka plan
        
        // 2. ANA BAŞLIĞI OLUŞTUR
        UIElementFactory.MakeLabel(leftPanel, "title", "Gerçek Zamanlı Sinyaller", new Vector2(4, -4), new Vector2(gw - 4, 28), 17, Color.white, FontStyle.Bold);

        // Aşağı doğru dizeceğimiz için yOff (Y ekseni Offset/Kayma) kullanıyoruz
        float yOff = -36;

        // --- 1. GRAFİK : HATA (LATERAL ERROR) ---
        UIElementFactory.MakeLabel(leftPanel, "lbl1", "● Hata  e(t) = r(t) - y(t)", new Vector2(8, yOff), new Vector2(gw, 20), 14, new Color(1f, 0.4f, 0.4f), FontStyle.Bold);
        yOff -= 24; // Başlıktan sonra aşağı in
        // Çerçeveyi oluştur ve GraphRenderer'ın imgError değişkenine (referansına) eşitle ki içini o boyasın
        graphRenderer.imgError = MakeGraph(leftPanel, "GrError", new Vector2(0, yOff), gw, gh); 
        yOff -= gh + 14; // Çerçeve boyu kadar aşağı in ve boşluk bırak

        // --- 2. GRAFİK : KONTROL ÇIKIŞI (CONTROL OUTPUT / STEERING) ---
        UIElementFactory.MakeLabel(leftPanel, "lbl2", "● Kontrol Çıkışı  u(t)", new Vector2(8, yOff), new Vector2(gw, 20), 14, new Color(0f, 0.9f, 1f), FontStyle.Bold);
        yOff -= 24; 
        graphRenderer.imgControl = MakeGraph(leftPanel, "GrControl", new Vector2(0, yOff), gw, gh); 
        yOff -= gh + 14;

        // --- 3. GRAFİK : REFERANS YOL (BEYAZ) VE ARACIN GERÇEK KONUMU (KIRMIZI) ---
        UIElementFactory.MakeLabel(leftPanel, "lbl3", "● r(t) Referans vs y(t) Gerçek", new Vector2(8, yOff), new Vector2(gw, 20), 14, new Color(0.7f, 1f, 0.4f), FontStyle.Bold);
        yOff -= 24; 
        graphRenderer.imgPath = MakeGraph(leftPanel, "GrPath", new Vector2(0, yOff), gw, gh); 
        yOff -= gh + 14;

        // --- 4. GRAFİK : SADECE PROPORTIONAL (ORANSAL) TERİM ---
        UIElementFactory.MakeLabel(leftPanel, "lbl4", "● P Terimi  Kp·e(t)", new Vector2(8, yOff), new Vector2(gw, 20), 14, new Color(1f, 0.6f, 0f), FontStyle.Bold);
        yOff -= 24; 
        graphRenderer.imgP = MakeGraph(leftPanel, "GrP", new Vector2(0, yOff), gw, gh); 
        yOff -= gh + 14;

        // --- 5. GRAFİK : SADECE INTEGRAL TERİM ---
        UIElementFactory.MakeLabel(leftPanel, "lbl5", "● I Terimi  Ki·∫e(t)dt", new Vector2(8, yOff), new Vector2(gw, 20), 14, new Color(0.8f, 0.2f, 1f), FontStyle.Bold);
        yOff -= 24; 
        graphRenderer.imgI = MakeGraph(leftPanel, "GrI", new Vector2(0, yOff), gw, gh); 
        yOff -= gh + 14;

        // --- 6. GRAFİK : SADECE DERIVATIVE (TÜREVSEL) TERİM ---
        UIElementFactory.MakeLabel(leftPanel, "lbl6", "● D Terimi  Kd·de/dt", new Vector2(8, yOff), new Vector2(gw, 20), 14, new Color(1f, 1f, 0f), FontStyle.Bold);
        yOff -= 24; 
        graphRenderer.imgD = MakeGraph(leftPanel, "GrD", new Vector2(0, yOff), gw, gh);
    }

    /// <summary>
    /// RawImage (Piksellerin boyanacağı tuval) oluşturur.
    /// </summary>
    private RawImage MakeGraph(Transform parent, string name, Vector2 pos, int w, int h)
    {
        return UIElementFactory.MakeRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(w, h)).gameObject.AddComponent<RawImage>();
    }
}
