using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arayüzdeki (UI) "CSV Kaydet" butonunu oluşturan ve yöneten modül.
/// BAĞLANTI: Butona tıklandığında DataLogger.cs'teki ExportData() fonksiyonunu tetikler.
/// </summary>
public static class CSVExportUIManager
{
    /// <summary>
    /// CSV Kaydet butonunu belirtilen pozisyona çizer ve olayları (event) bağlar.
    /// </summary>
    /// <param name="parent">Butonun ekleneceği üst UI paneli (Sağ Panel)</param>
    /// <param name="pos">Butonun X ve Y koordinatları</param>
    /// <param name="size">Butonun genişlik ve yüksekliği</param>
    /// <param name="carTracker">Verilerin alınacağı ana orkestratör</param>
    public static void Build(Transform parent, Vector2 pos, Vector2 size, CarLaneTracker carTracker)
    {
        // 1. BUTON OBJESİNİ OLUŞTUR
        var go = new GameObject("ExportBtn");
        go.transform.SetParent(parent, false);
        
        // 2. GÖRSELLİK (RENK) EKLE (Koyu yeşil)
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.6f, 0.2f);
        
        // 3. KONUMLANDIRMA (Sol üst köşeye sabitleme / Anchor: Top-Left)
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // 4. BUTON METNİNİ (TEXT) OLUŞTUR
        var txtGo = new GameObject("Txt");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 15; txt.fontStyle = FontStyle.Bold; txt.color = Color.white;
        txt.text = "💾 CSV KAYDET"; txt.alignment = TextAnchor.MiddleCenter;
        
        // Metni butonun tam içine kapla (Anchor Min:0, Max:1)
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        // 5. TIKLAMA (ONCLICK) İŞLEMİNİ BAĞLA
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => {
            // Butona tıklandığında DataLogger'ı bul ve ExportData()'yı çalıştır
            if (carTracker != null)
            {
                var logger = carTracker.GetComponent<DataLogger>();
                if (logger != null) logger.ExportData();
            }
        });
    }
}
