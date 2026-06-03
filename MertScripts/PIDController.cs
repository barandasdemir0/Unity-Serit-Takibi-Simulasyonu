using UnityEngine;

/// <summary>
/// Şerit takibi için modüler PID Kontrolcü (PID Controller) uygulaması.
/// Bu saf bir C# sınıfıdır (MonoBehaviour değil), böylece test edilebilir ve yeniden kullanılabilir.
/// 
/// PID Kontrol Denklemi:
///   u(t) = Kp * e(t) + Ki * ∫e(t)dt + Kd * de(t)/dt
/// 
/// Parametreler:
///   e(t) = r(t) - y(t)  (Yanal hata: referans yol ile gerçek konum arasındaki fark)
///   u(t) = Direksiyon kontrol sinyali (Çıkış)
///   Kp   = Oransal kazanç (Hataya anında verilen tepki)
///   Ki   = İntegral kazancı (Geçmişte biriken kalıcı hatayı sıfırlar)
///   Kd   = Türevsel kazanç (Hatanın değişim hızını yavaşlatır, salınımı engeller)
/// </summary>
[System.Serializable]
public class PIDController
{
    // ─────────────────────────────────────────────
    // Kontrolcü Modu — P, PI veya PID'yi ayrı ayrı test edebilmeyi sağlar
    // ─────────────────────────────────────────────
    public enum ControllerMode
    {
        P_Only,   // Sadece Oransal terim aktif
        PI_Only,  // Oransal + İntegral
        PID       // Tam PID Kontrolcü
    }

    [Header("Kontrolcü Modu")]
    [Tooltip("Hangi kontrolcü terimlerinin aktif olacağını seçin: P, PI veya PID")]
    public ControllerMode mode = ControllerMode.PID;

    // ─────────────────────────────────────────────
    // PID Kazançları — Arayüz üzerinden canlı olarak değiştirilebilir
    // ─────────────────────────────────────────────
    [Header("PID Kazanç Değerleri (Kp, Ki, Kd)")]
    [Tooltip("Oransal kazanç (Kp): Mevcut hataya verilen doğrudan tepki")]
    [Range(0f, 20f)]
    public float Kp = 1.2f;

    [Tooltip("İntegral kazanç (Ki): Birikmiş geçmiş hatayı düzeltir")]
    [Range(0f, 5f)]
    public float Ki = 0.0f;

    [Tooltip("Türevsel kazanç (Kd): Hata değişim oranını yavaşlatır, salınımı engeller")]
    [Range(0f, 10f)]
    public float Kd = 2.5f;

    // ─────────────────────────────────────────────
    // Anti-Windup (Sarma Önleyici) — İntegral teriminin sonsuza büyümesini engeller
    // ─────────────────────────────────────────────
    [Header("Anti-Windup (Sarma Önleyici)")]
    [Tooltip("İntegral birikiminin alabileceği maksimum mutlak değer")]
    public float integralMax = 10f;

    // ─────────────────────────────────────────────
    // Çıkış Sınırlandırması
    // ─────────────────────────────────────────────
    [Header("Çıkış Sınırları")]
    [Tooltip("Kontrol çıkışının (u(t)) alabileceği maksimum mutlak değer")]
    public float outputMax = 1f;

    // ─────────────────────────────────────────────
    // İç Durum Değişkenleri
    // ─────────────────────────────────────────────
    private float _integral;       // ∫e(t)dt — Zaman içinde biriken hata
    private float _previousError;  // e(t-1) — Bir önceki karenin hatası
    private bool  _isFirstUpdate;  // İlk karede türev sıçramasını önlemek için bayrak

    // ─────────────────────────────────────────────
    // Salt-okunur teşhis verileri (Grafikler ve UI için)
    // ─────────────────────────────────────────────
    public float LastError        { get; private set; }
    public float LastProportional { get; private set; }
    public float LastIntegral     { get; private set; }
    public float LastDerivative   { get; private set; }
    public float LastOutput       { get; private set; }

