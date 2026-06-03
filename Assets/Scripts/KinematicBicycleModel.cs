using UnityEngine;

/// <summary>
/// Aracın hareketini modelleyen Kinematik Bisiklet Modeli (Kinematic Bicycle Model).
/// BAĞLANTI: CarLaneTracker.cs tarafından PID çıkışını (direksiyon açısını) fiziksel hıza dönüştürmek için kullanılır.
/// Aracın tekerlek mesafesine ve hızına bağlı olarak dönüş yarıçapını (slip angle) hesaplar.
/// </summary>
public class KinematicBicycleModel
{
    // Dingil mesafesi (Ön ve arka teker arası mesafe)
    public float WheelBase { get; set; } = 2.5f;
    
    // Aracın fiziksel olarak dönebileceği maksimum direksiyon açısı (Derece)
    public float MaxSteeringAngle { get; set; } = 35f;
    
    // Direksiyonun ne kadar hızlı dönebileceği sınırı (Derece/saniye)
    public float MaxSteeringSpeed { get; set; } = 150f;

    // Şu anki anlık direksiyon açısı
    public float CurrentSteeringAngle { get; private set; }
    
    // Aracın dünya üzerindeki yönelim açısı (Heading)
    private float _headingAngle;

    /// <summary>
    /// Modeli başlangıç yönüne sıfırlar.
    /// BAĞLANTI: ResetUIManager.cs butona bastığında CarLaneTracker aracılığıyla çağrılır.
    /// </summary>
    public void Reset(float initialHeading)
    {
        _headingAngle = initialHeading;
        CurrentSteeringAngle = 0f;
    }

    /// <summary>
    /// Bisiklet modelinin hareket denklemlerini kullanarak aracın YENİ konumunu hesaplar.
    /// </summary>
    /// <param name="pos">Mevcut pozisyon</param>
    /// <param name="speed">Mevcut hız (m/s)</param>
    /// <param name="targetSteeringAngle">PID tarafından hedeflenen direksiyon açısı</param>
    /// <param name="inertiaFactor">Atalet çarpanı (KütleManager'dan gelir, direksiyonu yavaşlatır)</param>
    /// <param name="dt">Geçen zaman (DeltaTime)</param>
    /// <param name="newPos">Dışarıya verilecek YENİ pozisyon</param>
    /// <param name="newRot">Dışarıya verilecek YENİ yönelim (Rotasyon)</param>
    public void UpdateState(Vector3 pos, float speed, float targetSteeringAngle, float inertiaFactor, float dt, out Vector3 newPos, out Quaternion newRot)
    {
        // 1. DİREKSİYON DİNAMİĞİ
        // Direksiyon anında hedefe varamaz, fiziksel bir hızla ve kütlenin ataletiyle (inertiaFactor) yavaşça döner.
        float step = MaxSteeringSpeed * inertiaFactor * dt;
        CurrentSteeringAngle = Mathf.MoveTowards(CurrentSteeringAngle, targetSteeringAngle, step);

        // 2. KİNEMATİK MATEMATİK
        // Bisiklet modeline göre aracın dönme hızı (Açısal hız / Yaw Rate): w = (v / L) * tan(delta)
        // delta: Radyan cinsinden direksiyon açısı
        float deltaRad = CurrentSteeringAngle * Mathf.Deg2Rad;
        
        // HATA DÜZELTME (Drift Önleyici): Mert'in kodlarındaki gibi ağır araçların virajı daha yavaş dönmesini sağlamak için 
        // dönüş hızını (Yaw Rate) doğrudan atalet faktörü (inertiaFactor) ile çarpıyoruz. 
        // Aksi takdirde araç 1 saniyede 130 derece dönüp drift atıyormuş gibi görünür.
        float yawRateRad = (speed / WheelBase) * Mathf.Tan(deltaRad) * inertiaFactor;

        // Mevcut açıya (Heading) dönüş hızını ekleyerek yeni açıyı bul
        _headingAngle += yawRateRad * Mathf.Rad2Deg * dt;

        // Yeni açıyı kullanarak aracın baktığı yön vektörünü hesapla (Trigonometri)
        float headingRad = _headingAngle * Mathf.Deg2Rad;
        Vector3 forwardDir = new Vector3(Mathf.Sin(headingRad), 0f, Mathf.Cos(headingRad));

        // 3. YENİ KONUM HESABI
        // Yeni Konum = Eski Konum + (Yön * Hız * Zaman)
        newPos = pos + forwardDir * speed * dt;
        
        // Yeni Yönelimi Unity Quaternion'a çevir
        newRot = Quaternion.Euler(0f, _headingAngle, 0f);
    }
}
