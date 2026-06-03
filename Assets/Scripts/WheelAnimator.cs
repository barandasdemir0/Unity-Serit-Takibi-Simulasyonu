using UnityEngine;

/// <summary>
/// Aracın tekerleklerini hızına göre döndüren ve direksiyon açısına göre çeviren modül.
/// BAĞLANTI: CarLaneTracker.cs içinden her kare (frame) çağrılır.
/// </summary>
public static class WheelAnimator
{
    /// <summary>
    /// Tekerlek animasyonlarını gerçekleştirir.
    /// </summary>
    /// <param name="fl">Ön Sol Tekerlek Transformu</param>
    /// <param name="fr">Ön Sağ Tekerlek Transformu</param>
    /// <param name="bl">Arka Sol Tekerlek Transformu</param>
    /// <param name="br">Arka Sağ Tekerlek Transformu</param>
    /// <param name="speed">Aracın anlık hızı</param>
    /// <param name="steeringAngle">Hesaplanan direksiyon açısı</param>
    /// <param name="multiplier">Tekerleğin dönüş hızı çarpanı</param>
    /// <param name="dt">Fizik motoru zaman adımı (DeltaTime)</param>
    public static void Animate(Transform fl, Transform fr, Transform bl, Transform br, float speed, float steeringAngle, float multiplier, float dt)
    {
        // Tekerleğin kendi ekseni etrafında (ileri-geri) dönme açısını hesapla
        float rollAngle = speed * multiplier * dt;
        
        // Arka tekerlekleri sadece ileri yönde (X ekseni etrafında) döndür
        if (bl) bl.Rotate(Vector3.right, rollAngle, Space.Self);
        if (br) br.Rotate(Vector3.right, rollAngle, Space.Self);

        // Ön tekerlekler hem döner (rollAngle) hem de sağa/sola çevrilir (steeringAngle)
        if (fl) fl.localRotation = Quaternion.Euler(fl.localEulerAngles.x + rollAngle, steeringAngle, 0f);
        if (fr) fr.localRotation = Quaternion.Euler(fr.localEulerAngles.x + rollAngle, steeringAngle, 0f);
    }
}
