using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Yanal Hata (Lateral Error) Hesaplayıcısı.
/// BAĞLANTI: CarLaneTracker.cs içinden her kare (frame) çağrılır.
/// Aracın mevcut konumu ile referans yol (Spline) arasındaki dikey (dik) mesafeyi bulur.
/// </summary>
public class LateralErrorCalculator
{
    // Son hesaplanan yanal hata (e(t))
    public float LateralError { get; private set; }
    
    // Yol üzerindeki aracın izdüşümü (En yakın nokta)
    public Vector3 NearestPointOnSpline { get; private set; }
    
    // Yolun o noktadaki yönü (Teğet / Tangent)
    public Vector3 PathForward { get; private set; }

    /// <summary>
    /// Verilen pozisyonun yola olan uzaklığını hesaplar.
    /// </summary>
    /// <param name="vehiclePos">Aracın şu anki 3B konumu</param>
    /// <param name="targetSpline">Referans yol çizgisi</param>
    /// <param name="vehicleForward">Aracın baktığı yön (Sol/Sağ ayrımı için kullanılır)</param>
    /// <returns>Hata (metre cinsinden mesafe)</returns>
    public float Calculate(Vector3 vehiclePos, SplineContainer targetSpline, Vector3 vehicleForward)
    {
        // Aracın global dünya koordinatını Spline'ın yerel koordinat sistemine (Local Space) çevir.
        Vector3 localVehiclePos = targetSpline.transform.InverseTransformPoint(vehiclePos);

        // Unity.Mathematics kütüphanesi kullanarak Spline API'sine uygun formata çevir
        float3 posFloat3 = new float3(localVehiclePos.x, localVehiclePos.y, localVehiclePos.z);
        
        // SplineUtility, aracın konumuna spline üzerindeki "en yakın" (nearest) noktayı matematiksel olarak bulur
        SplineUtility.GetNearestPoint(targetSpline.Spline, posFloat3, out float3 nearestPoint, out float t);
        
        // Bulunan "t" (0 ile 1 arasında ilerleme) noktasına denk gelen 3D pozisyonu al
        Vector3 nearestPointLocal = targetSpline.Spline.EvaluatePosition(t);
        // O noktadaki yolun yön vektörünü (teğet) al
        Vector3 pathTangentLocal = targetSpline.Spline.EvaluateTangent(t);

        // Spline objesinin lokal koordinatlarını dünyanın global koordinatlarına çevir
        NearestPointOnSpline = targetSpline.transform.TransformPoint(nearestPointLocal);
        PathForward = targetSpline.transform.TransformDirection(pathTangentLocal).normalized;

        // Araç ile yolun en yakın noktası arasındaki mesafe vektörü
        Vector3 errorVector = vehiclePos - NearestPointOnSpline;

        // Yolun gidiş yönüne göre "sağ" yön vektörünü bul (Çapraz Çarpım / Cross Product)
        Vector3 pathRight = Vector3.Cross(Vector3.up, PathForward).normalized;

        // Hata vektörünü yolun sağ yönüyle nokta çarpımına (Dot Product) sokarak işaretli mesafeyi bul
        // Pozitif çıkarsa araç sağda, negatif çıkarsa araç soldadır.
        float signedError = Vector3.Dot(errorVector, pathRight);

        // Hesaplanan değeri iç değişkene kaydet
        LateralError = signedError;

        return LateralError;
    }
}
