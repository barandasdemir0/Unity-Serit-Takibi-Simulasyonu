using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Catmull-Rom spline tabanlı yol merkez çizgisi tanımlaması.
/// PID kontrolcüsü için r(t) referans yolunu sağlar.
/// 
/// Spline (eğri), tüm kontrol noktalarından yumuşak bir şekilde geçer
/// ve C1 sürekliliğine sahip kesintisiz bir eğri oluşturur.
/// Bu şunlar için kullanılır:
///   1. Yol geometrisini (merkez çizgisini) tanımlamak
///   2. Yanal hatayı hesaplamak: e(t) = r(t) - y(t)
///   3. Araç için referans yönünü (ileriye doğru teğet) sağlamak
/// </summary>
[ExecuteAlways]
public class RoadSpline : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Spline Ayarları
    // ─────────────────────────────────────────────
    [Header("Spline Ayarları")]
    [Tooltip("Yolun rotasını belirleyen kontrol noktaları. En az 4 nokta gereklidir.")]
    public List<Vector3> controlPoints = new List<Vector3>();

    [Tooltip("Spline eğrisinin kapalı bir döngü (pist) oluşturup oluşturmadığı")]
    public bool isClosedLoop = true;

    [Tooltip("Mesafe hesaplamaları için her bir segmentteki (parçadaki) interpolasyon örneği sayısı")]
    [Range(10, 100)]
    public int samplesPerSegment = 50;

    // ─────────────────────────────────────────────
    // Yay uzunluğu parametrelendirmesi için önbelleğe alınmış (cached) arama tablosu
    // ─────────────────────────────────────────────
    private List<Vector3> _cachedPoints;
    private List<float> _cachedDistances;
    private float _totalLength;

    /// <summary>
    /// Spline eğrisinin dünya birimleri cinsinden toplam yay uzunluğu.
    /// </summary>
    public float TotalLength => _totalLength;

    /// <summary>
    /// Spline eğrisindeki geçerli segment (parça) sayısı.
    /// </summary>
    public int SegmentCount => isClosedLoop ? controlPoints.Count : controlPoints.Count - 1;

    private void Awake()
    {
        GenerateDefaultTrack();
        RebuildCache();
    }

    /// <summary>
    /// Değişen keskinliklerde 8+ viraj içeren varsayılan bir yarış pisti oluşturur.
    /// Bu, PID testi için uygun kapalı döngü bir devre yaratır.
    /// Pist şunları içerir: yumuşak virajlar, keskin U dönüşleri (hairpin), S virajlar ve düzlükler.
    /// </summary>
    [ContextMenu("Regenerate Default Track")]
    public void GenerateDefaultTrack()
    {
        controlPoints.Clear();

        // Zikzakları, uzun düzlükleri, yumuşak virajları ve keskin köşeleri birleştiren devasa bir rota.
        controlPoints.AddRange(new Vector3[]
        {
            // 1. Kuzeye Doğru Dokuma (Zikzaklar)
            new Vector3(  0f, 0f,   0f),
            new Vector3( 40f, 0f,  60f),
            new Vector3(-30f, 0f, 120f),
            new Vector3( 50f, 0f, 180f),
            new Vector3(-40f, 0f, 240f),
            new Vector3( 40f, 0f, 300f),
            new Vector3(-20f, 0f, 360f),
            
            // 2. Kuzey-Batı Köşesi (Sağa keskin dönüş)
            new Vector3(  0f, 0f, 420f),
            new Vector3( 60f, 0f, 460f),
            new Vector3(120f, 0f, 440f),
            
            // 3. Kuzey Tarafı (Uzun Düzlük)
            new Vector3(200f, 0f, 440f),
            new Vector3(300f, 0f, 440f),
            new Vector3(400f, 0f, 440f),
            new Vector3(500f, 0f, 440f),
            new Vector3(600f, 0f, 440f),
            
            // 4. Kuzey-Doğu Köşesi (Sağa yumuşak viraj)
            new Vector3(680f, 0f, 420f),
            new Vector3(740f, 0f, 360f),
            new Vector3(760f, 0f, 280f),
            
            // 5. Doğu Tarafı (Yumuşak Virajlar / S-Virajı)
            new Vector3(720f, 0f, 200f),
            new Vector3(780f, 0f, 120f),
            new Vector3(720f, 0f,  40f),
            
            // 6. Güney-Doğu Köşesi (Keskin dönüş)
            new Vector3(740f, 0f, -40f),
            new Vector3(700f, 0f, -80f),
            new Vector3(620f, 0f, -80f),
            
            // 7. Güney Tarafı (Hafif tümsekli düzlük)
            new Vector3(520f, 0f, -80f),
            new Vector3(420f, 0f, -80f),
            new Vector3(320f, 0f, -30f),
            new Vector3(220f, 0f, -80f),
            new Vector3(120f, 0f, -80f),
            
            // 8. Güney-Batı Köşesi (Başlangıca doğru kuzeye yumuşak dönüş)
            new Vector3( 40f, 0f, -60f)
        });
    }

    /// <summary>
    /// Hızlı mesafe sorgulamaları için dahili arama tablolarını (cache) yeniden oluşturur.
    /// Kontrol noktaları değiştirildikten sonra çağrılması zorunludur.
    /// </summary>
    public void RebuildCache()
    {
        if (controlPoints.Count < 4) return;

        _cachedPoints = new List<Vector3>();
        _cachedDistances = new List<float>();
        _totalLength = 0f;

        int totalSamples = SegmentCount * samplesPerSegment;

        Vector3 prevPoint = EvaluateCatmullRom(0, 0f);
        _cachedPoints.Add(prevPoint);
        _cachedDistances.Add(0f);

        for (int i = 1; i <= totalSamples; i++)
        {
            float t = (float)i / totalSamples;
            int segIndex;
            float segT;
            GlobalTToSegment(t, out segIndex, out segT);

            Vector3 point = EvaluateCatmullRom(segIndex, segT);
            float dist = Vector3.Distance(prevPoint, point);
            _totalLength += dist;

            _cachedPoints.Add(point);
            _cachedDistances.Add(_totalLength);
            prevPoint = point;
        }
    }

    /// <summary>
    /// Belirli bir segment ve yerel parametre için Catmull-Rom spline üzerindeki bir noktayı hesaplar.
    /// 
    /// Catmull-Rom formülü:
    ///   q(t) = 0.5 * ((2*P1) + (-P0 + P2)*t + (2*P0 - 5*P1 + 4*P2 - P3)*t² + (-P0 + 3*P1 - 3*P2 + P3)*t³)
    /// </summary>
    /// <param name="segmentIndex">Spline segmentinin indeksi</param>
    /// <param name="t">Segment içindeki yerel parametre [0, 1] aralığında</param>
    /// <returns>Spline üzerindeki dünya koordinatı (World Position)</returns>
    public Vector3 EvaluateCatmullRom(int segmentIndex, float t)
    {
        int count = controlPoints.Count;

        // Bu segment için dört kontrol noktasını al
        // Kapalı döngüler (closed loop) için indeksleri modulo ile sar (wrap)
        int p0 = WrapIndex(segmentIndex - 1, count);
        int p1 = WrapIndex(segmentIndex,     count);
        int p2 = WrapIndex(segmentIndex + 1, count);
        int p3 = WrapIndex(segmentIndex + 2, count);

        Vector3 a = controlPoints[p0];
        Vector3 b = controlPoints[p1];
        Vector3 c = controlPoints[p2];
        Vector3 d = controlPoints[p3];

        // Catmull-Rom interpolasyon matrisi
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 result = 0.5f * (
            (2f * b) +
            (-a + c) * t +
            (2f * a - 5f * b + 4f * c - d) * t2 +
            (-a + 3f * b - 3f * c + d) * t3
        );

        return result;
    }

    /// <summary>
    /// Spline üzerindeki bir noktadaki teğeti (yönü) alır.
    /// Catmull-Rom formülünün türevi:
    ///   q'(t) = 0.5 * ((-P0 + P2) + (4*P0 - 10*P1 + 8*P2 - 2*P3)*t + (-3*P0 + 9*P1 - 9*P2 + 3*P3)*t²)
    /// </summary>
    public Vector3 EvaluateTangent(int segmentIndex, float t)
    {
        int count = controlPoints.Count;

        int p0 = WrapIndex(segmentIndex - 1, count);
        int p1 = WrapIndex(segmentIndex,     count);
        int p2 = WrapIndex(segmentIndex + 1, count);
        int p3 = WrapIndex(segmentIndex + 2, count);

        Vector3 a = controlPoints[p0];
        Vector3 b = controlPoints[p1];
        Vector3 c = controlPoints[p2];
        Vector3 d = controlPoints[p3];

        float t2 = t * t;

        Vector3 tangent = 0.5f * (
            (-a + c) +
            (4f * a - 10f * b + 8f * c - 2f * d) * t +
            (-3f * a + 9f * b - 9f * c + 3f * d) * t2
        );

        return tangent.normalized;
    }

    /// <summary>
    /// Spline üzerinde, verilen bir dünya pozisyonuna en yakın noktayı bulur.
    /// Spline noktasını ve işaretli (signed) yanal hatayı döndürür.
    /// 
    /// Yanal hata (Lateral Error) kuralı:
    ///   Negatif (-) = Araç merkez çizgisinin SOLUNDA
    ///   Pozitif (+) = Araç merkez çizgisinin SAĞINDA
    /// </summary>
    /// <param name="worldPos">Aracın mevcut dünya pozisyonu</param>
    /// <param name="nearestPoint">Çıkış: Spline üzerindeki en yakın nokta</param>
    /// <param name="lateralError">Çıkış: İşaretli yanal mesafe (+ sol, - sağ)</param>
    /// <param name="forwardDirection">Çıkış: En yakın noktadaki teğet yönü</param>
    public void GetNearestPoint(Vector3 worldPos, out Vector3 nearestPoint, 
                                 out float lateralError, out Vector3 forwardDirection)
    {
        if (_cachedPoints == null || _cachedPoints.Count == 0)
        {
            nearestPoint = Vector3.zero;
            lateralError = 0f;
            forwardDirection = Vector3.forward;
            return;
        }

        // ── Adım 1: Önbelleğe alınmış (cached) noktalar üzerinden kaba bir arama yap ──
        float minDistSqr = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < _cachedPoints.Count; i++)
        {
            Vector3 diff = worldPos - _cachedPoints[i];
            diff.y = 0f;
            float distSqr = diff.sqrMagnitude;

            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                bestIndex = i;
            }
        }

        // ── Adım 2: Üçlü Arama (Ternary Search) kullanarak sürekli ve kesin izdüşümü bul ──
        // Bu işlem, spline önbelleğindeki (cache) ayrıklaştırma gürültüsünü (discretization noise) tamamen ortadan kaldırır.
        int totalSamples = SegmentCount * samplesPerSegment;
        float t_best = (float)bestIndex / totalSamples;
        float step = 1f / totalSamples;
        
        float t_min = t_best - step;
        float t_max = t_best + step;
        
        // Kesin yerel minimum mesafeyi bulmak için Üçlü Arama
        for (int iter = 0; iter < 10; iter++)
        {
            float t_mid1 = t_min + (t_max - t_min) / 3f;
            float t_mid2 = t_max - (t_max - t_min) / 3f;
            
            Vector3 p1 = EvaluateGlobal(t_mid1);
            Vector3 p2 = EvaluateGlobal(t_mid2);
            
            p1.y = 0f; p2.y = 0f;
            Vector3 wp = worldPos; wp.y = 0f;
            
            float d1 = (p1 - wp).sqrMagnitude;
            float d2 = (p2 - wp).sqrMagnitude;
            
            if (d1 < d2)
                t_max = t_mid2;
            else
                t_min = t_mid1;
        }
        
        float final_t = (t_min + t_max) / 2f;
        nearestPoint = EvaluateGlobal(final_t);
        
        // ── Adım 3: Gerçek analitik teğeti (forward direction) al ──
        Vector3 tangent = EvaluateTangentGlobal(final_t);
        tangent.y = 0f;
        tangent.Normalize();
        forwardDirection = tangent;

        // ── Adım 4: İşaretli yanal hatayı (signed lateral error) hesapla ──
        Vector3 toVehicle = worldPos - nearestPoint;
        toVehicle.y = 0f;
        float distance = toVehicle.magnitude;

        // Cross product (vektörel çarpım) aracın yolun sağında mı solunda mı olduğunu belirler
        Vector3 cross = Vector3.Cross(tangent, toVehicle);
        float sign = Mathf.Sign(cross.y);

        lateralError = sign * distance;
    }

    /// <summary>
    /// Global t [0, 1] parametresinde Catmull-Rom spline'ını değerlendirir.
    /// Döngü sarmasını (loop wrapping) otomatik olarak işler.
    /// </summary>
    public Vector3 EvaluateGlobal(float globalT)
    {
        if (isClosedLoop) globalT = globalT - Mathf.Floor(globalT);
        else globalT = Mathf.Clamp01(globalT);
        
        int segIndex;
        float localT;
        GlobalTToSegment(globalT, out segIndex, out localT);
        return EvaluateCatmullRom(segIndex, localT);
    }
    
    /// <summary>
    /// Global t [0, 1] parametresindeki analitik teğeti değerlendirir.
    /// </summary>
    public Vector3 EvaluateTangentGlobal(float globalT)
    {
        if (isClosedLoop) globalT = globalT - Mathf.Floor(globalT);
        else globalT = Mathf.Clamp01(globalT);
        
        int segIndex;
        float localT;
        GlobalTToSegment(globalT, out segIndex, out localT);
        return EvaluateTangent(segIndex, localT);
    }

    /// <summary>
    /// Spline üzerinde, normalize edilmiş mesafe [0, 1] üzerinden bir nokta alır.
    /// Düzgün bir hız sağlamak için yay uzunluğu parametrelendirmesini (arc-length parameterization) kullanır.
    /// </summary>
    public Vector3 GetPointAtNormalizedDistance(float normalizedDist)
    {
        if (_cachedPoints == null || _cachedPoints.Count == 0)
            return Vector3.zero;

        float targetDist = normalizedDist * _totalLength;

        for (int i = 1; i < _cachedDistances.Count; i++)
        {
            if (_cachedDistances[i] >= targetDist)
            {
                float segFraction = (_cachedDistances[i] - targetDist) / 
                                     (_cachedDistances[i] - _cachedDistances[i - 1]);
                return Vector3.Lerp(_cachedPoints[i], _cachedPoints[i - 1], segFraction);
            }
        }

        return _cachedPoints[_cachedPoints.Count - 1];
    }

    // ─────────────────────────────────────────────
    // Yardımcı Metotlar (Helper Methods)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Global t [0,1] parametresini segment indeksine ve yerel t'ye dönüştürür.
    /// </summary>
    private void GlobalTToSegment(float globalT, out int segIndex, out float localT)
    {
        int segCount = SegmentCount;
        float scaled = globalT * segCount;
        segIndex = Mathf.Clamp((int)scaled, 0, segCount - 1);
        localT = scaled - segIndex;
    }

    /// <summary>
    /// Kapalı döngü (closed-loop) spline'ları için bir indeksi sarar (wrap).
    /// </summary>
    private int WrapIndex(int index, int count)
    {
        if (isClosedLoop)
        {
            return ((index % count) + count) % count;
        }
        return Mathf.Clamp(index, 0, count - 1);
    }

    // ─────────────────────────────────────────────
    // Editör Görselleştirmesi
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (controlPoints == null || controlPoints.Count < 4) return;

        // Kontrol noktalarını çiz (Sarı küreler)
        Gizmos.color = Color.yellow;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            Gizmos.DrawSphere(controlPoints[i], 0.5f);
        }

        // Spline eğrisini çiz (Camgöbeği çizgi)
        Gizmos.color = Color.cyan;
        int segments = SegmentCount;
        int steps = 20;

        for (int seg = 0; seg < segments; seg++)
        {
            Vector3 prev = EvaluateCatmullRom(seg, 0f);
            for (int step = 1; step <= steps; step++)
            {
                float t = (float)step / steps;
                Vector3 current = EvaluateCatmullRom(seg, t);
                Gizmos.DrawLine(prev, current);
                prev = current;
            }
        }
    }
#endif
}
