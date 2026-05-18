using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(DataLogger))]
public class CarLaneTracker : MonoBehaviour
{
    [Header("Referans Yol (Spline)")]
    public SplineContainer targetSpline;

    [Header("Araç Ayarları (Kinematic Bicycle Model)")]
    public float forwardSpeed = 14f;
    public float maxSteerDeg = 35f;
    public float steeringSpeed = 180f; // derece/saniye (yumuşatma)
    public float wheelBase = 2.5f;

    [Header("Hız Kontrol Sistemi")]
    [Tooltip("Hedef hız (m/s) — UI slider'ından ayarlanır")]
    public float targetSpeed = 14f;
    [Tooltip("Viraj eğriliğine göre otomatik fren")]
    public bool autoBrakeOnCurves = true;
    [Tooltip("Minimum hız (virajlarda düşürülecek en alt değer)")]
    public float minCurveSpeed = 5f;
    [Tooltip("Hız değişim hızı (ivme/fren)")]
    public float speedChangeRate = 8f;
    [Tooltip("Eğrilik hassasiyeti — düşük değer: daha erken frenleme")]
    public float curvatureSensitivity = 0.04f;

    [Header("PID Kontrolcüsü")]
    public PIDController pid;

    // UI / DataLogger için dışarıya açık değerler
    [HideInInspector] public float currentError = 0f;
    [HideInInspector] public float currentControlSignal = 0f;
    [HideInInspector] public Vector3 currentRefPoint = Vector3.zero;
    [HideInInspector] public float currentRefLateral = 0f;  // r(t)
    [HideInInspector] public float currentCarLateral = 0f;  // y(t)
    [HideInInspector] public float currentCurvature = 0f;   // yolun eğriliği
    [HideInInspector] public float currentActualSpeed = 0f;  // gerçek hız

    private float currentSteerAngle = 0f;
    private DataLogger logger;
    private TrailRenderer trail;
    private float splineT = 0f;

    void Awake()
    {
        logger = GetComponent<DataLogger>();
        if (pid == null) pid = new PIDController();

        // Grafikte hatayı net görebilmek için zayıflatılmış PID değerleri
        pid.Kp = 1.5f;
        pid.Ki = 0.0f;
        pid.Kd = 0.5f;
        pid.maxIntegral = 10f;

        // WheelCollider uyarılarını gider:
        // WheelCollider'lar Rigidbody gerektirir ama biz kinematik model
        // kullandığımız için onlara ihtiyacımız yok. Tamamen kaldır.
        foreach (var wc in GetComponentsInChildren<WheelCollider>(true))
        {
            if (wc != null) DestroyImmediate(wc);
        }

        // Başlangıçta hedef hız = forwardSpeed
        targetSpeed = forwardSpeed;
    }

    void Start()
    {
        // Trail renderer
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.startWidth = 0.4f;
        trail.endWidth = 0f;
        trail.time = 10f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.3f, 0f, 0.9f);
        trail.endColor   = new Color(1f, 0.3f, 0f, 0f);

        // Spline'ı otomatik bul
        if (targetSpline == null)
        {
            var sObj = GameObject.Find("Spline");
            if (sObj != null) targetSpline = sObj.GetComponent<SplineContainer>();
        }

