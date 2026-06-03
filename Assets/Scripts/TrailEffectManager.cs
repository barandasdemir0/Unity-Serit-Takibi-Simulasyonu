using UnityEngine;

/// <summary>
/// Aracın arkasında oluşan turuncu/kırmızı izi (Trail) yöneten modül.
/// BAĞLANTI: CarLaneTracker.cs Start() fonksiyonu içinden çağrılır.
/// </summary>
public static class TrailEffectManager
{
    /// <summary>
    /// Hedef objeye (araca) TrailRenderer bileşeni ekleyerek ayarlarını yapar.
    /// </summary>
    /// <param name="vehicle">Efektin ekleneceği araç objesi</param>
    public static void AttachTrail(GameObject vehicle)
    {
        // Araca Unity'nin yerleşik "TrailRenderer" bileşenini ekle
        var trail = vehicle.AddComponent<TrailRenderer>();
        
        // İzin başlangıç kalınlığını ayarla (Araçtan çıkarkenki kalınlık)
        trail.startWidth = 0.4f; 
        
        // İzin bitiş kalınlığını ayarla (Kaybolurkenki kalınlık)
        trail.endWidth = 0f; 
        
        // İzin ekranda kalma süresi (Saniye) - Çok uzun olursa drift yapıyormuş gibi görünür
        trail.time = 2f;
        
        // İze varsayılan, ışıklandırmasız düz bir materyal ata
        trail.material = new Material(Shader.Find("Sprites/Default"));
        
        // İzin başlangıç rengini belirle (Turuncumsu kırmızı, %90 opak)
        trail.startColor = new Color(1f, 0.3f, 0f, 0.9f);
        
        // İzin bitiş rengini belirle (Aynı renk ama tamamen saydam/kaybolmuş)
        trail.endColor = new Color(1f, 0.3f, 0f, 0f);
    }
}
