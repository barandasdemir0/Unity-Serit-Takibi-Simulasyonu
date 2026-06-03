using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Şerit Takip Sistemi Ana Yöneticisi (Orkestratör).
/// BAĞLANTI: Bu sınıf, matematiksel işlemleri yapmaz. Alt modülleri (LateralErrorCalculator.cs, PIDController.cs, KinematicBicycleModel.cs) birbirine bağlar.
/// </summary>
[RequireComponent(typeof(DataLogger))]
public class CarLaneTracker : MonoBehaviour
{
    // BAĞLANTI: Unity'nin Spline (Yol) sistemi. Aracın takip edeceği referans rotadır.
    public SplineContainer targetSpline;
    
    // Kullanıcı tarafından UI üzerinden değiştirilebilen araç ayarları
    [Range(1f, 30f)] public float DesiredSpeed = 10f;
    [Range(500f, 5000f)] public float VehicleMass = 1500f;
    [Range(-5f, 5f)] public float InitialLateralError = 2.0f;
    
    // Araç fizik kısıtlamaları (KinematicBicycleModel.cs'e aktarılır)
    public float wheelBase = 2.5f, maxSteeringAngle = 35f, maxSteeringSpeed = 150f, wheelMultiplier = 360f;
    
    // BAĞLANTI: Görsel tekerlek modelleri (WheelAnimator.cs tarafından döndürülür)
    public Transform frontLeftTire, frontRightTire, backLeftTire, backRightTire;

    // --- ALT SİSTEM (MODÜL) TANIMLAMALARI ---
    // BAĞLANTI: Kontrolcü matematiği -> PIDController.cs
    public PIDController pid = new PIDController();
    
    // BAĞLANTI: Hata hesaplama -> LateralErrorCalculator.cs
    private LateralErrorCalculator _errorCalc = new LateralErrorCalculator();
    
    // BAĞLANTI: Araç fiziği -> KinematicBicycleModel.cs
    private KinematicBicycleModel _bicycleModel = new KinematicBicycleModel();
    
    // BAĞLANTI: Veri kayıt yapısı -> CarDataRecorder.cs
    public CarDataRecorder Recorder { get; private set; } = new CarDataRecorder();
    
    // BAĞLANTI: CSV dışa aktarımı -> DataLogger.cs
    private DataLogger _logger;

    // Dışarıdan (UI ve Grafikler tarafından) okunabilen salt-okunur (readonly) veriler
    public float CurrentLateralError => _errorCalc.LateralError;
    public float CurrentControlOutput { get; private set; }
    public float CurrentSteeringAngle => _bicycleModel.CurrentSteeringAngle;
    public float CurrentSpeed { get; private set; }
    public bool IsRunning { get; set; } = true;

    void Awake()
    {
        // DataLogger bileşenini bu objede bul ve referansını al
        _logger = GetComponent<DataLogger>();
        
        // Araç modelinin kendi içinde gelen varsayılan WheelCollider'larını sil (çünkü biz Kinematik Model kullanıyoruz)
        foreach (var wc in GetComponentsInChildren<WheelCollider>(true)) DestroyImmediate(wc);
    }

    void Start()
    {
        // Kütleyi Rigidbody'ye yansıt (Fizik motoru tepkileri için)
        if (TryGetComponent<Rigidbody>(out var rb)) rb.mass = VehicleMass;
        
        // Eğer hedef yol atanmamışsa sahneden otomatik bul
        if (targetSpline == null) targetSpline = GameObject.Find("Spline")?.GetComponent<SplineContainer>();

        // Görsel tekerlekleri hiyerarşiden bul
        frontLeftTire = transform.Find("Sport Car_39 FL Tire");
        frontRightTire = transform.Find("Sport Car_39 FR Tire");
        backLeftTire = transform.Find("Sport Car_39 BL Tire");
        backRightTire = transform.Find("Sport Car_39 BR Tire");

        // BAĞLANTI: Arabanın arkasından çıkan izi oluşturur -> TrailEffectManager.cs
        TrailEffectManager.AttachTrail(gameObject);
        
        // Simülasyonu ilk durumuna getir
        ResetSimulation();
    }