        // ===== ARABAYI YOLUN BAŞLANGICINA SNAP'LE =====
        if (targetSpline != null)
        {
            // Spline'ın 0. noktasını ve tanjantını DÜNYA KOORDİNATLARINDA hesapla.
            // TransformPoint scale'i doğru hesaplar.
            Vector3 startPos = targetSpline.transform.TransformPoint(
                (Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, 0f));

            // Tanjant için numerik (sayısal) yöntem: iki çok yakın noktanın farkını al.
            // Bu yöntem Spline objesinin scale'i ne olursa olsun HER ZAMAN doğru çalışır.
            Vector3 p0 = targetSpline.transform.TransformPoint(
                (Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, 0f));
            Vector3 p1 = targetSpline.transform.TransformPoint(
                (Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, 0.001f));
            Vector3 startFwd = (p1 - p0).normalized;

            if (startFwd.sqrMagnitude < 0.001f) startFwd = Vector3.forward;

            transform.position = startPos + Vector3.up * 0.5f;
            transform.rotation = Quaternion.LookRotation(startFwd, Vector3.up);

            Debug.Log($"<color=lime>[CarLaneTracker] Araç yola snap'lendi → {transform.position}</color>");
        }
        else
        {
            Debug.LogError("[CarLaneTracker] Spline bulunamadı!");
        }
    }

    /// <summary>
    /// Yolun belirli bir t noktasındaki eğriliğini (curvature) hesaplar.
    /// Eğrilik = |dT/ds| ≈ iki yakın tanjant vektörünün açısal farkı / mesafe
    /// Yüksek eğrilik = keskin viraj
    /// </summary>
    float CalculateCurvature(float t)
    {
        float dt = 0.005f;
        float t0 = Mathf.Clamp01(t - dt);
        float t1 = t;
        float t2 = Mathf.Clamp01(t + dt);

        // Üç noktayı dünya koordinatlarında hesapla
        Vector3 p0 = targetSpline.transform.TransformPoint(
            (Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, t0));
        Vector3 p1 = targetSpline.transform.TransformPoint(
            (Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, t1));
        Vector3 p2 = targetSpline.transform.TransformPoint(
            (Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, t2));

        // İki tanjant vektörü
        Vector3 tan1 = (p1 - p0).normalized;
        Vector3 tan2 = (p2 - p1).normalized;

        if (tan1.sqrMagnitude < 0.0001f || tan2.sqrMagnitude < 0.0001f) return 0f;

        // Eğrilik ≈ açısal değişim / ark uzunluğu
        float angle = Vector3.Angle(tan1, tan2) * Mathf.Deg2Rad;
        float dist = ((p1 - p0).magnitude + (p2 - p1).magnitude) * 0.5f;
        if (dist < 0.001f) return 0f;

        return angle / dist;
    }

    /// <summary>
    /// Eğrilik tabanlı otomatik hız kontrolü.
    /// Yüksek eğrilikli (keskin viraj) bölgelerde hızı düşürür.
    /// </summary>
    float CalculateSpeedForCurvature(float curvature)
    {
        if (!autoBrakeOnCurves) return targetSpeed;

        // Eğrilik ne kadar yüksekse, hız o kadar düşük
        // factor: 0 (keskin viraj) → 1 (düz yol)
        float factor = 1f - Mathf.Clamp01(curvature / curvatureSensitivity);
        float desiredSpeed = Mathf.Lerp(minCurveSpeed, targetSpeed, factor);

        return desiredSpeed;
    }

    void FixedUpdate()
    {
        if (targetSpline == null) return;

        // === 1. EN YAKIN NOKTAYI BUL ===
        // Araç pozisyonunu spline'ın lokal uzayına çevir
        float3 carPosLocal = math.transform(
            targetSpline.transform.worldToLocalMatrix,
            new float3(transform.position.x, transform.position.y, transform.position.z));

        float3 nearestLocal;
        SplineUtility.GetNearestPoint(targetSpline.Spline, carPosLocal, out nearestLocal, out splineT);

        // En yakın noktayı dünya koordinatlarına çevir
        Vector3 nearestWorld = targetSpline.transform.TransformPoint(nearestLocal);
        currentRefPoint = nearestWorld;

        // === 2. YOLUN YÖN VEKTÖRLERİ (NUMERİK TANJANT) ===
        float tA = splineT;
        float tB = splineT + 0.001f;
        if (tB > 1f) 
        {
            tB = splineT;
            tA = splineT - 0.001f;
        }

        Vector3 pA = targetSpline.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, tA));
        Vector3 pB = targetSpline.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, tB));

        Vector3 pathForward = (pB - pA).normalized;
        if (pathForward.sqrMagnitude < 0.001f)
            pathForward = transform.forward; // Güvenlik: çok uç durumda aracın yönünü kullan

        // pathRight = yolun sağ tarafı (düz bir yüzeyde)
        Vector3 pathRight = Vector3.Cross(Vector3.up, pathForward).normalized;

        // === 3. YANAL SAPMA HESABI ===
        // y(t): aracın yolun sağına olan mesafesi (pozitif = sağda, negatif = solda)
        Vector3 offset = transform.position - nearestWorld;
        float lateralDeviation = Vector3.Dot(offset, pathRight);

        // r(t) = 0 (şerit merkezi referansı)
        // e(t) = r(t) - y(t) → araç sağdaysa negatif hata → sola direksiyon kır
        currentRefLateral = 0f;
        currentCarLateral = lateralDeviation;
        float error = currentRefLateral - currentCarLateral;
        currentError = error;

        // === 4. PID KONTROL SİNYALİ ===
        float u = pid.CalculateControlSignal(error, Time.fixedDeltaTime);
        currentControlSignal = u;

        // === 5. DİREKSİYON YUMUŞATMA ===
        float targetSteer = Mathf.Clamp(u, -maxSteerDeg, maxSteerDeg);
        currentSteerAngle = Mathf.MoveTowards(
            currentSteerAngle, targetSteer, steeringSpeed * Time.fixedDeltaTime);

        // === 6. HIZ KONTROL SİSTEMİ ===
        // Eğrilik hesapla (ilerideki yol parçası)
        float lookAheadT = Mathf.Clamp01(splineT + 0.02f); // Biraz ilerisine bak
        currentCurvature = CalculateCurvature(lookAheadT);

        // Eğriliğe göre hedef hızı belirle
        float desiredSpeed = CalculateSpeedForCurvature(currentCurvature);

        // Hızı yumuşak şekilde hedefe doğru değiştir (ivme/fren)
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredSpeed, speedChangeRate * Time.fixedDeltaTime);
        currentActualSpeed = forwardSpeed;

        // === 7. KİNEMATİK BİSİKLET MODELİ ===
        // yaw_rate = (v / L) * tan(δ)
        float steerRad  = currentSteerAngle * Mathf.Deg2Rad;
        float yawRateDeg = (forwardSpeed / wheelBase) * Mathf.Tan(steerRad) * Mathf.Rad2Deg;

        transform.Rotate(Vector3.up, yawRateDeg * Time.fixedDeltaTime, Space.World);
        transform.position += transform.forward * forwardSpeed * Time.fixedDeltaTime;

        // Yere yapıştır
        transform.position = new Vector3(
            transform.position.x,
            Mathf.Max(transform.position.y, 0.5f),
            transform.position.z);

        // === 8. VERİ KAYDET ===
        if (logger != null)
            logger.LogData(error, u, transform.position, nearestWorld, pid.mode.ToString(), forwardSpeed, targetSpeed);

        // Sahne görünümü debug çizgileri
        Debug.DrawLine(transform.position, nearestWorld, Color.green);
        Debug.DrawRay(nearestWorld, pathForward * 3f, Color.blue);
    }
}
