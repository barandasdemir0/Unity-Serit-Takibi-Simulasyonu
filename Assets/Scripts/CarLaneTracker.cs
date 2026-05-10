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
    public float forwardSpeed = 8f;
    public float maxSteerDeg = 35f;
    public float steeringSpeed = 180f; // derece/saniye (yumuşatma)
    public float wheelBase = 2.5f;

    [Header("PID Kontrolcüsü")]
    public PIDController pid;

    // UI / DataLogger için dışarıya açık değerler
    [HideInInspector] public float currentError = 0f;
    [HideInInspector] public float currentControlSignal = 0f;
    [HideInInspector] public Vector3 currentRefPoint = Vector3.zero;
    [HideInInspector] public float currentRefLateral = 0f;  // r(t)
    [HideInInspector] public float currentCarLateral = 0f;  // y(t)

    private float currentSteerAngle = 0f;
    private DataLogger logger;
    private TrailRenderer trail;
    private float splineT = 0f;

    void Awake()
    {
        logger = GetComponent<DataLogger>();
        if (pid == null) pid = new PIDController();

        // Güvenli varsayılan PID değerleri
        pid.Kp = 5.0f;
        pid.Ki = 0.0f;
        pid.Kd = 3.0f;
        pid.maxIntegral = 10f;

        // WheelCollider uyarılarını gider:
        // WheelCollider'lar Rigidbody gerektirir ama biz kinematik model
        // kullandığımız için onlara ihtiyacımız yok. Tamamen kaldır.
        foreach (var wc in GetComponentsInChildren<WheelCollider>(true))
        {
            if (wc != null) DestroyImmediate(wc);
        }
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

        // === 6. KİNEMATİK BİSİKLET MODELİ ===
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

        // === 7. VERİ KAYDET ===
        if (logger != null)
            logger.LogData(error, u, transform.position, nearestWorld, pid.mode.ToString());

        // Sahne görünümü debug çizgileri
        Debug.DrawLine(transform.position, nearestWorld, Color.green);
        Debug.DrawRay(nearestWorld, pathForward * 3f, Color.blue);
    }
}
