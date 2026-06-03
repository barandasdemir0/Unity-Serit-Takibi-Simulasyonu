using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simülasyonda oluşan saniyelik verileri (Grafikler ve CSV için) RAM (Hafıza) üzerinde tutan modül.
/// BAĞLANTI: CarLaneTracker.cs tarafından her kare güncellenir.
/// DataLogger.cs ve GraphRenderer.cs bu sınıfın içindeki kayıtlı verileri okur.
/// </summary>
public class CarDataRecorder
{
    /// <summary>
    /// Her bir anlık karenin (frame) verilerini temsil eden yapı (Struct).
    /// </summary>
    public struct FrameData
    {
        // Zaman, Yanal Hata, Kontrol Çıkışı, Aracın Yanal Pozisyonu, Referans Nokta Pozisyonu
        public float time, lateralError, controlOutput, vehiclePosX, referencePosX;
        
        // Oransal, İntegral, Türevsel Terim Değerleri ve Anlık Direksiyon Açısı
        public float pTerm, iTerm, dTerm, steeringAngle;
        
        // Aracın ve Hedefin Dünya Üzerindeki X ve Z Koordinatları
        public float vehicleGlobalX, vehicleGlobalZ, targetGlobalX, targetGlobalZ;
    }

    // Geçmiş verileri RAM üzerinde sakladığımız liste
    public List<FrameData> RecordedData { get; private set; } = new List<FrameData>();
    
    // RAM'in dolmaması için grafikte çizilecek/tutulacak maksimum frame sayısı
    public int maxRecordedFrames = 2000;
    
    // Simülasyonun başladığı ilk an (Sıfırıncı saniye)
    private float _startTime;

    /// <summary>
    /// Kaydı sıfırlayıp baştan başlatır. 
    /// BAĞLANTI: Yeniden Başlat (Restart) butonuna basılınca çalışır.
    /// </summary>
    public void StartRecording()
    {
        _startTime = Time.time;
        RecordedData.Clear();
    }

    /// <summary>
    /// O anki frame'de oluşan tüm parametreleri alıp listeye (RAM'e) kaydeder.
    /// BAĞLANTI: CarLaneTracker.cs FixedUpdate içinden çağırır.
    /// </summary>
    public void RecordFrame(float error, float controlOut, PIDController pid, float steerAngle, Vector3 vehiclePos, Vector3 targetPos)
    {
        // Yeni bir paket oluştur ve içini doldur
        var data = new FrameData
        {
            time = Time.time - _startTime, // Geçen toplam süre
            lateralError = error,
            controlOutput = controlOut,
            vehiclePosX = error,
            referencePosX = 0f, // Referans her zaman sıfır noktası kabul edilir
            pTerm = pid.LastProportional, 
            iTerm = pid.LastIntegral, 
            dTerm = pid.LastDerivative,
            steeringAngle = steerAngle,
            vehicleGlobalX = vehiclePos.x, 
            vehicleGlobalZ = vehiclePos.z,
            targetGlobalX = targetPos.x, 
            targetGlobalZ = targetPos.z
        };

        // Oluşturulan paketi listeye ekle
        RecordedData.Add(data);
        
        // Eğer liste sınırını (2000) aştıysa, en eski (ilk sıradaki) veriyi silerek listeyi kaydır
        if (RecordedData.Count > maxRecordedFrames) RecordedData.RemoveAt(0);
    }
    
    /// <summary>
    /// Kayıtlı tüm verileri temizler.
    /// </summary>
    public void Clear() => RecordedData.Clear();
}
