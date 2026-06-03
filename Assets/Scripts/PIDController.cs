using UnityEngine;

/// <summary>
/// Şerit takibi için ana PID (Proportional-Integral-Derivative) Kontrolcü sınıfı.
/// BAĞLANTI: Bu sınıf matematiksel işlemlerin sadece toplanma (sum) noktasıdır.
/// İşlemleri kendi içindeki 3 ayrı modüle (ProportionalTerm, IntegralTerm, DerivativeTerm) devreder.
/// </summary>
[System.Serializable]
public class PIDController
{
    // Arayüzden seçilebilen P, PI veya PID modları
    public enum ControllerMode { P_Only, PI_Only, PID }

    [Header("Kontrolcü Modu")]
    public ControllerMode mode = ControllerMode.PID;

    // Kullanıcı arayüzünden (PIDSettingsUIManager.cs) gelen Kp, Ki, Kd çarpanları
    [Header("PID Kazanç Değerleri")]
    [Range(0f, 20f)] public float Kp = 1.2f;
    [Range(0f, 5f)]  public float Ki = 0.3f;
    [Range(0f, 10f)] public float Kd = 2.5f;

    // İntegral şişmesini engelleyen Windup limiti ve maksimum direksiyon çıkış limiti
    [Header("Sınırlar")]
    public float integralMax = 10f;
    public float outputMax = 1f;

    // --- ATOMİK ALT MODÜLLER (Matematik İşlemleri Burada Yapılır) ---
    private ProportionalTerm _pTerm = new ProportionalTerm();
    private IntegralTerm _iTerm = new IntegralTerm();
    private DerivativeTerm _dTerm = new DerivativeTerm();

    // Dışarıdan (GraphRenderer.cs ve DataLogger.cs tarafından) okunabilen salt-okunur veriler
    public float LastError { get; private set; }
    public float LastProportional { get; private set; }
    public float LastIntegral { get; private set; }
    public float LastDerivative { get; private set; }
    public float LastOutput { get; private set; }

    /// <summary>
    /// Başlangıçta tüm değerleri sıfırlar.
    /// </summary>
    public PIDController() { Reset(); }

    /// <summary>
    /// Sistem yeniden başlatıldığında veya PID modu değiştiğinde geçmişi temizler.
    /// </summary>
    public void Reset()
    {
        _iTerm.Reset();
        _dTerm.Reset();
        LastError = LastProportional = LastIntegral = LastDerivative = LastOutput = 0f;
    }

    /// <summary>
    /// Mevcut yanal hatayı (error) alıp, verilmesi gereken direksiyon miktarını (output) üretir.
    /// BAĞLANTI: CarLaneTracker.cs tarafından her karede çağrılır.
    /// </summary>
    public float Compute(float error, float deltaTime)
    {
        if (deltaTime <= 0f) return LastOutput;
        
        LastError = error;

        // 1. P TERİMİ: Mevcut hataya anında verilen oransal tepki.
        LastProportional = _pTerm.Calculate(Kp, error);

        // 2. I TERİMİ: Geçmiş hataların zamanla birikimi (Kalıcı hatayı yok eder).
        // Eğer mod "P_Only" ise integral hesabı yapılmaz.
        if (mode == ControllerMode.PI_Only || mode == ControllerMode.PID)
            LastIntegral = _iTerm.Calculate(Ki, error, deltaTime, integralMax);
        else
            LastIntegral = 0f;

        // 3. D TERİMİ: Hatanın değişim hızını (türevini) ölçerek salınımı engeller.
        // Eğer mod PID değilse türev hesabı yapılmaz. (10f = Low Pass Filter frekansı)
        if (mode == ControllerMode.PID)
            LastDerivative = _dTerm.Calculate(Kd, error, deltaTime, 10f);
        else
            LastDerivative = 0f;

        // Toplam Kontrol Sinyali: u(t) = P + I + D
        float output = LastProportional + LastIntegral + LastDerivative;
        
        // Çıkışı fiziksel [-outputMax, outputMax] aralığına sıkıştır (Örn: -1 ile +1 arası)
        LastOutput = Mathf.Clamp(output, -outputMax, outputMax);

        return LastOutput;
    }
}
