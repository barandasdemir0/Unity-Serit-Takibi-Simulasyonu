using UnityEngine;

/// <summary>
/// PID Kontrolcünün "I" (İntegral) Terimi.
/// BAĞLANTI: PIDController.cs içinden çağrılır.
/// Amacı: Sadece P ve D kullanıldığında virajlarda oluşan "Kalıcı Hatayı" (Steady-State Error) 
/// zamanla biriktirerek aracı tam merkeze çeker.
/// Formül: I = Ki * ∫e(t)dt
/// </summary>
public class IntegralTerm
{
    // Zaman içindeki hataların toplamını tutan değişken
    private float _integralSum = 0f;

    /// <summary>
    /// Simülasyon başa sarıldığında geçmiş hafızayı siler.
    /// </summary>
    public void Reset()
    {
        _integralSum = 0f;
    }

    /// <summary>
    /// İntegrali (birikimi) hesaplar.
    /// </summary>
    /// <param name="ki">Kullanıcının girdiği Ki katsayısı</param>
    /// <param name="currentError">Mevcut anlık hata</param>
    /// <param name="deltaTime">Fizik motoru zaman adımı (Hatanın biriktiği süre)</param>
    /// <param name="maxLimit">Anti-Windup (Sarma Önleyici) için maksimum sınır</param>
    public float Calculate(float ki, float currentError, float deltaTime, float maxLimit)
    {
        // 1. İNTEGRAL ALMA: Hata * Geçen Zaman (Dikdörtgenler yöntemiyle sürekli toplama)
        _integralSum += currentError * deltaTime;

        // 2. ANTI-WINDUP (SARMA ÖNLEYİCİ) KORUMASI:
        // Araç şeritten çok uzun süre koptuğunda integralin sonsuza gitmesini ve
        // sonrasında aracı feci şekilde ters yöne savurmasını engeller.
        _integralSum = Mathf.Clamp(_integralSum, -maxLimit, maxLimit);

        // Biriken hatayı katsayı ile çarparak gönder
        return ki * _integralSum;
    }
}
