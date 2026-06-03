using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simülasyonu "Durdur / Devam Et" (Pause/Play) arayüz bileşenini yöneten sınıf.
/// BAĞLANTI: Unity'nin evrensel zaman ölçeğini (Time.timeScale) kullanarak tüm fiziği durdurur.
/// </summary>
public class PauseUIManager
{
    // Sistem duraklatıldı mı?
    private bool _isPaused = false;
    
    // UI Referansları
    private Button _pauseBtn;
    private Text _pauseBtnText;

    /// <summary>
    /// Durdur / Devam Et butonunu çizer.
    /// </summary>
    public PauseUIManager(Transform parent, Vector2 pos, Vector2 size)
    {
        // 1. BUTON OBJESİ
        var pauseGo = new GameObject("PauseBtn"); 
        pauseGo.transform.SetParent(parent, false);
        
        // 2. GÖRSELLİK (Kırmızı Renk)
        var pauseImg = pauseGo.AddComponent<Image>(); 
        pauseImg.color = new Color(0.7f, 0.2f, 0.1f);
        
        // 3. KONUMLANDIRMA (Sağ Üst Köşe / Anchor: Top-Right)
        var pauseRt = pauseGo.GetComponent<RectTransform>();
        pauseRt.anchorMin = new Vector2(1, 1); pauseRt.anchorMax = new Vector2(1, 1); pauseRt.pivot = new Vector2(1, 1);
        pauseRt.anchoredPosition = pos; pauseRt.sizeDelta = size;

        // 4. BUTON METNİ
        var pauseTxtGo = new GameObject("PauseTxt"); 
        pauseTxtGo.transform.SetParent(pauseGo.transform, false);
        
        _pauseBtnText = pauseTxtGo.AddComponent<Text>(); 
        _pauseBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _pauseBtnText.text = "⏸  DURDUR"; _pauseBtnText.color = Color.white; 
        _pauseBtnText.fontSize = 18; _pauseBtnText.fontStyle = FontStyle.Bold; _pauseBtnText.alignment = TextAnchor.MiddleCenter;
        
        var pTrt = pauseTxtGo.GetComponent<RectTransform>(); 
        pTrt.anchorMin = Vector2.zero; pTrt.anchorMax = Vector2.one; pTrt.sizeDelta = Vector2.zero;

        // 5. TIKLAMA OLAYI BAĞLANTISI
        _pauseBtn = pauseGo.AddComponent<Button>(); 
        _pauseBtn.targetGraphic = pauseImg;
        
        // Tıklandığında kendi içindeki TogglePause fonksiyonunu çağır
        _pauseBtn.onClick.AddListener(TogglePause);
    }

    /// <summary>
    /// Butona tıklandığında oyun zamanını (TimeScale) dondurur veya geri çözer.
    /// Ayrıca butonun üstündeki yazıyı ve rengi değiştirir.
    /// </summary>
    private void TogglePause()
    {
        // Durumu tersine çevir
        _isPaused = !_isPaused;
        
        // Unity'nin fizik/oyun hızını 0 yaparsak her şey donar. 1 yaparsak normale döner.
        Time.timeScale = _isPaused ? 0f : 1f;
        
        // Buton arayüzünü güncelle
        if (_pauseBtnText != null)
        {
            // Durdurulduysa Devam yaz, çalışıyorsa Durdur yaz
            _pauseBtnText.text = _isPaused ? "▶  DEVAM" : "⏸  DURDUR";
            
            // Renk ayarı: Devam (Yeşil), Durdur (Kırmızı)
            if (_pauseBtn != null) 
            {
                _pauseBtn.GetComponent<Image>().color = _isPaused ? new Color(0.1f, 0.55f, 0.15f) : new Color(0.7f, 0.2f, 0.1f);
            }
        }
    }
}
