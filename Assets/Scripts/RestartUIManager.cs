using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arayüzdeki (UI) "Yeniden Başlat" butonunu oluşturan ve yöneten modül.
/// BAĞLANTI: Butona basıldığında CarLaneTracker.cs içindeki ResetSimulation() çağrılır.
/// </summary>
public static class RestartUIManager
{
    /// <summary>
    /// Yeniden Başlat butonunu belirtilen pozisyona çizer ve olayları (event) bağlar.
    /// </summary>
    public static void Build(Transform parent, Vector2 pos, Vector2 size, CarLaneTracker carTracker)
    {
        // 1. BUTON OBJESİNİ OLUŞTUR
        var go = new GameObject("RestartBtn"); 
        go.transform.SetParent(parent, false);
        
        // 2. GÖRSELLİK (Mavi Renk)
        var img = go.AddComponent<Image>(); 
        img.color = new Color(0.15f, 0.45f, 0.7f);
        
        // 3. KONUMLANDIRMA
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // 4. BUTON METNİ OLUŞTUR
        var txtGo = new GameObject("Txt"); 
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>(); 
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16; txt.fontStyle = FontStyle.Bold; txt.color = Color.white; 
        txt.text = "🔄  YENİDEN BAŞLAT"; txt.alignment = TextAnchor.MiddleCenter;
        
        var trt = txtGo.GetComponent<RectTransform>(); 
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;

        // 5. TIKLAMA (ONCLICK) İŞLEMİ BAĞLA
        var btn = go.AddComponent<Button>(); 
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => {
            if (carTracker != null) 
            { 
                // Arabayı ve simülasyonu başa sar
                carTracker.ResetSimulation(); 
                
                // Kaydedilmiş CSV verilerini ve Grafikleri (RAM'i) temizle
                var logger = carTracker.GetComponent<DataLogger>();
                if (logger != null) logger.ClearData(); 
            }
        });
    }
}
