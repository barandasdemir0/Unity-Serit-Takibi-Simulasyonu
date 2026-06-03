using UnityEngine;

/// <summary>
/// PID Kontrolcünün "D" (Türevsel / Derivative) Terimi.
/// BAĞLANTI: PIDController.cs içinden çağrılır.
/// Amacı: Hatanın değişim hızına bakarak sistemin "salınım (osilasyon) yapmasını" engeller. Frenleyici etki yaratır.
/// Formül: D = Kd * (de(t) / dt)
/// </summary>
public class DerivativeTerm
{
    private float _previousError = 0f; // e(t-1) : Bir önceki karenin hatası
    private bool _isFirstUpdate = true; // İlk karede türev sıçramasını önlemek için bayrak
    private float _filteredErrorRate = 0f; // Low Pass Filter'dan geçmiş temiz türev

    /// <summary>
    /// Simülasyon başa sarıldığında geçmiş hafızayı siler.
    /// </summary>
    public void Reset()
    {
        _previousError = 0f;
        _isFirstUpdate = true;
        _filteredErrorRate = 0f;
    }

    /// <summary>
    /// Türevi ve Low Pass Filter'ı hesaplar.
    /// </summary>
    /// <param name="kd">Kullanıcının girdiği Kd katsayısı</param>
    /// <param name="currentError">Mevcut anlık hata</param>
    /// <param name="deltaTime">Fizik motoru zaman adımı</param>
    /// <param name="filterCutoffHz">Gürültüyü engelleyecek filtrenin frekansı (Genelde 10-20 Hz)</param>
    public float Calculate(float kd, float currentError, float deltaTime, float filterCutoffHz)
    {
        // İlk karedeysek elimizde geçmiş veri yoktur, 0 döndürürüz.
        if (_isFirstUpdate)
        {
            _previousError = currentError;
            _isFirstUpdate = false;
            return 0f; 
        }

        // 1. HAM TÜREV HESABI (Raw Derivative)
        // de(t) / dt ≈ (Şu anki hata - Önceki Hata) / Geçen Zaman
        float rawErrorRate = (currentError - _previousError) / deltaTime;

        // 2. LOW PASS FILTER (ALÇAK GEÇİREN FİLTRE) UYGULAMASI
        // Türev işlemi doğası gereği gürültüye (Noise) çok duyarlıdır. Ufak bir titreme sonsuz türev üretebilir.
        // Bu yüzden yüksek frekanslı gürültüleri kesip yumuşatılmış türevi buluyoruz.
        float rc = 1f / (2f * Mathf.PI * filterCutoffHz);
        float alpha = deltaTime / (rc + deltaTime);
        
        // Yeni temizlenmiş türev = Eski temiz türev + alpha * (Ham Türev - Eski temiz türev)
        _filteredErrorRate = _filteredErrorRate + alpha * (rawErrorRate - _filteredErrorRate);

        // Bir sonraki frame için mevcut hatayı kaydet
        _previousError = currentError;

        // Temizlenmiş türevi Kd ile çarparak gönder
        return kd * _filteredErrorRate;
    }
}
