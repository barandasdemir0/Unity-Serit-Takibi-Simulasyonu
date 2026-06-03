using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Aracı başlangıç konumuna (Spline'ın en başına) hata payı (Offset) ile yerleştiren modül.
/// BAĞLANTI: CarLaneTracker.cs içindeki ResetSimulation() metodu tarafından çağrılır.
/// </summary>
public static class VehiclePositionResetter
{
    /// <summary>
    /// Aracın konumunu ve yönünü başlangıca göre ayarlar.
    /// </summary>
    /// <param name="vehicle">Aracın (Kendi objemizin) Transformu</param>
    /// <param name="targetSpline">Referans alınan yol çizgisi (Spline)</param>
    /// <param name="initialError">Kullanıcının belirlediği Başlangıç Hatası (Yanal uzaklık)</param>
    public static void ResetPosition(Transform vehicle, SplineContainer targetSpline, float initialError)
    {
        // Eğer yol (spline) yoksa hiçbir işlem yapma
        if (targetSpline == null) return;

        // Spline'ın tam başındaki (t=0) noktayı al
        Vector3 p0 = targetSpline.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, 0f));
        
        // Yönü bulabilmek için spline'ın azıcık ilerisindeki (t=0.001) noktayı al
        Vector3 p1 = targetSpline.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(targetSpline.Spline, 0.001f));
        
        // Bu iki nokta arasındaki vektörü bularak yolun tam o anki ileri yönünü (Forward) hesapla
        Vector3 fwd = (p1 - p0).normalized;
        
        // Hata koruması: Vektör sıfırsa varsayılan ileri yönü kabul et
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;

        // İleri yön vektörü ile yukarı vektörünün çarpımından (Cross Product) sağ (Right) vektörü bul
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        
        // Aracı yola yerleştir (Başlangıç noktası + Yerden biraz yukarı + Sağ-sol hata miktarı)
        vehicle.position = p0 + Vector3.up * 0.5f + right * initialError;
        
        // Aracın burnunu tam yolun gidiş yönüne doğru döndür
        vehicle.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        
        // BAŞLANGICA IŞINLANIRKEN ÇIKAN DEVASA İZİ (TRAIL) TEMİZLE
        var trail = vehicle.GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();
    }
}
