using UnityEngine;

[System.Serializable]
public class PIDController
{
    public enum ControllerMode { P, PI, PID }

    [Header("Kontrolcü Modu (P / PI / PID)")]
    public ControllerMode mode = ControllerMode.PID;

    [Header("PID Katsayıları")]
    public float Kp = 5f;
    public float Ki = 0f;
    public float Kd = 3f;

    // Integral sınırı (Integral Windup önlemi)
    public float maxIntegral = 10f;

    private float integral = 0f;
    private float previousError = 0f;
    private float previousDerivative = 0f;

    /// <summary>
    /// Kontrol sinyalini (u(t)) hesaplayan ana fonksiyon.
    /// u(t) = Kp*e(t) + Ki*∫e(t)dt + Kd*(de(t)/dt)
    /// </summary>
    public float CalculateControlSignal(float error, float deltaTime)
    {
        if (deltaTime <= 0f) return 0f;

        // Oransal (Proportional) terim: Kp * e(t)
        float P = Kp * error;

        // İntegral terimi (sadece PI ve PID modunda aktif)
        float I = 0f;
        if (mode == ControllerMode.PI || mode == ControllerMode.PID)
        {
            integral += error * deltaTime;
            integral = Mathf.Clamp(integral, -maxIntegral, maxIntegral);
            I = Ki * integral;
        }

        // Türevsel terimi (sadece PID modunda aktif)
        float D = 0f;
        if (mode == ControllerMode.PID)
        {
            float rawDerivative = (error - previousError) / deltaTime;
            // Alçak geçiren filtre: gürültüyü bastırır, alpha=0.5 → hızlı tepki
            previousDerivative = Mathf.Lerp(previousDerivative, rawDerivative, 0.5f);
            D = Kd * previousDerivative;
        }

        previousError = error;

        // u(t) = P + I + D
        return P + I + D;
    }

    public void ResetController()
    {
        integral = 0f;
        previousError = 0f;
        previousDerivative = 0f;
    }
}