    /// <summary>
    /// Varsayılan kazanç değerleriyle yapıcı metot.
    /// </summary>
    public PIDController()
    {
        Reset();
    }

    /// <summary>
    /// Belirtilen kazanç değerleriyle yapıcı metot.
    /// </summary>
    public PIDController(float kp, float ki, float kd)
    {
        Kp = kp;
        Ki = ki;
        Kd = kd;
        Reset();
    }

    /// <summary>
    /// Kontrolcünün iç durumunu sıfırlar.
    /// Simülasyon yeniden başlatıldığında veya mod değiştirildiğinde çağrılır.
    /// </summary>
    public void Reset()
    {
        _integral = 0f;
        _previousError = 0f;
        _isFirstUpdate = true;
        LastError = 0f;
        LastProportional = 0f;
        LastIntegral = 0f;
        LastDerivative = 0f;
        LastOutput = 0f;
    }

    /// <summary>
    /// Mevcut e(t) hatasını alarak u(t) PID kontrol çıkışını hesaplar.
    /// 
    /// Formül:
    ///   P = Kp * e(t)
    ///   I = Ki * ∫e(t)dt  (Anti-windup sınırlandırması ile)
    ///   D = Kd * de(t)/dt  (İlk kare koruması ve Alçak Geçiren Filtre ile)
    ///   u(t) = P + I + D   ([-outputMax, outputMax] aralığında sınırlandırılır)
    /// </summary>
    /// <param name="error">Mevcut yanal hata e(t) = r(t) - y(t)</param>
    /// <param name="deltaTime">Zaman adımı (Genellikle Time.fixedDeltaTime)</param>
    /// <returns>Direksiyon yönlendirmesi için kontrol sinyali u(t)</returns>

    // Alçak Geçiren Filtre için iç durum değişkenleri
    private float _filteredErrorRate = 0f;
    private float _filterCutoff = 10f; // Hz cinsinden kesme frekansı

    public float Compute(float error, float deltaTime)
    {
        // Delta time sıfır veya negatifse işlemi durdur
        if (deltaTime <= 0f)
            return LastOutput;

        LastError = error;

        // ── P: Oransal Terim ──────────────────────────────────────
        float proportional = Kp * error;
        LastProportional = proportional;

        // ── I: İntegral Terimi ────────────────────────────────────
        float integral = 0f;
        if (mode == ControllerMode.PI_Only || mode == ControllerMode.PID)
        {
            // Hatayı zamanla biriktir
            _integral += error * deltaTime;
            // Anti-windup: İntegralin aşırı büyümesini engelle
            _integral = Mathf.Clamp(_integral, -integralMax, integralMax);
            integral = Ki * _integral;
        }
        LastIntegral = integral;

        // ── D: Türevsel Terim ─────────────────────────────────────
        float derivative = 0f;
        if (mode == ControllerMode.PID)
        {
            if (!_isFirstUpdate)
            {
                // Hatanın değişim hızını (türevini) hesapla
                float rawErrorRate = (error - _previousError) / deltaTime;

                // Türev kaynaklı ani sıçramaları engellemek için Alçak Geçiren Filtre uygula
                // alpha = dt / (RC + dt), burada RC = 1 / (2*pi*fc)
                float rc = 1f / (2f * Mathf.PI * _filterCutoff);
                float alpha = deltaTime / (rc + deltaTime);
                _filteredErrorRate = _filteredErrorRate + alpha * (rawErrorRate - _filteredErrorRate);

                derivative = Kd * _filteredErrorRate;
            }
        }
        LastDerivative = derivative;

        // Geçmiş hatayı güncelle
        _previousError = error;
        _isFirstUpdate = false;

        // ── Toplam Kontrol Çıkışı (u(t)) ─────────────────────────
        float output = proportional + integral + derivative;
        // Çıkışı direksiyon fiziksel sınırlarına göre kısıtla
        output = Mathf.Clamp(output, -outputMax, outputMax);

        LastOutput = output;
        return output;
    }
}