    void FixedUpdate()
    {
        // Eğer sistem duraklatılmışsa veya yol yoksa hiçbir şey yapma
        if (!IsRunning || targetSpline == null) return;
        
        // Fizik motorunun sabit zaman adımı (Genellikle 0.02 saniye)
        float dt = Time.fixedDeltaTime;

        // 1. HIZLANMA MANTIĞI: Aracın hızını istenen hıza (DesiredSpeed) doğru yavaşça artır
        CurrentSpeed = Mathf.Max(0f, CurrentSpeed + (DesiredSpeed - CurrentSpeed) * 2f * dt);

        // 2. HATA HESABI (MODÜL 1)
        // BAĞLANTI: LateralErrorCalculator.cs sınıfına aracın pozisyonunu gönderip hatayı alırız.
        float latError = _errorCalc.Calculate(transform.position, targetSpline, transform.forward);
        
        // 3. PID KONTROL (MODÜL 2)
        // BAĞLANTI: Hatayı PIDController.cs sınıfına gönderip, vermemiz gereken direksiyon açısı oranını (u(t)) alırız.
        // Eksi işareti eklendi: Araç sağdaysa (+ hata) sola (-) dönmeli.
        CurrentControlOutput = pid.Compute(-latError, dt);

        // 4. BİSİKLET FİZİK MODELİ (MODÜL 3)
        // Kullanıcı arayüzünden gelen parametreleri fizik modeline aktar
        _bicycleModel.WheelBase = wheelBase; 
        _bicycleModel.MaxSteeringAngle = maxSteeringAngle; 
        _bicycleModel.MaxSteeringSpeed = maxSteeringSpeed; // UI'dan gelen hızı modele aktar
        
        // Ağır araçların direksiyon tepkisi yavaş olur (Atalet / Inertia hesabı)
        float inertia = Mathf.Lerp(1.0f, 0.1f, (VehicleMass - 500f) / 4500f);
        
        // BAĞLANTI: KinematicBicycleModel.cs sınıfına mevcut durumu gönder, o bize aracın "yeni" pozisyonunu versin.
        // PID'nin hesapladığı hedef açıyı (CurrentControlOutput * maxSteeringAngle) doğrudan fizik modeline yolluyoruz.
        _bicycleModel.UpdateState(transform.position, CurrentSpeed, CurrentControlOutput * maxSteeringAngle, inertia, dt, out Vector3 newPos, out Quaternion newRot);
        
        // Aracın Zemin yüksekliğini (y) sabitle ve hesaplanan yeni konuma yerleştir
        newPos.y = 0.5f; 
        transform.SetPositionAndRotation(newPos, newRot);

        // 6. GÖRSELLİK VE KAYIT İŞLEMLERİ (MODÜL 4)
        // BAĞLANTI: Tekerleklerin dönüşünü canlandırır -> WheelAnimator.cs
        WheelAnimator.Animate(frontLeftTire, frontRightTire, backLeftTire, backRightTire, CurrentSpeed, CurrentSteeringAngle, wheelMultiplier, dt);
        
        // BAĞLANTI: Grafikler ve CSV için bu karenin (frame) verilerini RAM'e kaydet -> CarDataRecorder.cs
        Recorder.RecordFrame(CurrentLateralError, CurrentControlOutput, pid, CurrentSteeringAngle, transform.position, _errorCalc.NearestPointOnSpline);
        
        // BAĞLANTI: Belirli aralıklarla CSV için veriyi tut -> DataLogger.cs
        if (_logger != null) _logger.LogData(this);
    }

    /// <summary>
    /// Simülasyonu başa sarar ve tüm değişkenleri sıfırlar.
    /// </summary>
    public void ResetSimulation()
    {
        // BAĞLANTI: Aracı başlangıç çizgisine yerleştirir -> VehiclePositionResetter.cs
        VehiclePositionResetter.ResetPosition(transform, targetSpline, InitialLateralError);
        
        // Değişkenleri ve fizik modellerini sıfırla
        CurrentSpeed = 0f;
        pid.Reset();
        _bicycleModel.Reset(transform.eulerAngles.y);
        Recorder.StartRecording();
    }
}
