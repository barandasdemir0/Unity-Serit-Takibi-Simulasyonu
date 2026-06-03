using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kinematik bisiklet modeli kullanan araç kontrolcüsü.
/// PID kontrolcüsünden gelen direksiyon açılarını alarak aracı yol boyunca 
/// hareket ettirir ve yanal sapmayı en aza indirmeye çalışır.
/// 
/// Bisiklet Modeli (Bicycle Model):
///   Araç iki tekerlekli (ön + arka aks) bir model olarak basitleştirilmiştir.
///   Ön tekerleğe uygulanan direksiyon açısı (δ) bir dönüş yarıçapı oluşturur:
///     R = L / tan(δ)  (L: Dingil mesafesi)
///   Açısal hız:
///     ω = v / R = v * tan(δ) / L
///   Konum Güncellemesi:
///     x(t+dt) = x(t) + v * cos(θ) * dt
///     z(t+dt) = z(t) + v * sin(θ) * dt
///     θ(t+dt) = θ(t) + ω * dt
/// </summary>
public class VehicleController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Referanslar
    // ─────────────────────────────────────────────
    [Header("Referanslar")]
    [Tooltip("Hata hesaplaması için kullanılacak referans yol")]
    public RoadSpline roadSpline;

    // ─────────────────────────────────────────────
    // Araç Parametreleri
    // ─────────────────────────────────────────────
    [Header("Araç Parametreleri")]
    [Tooltip("Aracın m/s cinsinden ileri hızı")]
    [Range(1f, 30f)]
    public float vehicleSpeed = 7f;

    [Tooltip("Araç kütlesi (kg). Arttıkça yönelim ataleti artar — aynı PID katsayıları sistemi kontrol edemez hale gelebilir.")]
    [Range(500f, 5000f)]
    public float vehicleMass = 1500f;

    [Tooltip("Simülasyon başlangıcında aracın şerit merkezine yanal mesafesi (m). Kontrolcünün hatayı giderme performansını gözlemlemek için.")]
    [Range(-3f, 3f)]
    public float initialLateralOffset = 1.5f;

    [Tooltip("Dingil mesafesi (Ön ve arka akslar arasındaki uzaklık) metre cinsinden")]
    public float wheelbase = 2.5f;

    [Tooltip("Maksimum direksiyon açısı (Derece)")]
    [Range(10f, 60f)]
    public float maxSteeringAngle = 35f;

    [Tooltip("Maksimum direksiyon çevirme hızı (Derece/Saniye - Slew Rate)")]
    [Range(10f, 500f)]
    public float maxSteeringSpeed = 150f;

    // ─────────────────────────────────────────────
    // PID Kontrolcü (Gömülü ve Serileştirilmiş)
    // ─────────────────────────────────────────────
    [Header("PID Kontrolcü")]
    public PIDController pidController = new PIDController();

    // ─────────────────────────────────────────────
    // Tekerlek Görselleri (Animasyon için)
    // ─────────────────────────────────────────────
    [Header("Tekerlek Referansları")]
    [Tooltip("Sol Ön Tekerlek")]
    public Transform frontLeftTire;
    [Tooltip("Sağ Ön Tekerlek")]
    public Transform frontRightTire;
    [Tooltip("Sol Arka Tekerlek")]
    public Transform backLeftTire;
    [Tooltip("Sağ Arka Tekerlek")]
    public Transform backRightTire;

    [Tooltip("Görsel efekt için tekerlek dönme hızı çarpanı")]
    public float wheelRotationMultiplier = 360f;

    // ─────────────────────────────────────────────
    // Simülasyon Durumu (Salt-okunur teşhis verileri)
    // ─────────────────────────────────────────────
    [Header("Teşhis Verileri (Salt-Okunur)")]
    [SerializeField] private float _currentLateralError;
    [SerializeField] private float _currentControlOutput;
    [SerializeField] private float _currentSteeringAngle;

    /// <summary>Mevcut yanal hata e(t) = r(t) - y(t)</summary>
    public float CurrentLateralError => _currentLateralError;

    /// <summary>Mevcut PID kontrol çıkışı u(t)</summary>
    public float CurrentControlOutput => _currentControlOutput;

    /// <summary>Mevcut direksiyon açısı (derece)</summary>
    public float CurrentSteeringAngle => _currentSteeringAngle;

    /// <summary>Referans noktası r(t) — Spline çizgisi üzerindeki en yakın nokta</summary>
    public Vector3 ReferencePoint { get; private set; }

    /// <summary>En yakın noktadaki referans yolun ileri yönü</summary>
    public Vector3 ReferenceForward { get; private set; }

    // ─────────────────────────────────────────────
    // Veri Kaydı (Grafikler ve CSV için)
    // ─────────────────────────────────────────────
    public struct FrameData
    {
        public float time;           // Simülasyon zamanı (s)
        public float lateralError;   // e(t) = r(t) - y(t) — Yanal hata (m)
        public float controlOutput;  // u(t) — PID kontrolcü çıkışı [-1, 1]
        public float vehiclePosX;    // y(t) — Aracın spline çerçevesindeki yanal konumu (m) [= lateralError, r(t)=0 referansına göre]
        public float referencePosX;  // r(t) — Referans merkez konumu (her zaman 0, spline merkezi)
        public float pTerm;          // PID P bileşeni: Kp * e(t)
        public float iTerm;          // PID I bileşeni: Ki * ∫e(t)dt
        public float dTerm;          // PID D bileşeni: Kd * de(t)/dt
        public float steeringAngle;  // δ(t) — Anlık direksiyon açısı (derece)
        public float vehicleGlobalX; // Araç Global X koordinatı (dünya uzayı, m)
        public float vehicleGlobalZ; // Araç Global Z koordinatı (dünya uzayı, m)
        public float targetGlobalX;  // Referans noktası Global X koordinatı (m)
        public float targetGlobalZ;  // Referans noktası Global Z koordinatı (m)
    }

    /// <summary>Gerçek zamanlı grafik görselleştirmesi için kaydedilen kare verileri</summary>
    public List<FrameData> RecordedData { get; private set; } = new List<FrameData>();

    [Header("Veri Kaydı")]
    [Tooltip("Kaydedilecek maksimum veri noktası sayısı")]
    public int maxRecordedFrames = 2000;

    // ─────────────────────────────────────────────
    // İç Durum Değişkenleri
    // ─────────────────────────────────────────────
    private float _heading;      // Mevcut yönelim açısı (θ) radyan cinsinden
    private float _simTime;      // Birikmiş simülasyon zamanı
    private float _startTime;    // Simülasyonun gerçek başlama zamanı
    private bool _isRunning = true;

    /// <summary>Simülasyonun şu an çalışıp çalışmadığını belirtir</summary>
    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value;
    }

    private void Start()
    {
        // Yönelim açısını mevcut dönüşten başlat
        _heading = transform.eulerAngles.y * Mathf.Deg2Rad;
        _simTime = 0f;
        _startTime = Time.time;

        // Başlangıç yanal ofset uygula — araç şerit merkezinden sapık başlar
        // Böylece PID kontrolcünün hatayı giderme süreci gözlemlenebilir
        ApplyInitialLateralOffset();

        // Tekerlek referansları atanmamışsa otomatik olarak bul
        if (frontLeftTire == null)
            frontLeftTire = transform.Find("Sport Car_39 FL Tire");
        if (frontRightTire == null)
            frontRightTire = transform.Find("Sport Car_39 FR Tire");
        if (backLeftTire == null)
            backLeftTire = transform.Find("Sport Car_39 BL Tire");
        if (backRightTire == null)
            backRightTire = transform.Find("Sport Car_39 BR Tire");
    }

    /// <summary>
    /// Başlangıç yanal ofset: aracı yol merkezinden belirli mesafede konumlandırır.
    /// Spline'ın başlangıç yönüne dik olarak kaydırma yapılır.
    /// </summary>
    private void ApplyInitialLateralOffset()
    {
        if (roadSpline == null || Mathf.Approximately(initialLateralOffset, 0f)) return;

        // Spline'ın başlangıç teğetini al (globalT = 0 → spline'ın ilk noktası)
        Vector3 splineForward = roadSpline.EvaluateTangentGlobal(0f).normalized;
        // Sağ vektör = forward × up
        Vector3 splineRight = Vector3.Cross(splineForward, Vector3.up).normalized;
        // Aracı yanal yönde kaydır
        transform.position += splineRight * initialLateralOffset;
    }

    private void FixedUpdate()
    {
        if (!_isRunning || roadSpline == null) return;

        float dt = Time.fixedDeltaTime;
        _simTime += dt;

        // ── Adım 1: Yanal hata e(t)'nin hesaplanması ──────────
        // Referans yol üzerindeki en yakın noktayı bul
        Vector3 nearestPoint;
        float lateralError;
        Vector3 forwardDir;

        roadSpline.GetNearestPoint(transform.position, out nearestPoint, 
                                    out lateralError, out forwardDir);

        ReferencePoint = nearestPoint;
        ReferenceForward = forwardDir;
        _currentLateralError = lateralError;

        // ── Adım 2: PID kontrol çıkışı u(t)'nin hesaplanması ──────
        // e(t) = r(t) - y(t)
        // Merkez çizgisi takibi için: r(t) = 0 (sıfır yanal sapma istiyoruz)
        // Bu yüzden e(t) = 0 - lateralError = -lateralError
        // Ancak lateralError halihazırda işaretli olduğu için (+ sol, - sağ)
        // PID aracı merkeze yönlendirmeli, bu yüzden hatayı ters çeviriyoruz
        float error = -lateralError;
        float controlOutput = pidController.Compute(error, dt);

        _currentControlOutput = controlOutput;

        // ── Adım 3: Kontrol çıkışının direksiyon açısına dönüştürülmesi ──
        // u(t) değeri direksiyon açısına (δ) eşlenir
        // Pozitif u(t) → sola dön, Negatif → sağa dön
        float targetSteeringAngle = controlOutput * maxSteeringAngle;
        targetSteeringAngle = Mathf.Clamp(targetSteeringAngle, -maxSteeringAngle, maxSteeringAngle);
        
        // ── Adım 3.5: Fiziksel direksiyon hızı sınırının (Slew Rate) uygulanması ──
        // Bu, PID kazançları çok yüksek olduğunda aracın gerçekçi bir şekilde dengesizleşmesini sağlar!
        _currentSteeringAngle = Mathf.MoveTowards(_currentSteeringAngle, targetSteeringAngle, maxSteeringSpeed * dt);

        float steeringRad = _currentSteeringAngle * Mathf.Deg2Rad;

        // ── Adım 4: Kinematik bisiklet modeli güncellemesi ──────
        // Kütle ataleti: Ağır araç yönelim değişimini daha yavaş yapar.
        // Atalet faktörü → [500 kg → 1.0x, 5000 kg → 0.1x] (doğrusal interpolasyon)
        float inertiaFactor = Mathf.Lerp(1.0f, 0.1f, (vehicleMass - 500f) / 4500f);

        // Açısal hız: ω = v * tan(δ) / L  × inertia_factor
        float angularVelocity = (vehicleSpeed * Mathf.Tan(steeringRad)) / wheelbase * inertiaFactor;

        // Yönelim (heading) güncellemesi: θ(t+dt) = θ(t) + ω * dt
        _heading += angularVelocity * dt;

        // Konum güncellemesi:
        // x(t+dt) = x(t) + v * sin(θ) * dt  (Unity X = yanal eksen)
        // z(t+dt) = z(t) + v * cos(θ) * dt  (Unity Z = ileri eksen)
        Vector3 velocity = new Vector3(
            vehicleSpeed * Mathf.Sin(_heading),
            0f,
            vehicleSpeed * Mathf.Cos(_heading)
        );

        Vector3 newPosition = transform.position + velocity * dt;
        newPosition.y = 0.5f; // Aracı yer hizasında tut
        transform.position = newPosition;

        // Rotasyonu yönelim açısına göre güncelle
        transform.rotation = Quaternion.Euler(0f, _heading * Mathf.Rad2Deg, 0f);

        // ── Adım 5: Tekerleklerin anime edilmesi ──────────────────────
        AnimateWheels(_currentSteeringAngle, dt);

        // ── Adım 6: Grafikler için verilerin kaydedilmesi ──────────────
        RecordFrameData();
    }

    /// <summary>
    /// Tekerleklerin dönüşünü (yuvarlanma) ve ön tekerleklerin yönlenmesini canlandırır.
    /// </summary>
    private void AnimateWheels(float steeringAngle, float dt)
    {
        float rollAngle = vehicleSpeed * wheelRotationMultiplier * dt;

        // Tüm tekerlekleri yuvarla
        if (backLeftTire != null)
            backLeftTire.Rotate(Vector3.right, rollAngle, Space.Self);
        if (backRightTire != null)
            backRightTire.Rotate(Vector3.right, rollAngle, Space.Self);

        // Ön tekerlekler: yuvarla + döndür
        if (frontLeftTire != null)
        {
            frontLeftTire.localRotation = Quaternion.Euler(
                frontLeftTire.localEulerAngles.x + rollAngle,
                steeringAngle,
                0f
            );
        }
        if (frontRightTire != null)
        {
            frontRightTire.localRotation = Quaternion.Euler(
                frontRightTire.localEulerAngles.x + rollAngle,
                steeringAngle,
                0f
            );
        }
    }

    /// <summary>
    /// Gerçek zamanlı grafik gösterimi için mevcut karenin verilerini kaydeder.
    /// Sınırsız bellek büyümesini önlemek için ring buffer (dairesel tampon) kullanır.
    /// </summary>
    private void RecordFrameData()
    {
        FrameData data = new FrameData
        {
            time           = Time.time - _startTime,
            lateralError   = _currentLateralError,
            controlOutput  = _currentControlOutput,
            // y(t): Aracın spline merkez hattına göre yanal konumu.
            // r(t) = 0 olarak alındığından, y(t) = lateralError'a eşittir.
            // Pozitif → araç yolun sağında, Negatif → solunda.
            vehiclePosX    = _currentLateralError,
            referencePosX  = 0f,
            pTerm          = pidController.LastProportional,
            iTerm          = pidController.LastIntegral,
            dTerm          = pidController.LastDerivative,
            steeringAngle  = _currentSteeringAngle,
            vehicleGlobalX = transform.position.x,
            vehicleGlobalZ = transform.position.z,
            targetGlobalX  = ReferencePoint.x,
            targetGlobalZ  = ReferencePoint.z
        };

        RecordedData.Add(data);

        // Ring buffer: Limit aşılırsa en eski veriyi sil
        if (RecordedData.Count > maxRecordedFrames)
        {
            RecordedData.RemoveAt(0);
        }
    }

    /// <summary>
    /// Simülasyonu sıfırlar: araç pozisyonu, PID durumu ve kaydedilen veriler silinir.
    /// </summary>
    public void ResetSimulation()
    {
        // Aracı başlangıç konumuna al
        transform.position = new Vector3(0f, 0.5f, 0f);
        transform.rotation = Quaternion.identity;
        _heading = 0f;
        _simTime = 0f;
        _startTime = Time.time;
        _currentSteeringAngle = 0f;

        // Başlangıç yanal ofsetini yeniden uygula
        ApplyInitialLateralOffset();

        // PID kontrolcüsünü sıfırla
        pidController.Reset();

        // Kayıtlı verileri temizle
        RecordedData.Clear();
    }
}
