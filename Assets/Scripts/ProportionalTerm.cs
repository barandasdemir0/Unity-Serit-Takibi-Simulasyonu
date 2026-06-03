using UnityEngine;

/// <summary>
/// PID Kontrolcünün "P" (Oransal / Proportional) Terimi.
/// BAĞLANTI: PIDController.cs içinden çağrılır.
/// Amacı: Sistemin mevcut hataya ne kadar agresif tepki vereceğini belirler.
/// Formül: P = Kp * e(t)
/// </summary>
public class ProportionalTerm
{
    /// <summary>
    /// Oransal terimi hesaplar. Doğrudan hata ile katsayıyı çarpar.
    /// </summary>
    /// <param name="kp">Kullanıcının UI üzerinden girdiği Kp katsayısı</param>
    /// <param name="currentError">Aracın şeride olan anlık uzaklığı</param>
    public float Calculate(float kp, float currentError)
    {
        return kp * currentError;
    }
}
