using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Araç verilerini CSV dosyasına dışa aktarmak için toplayan kayıt (Logger) modülü.
/// BAĞLANTI: CarLaneTracker.cs tarafından belirli aralıklarla tetiklenir.
/// Verileri diskteki CSV dosyasına yazmak için FileIOService.cs'i kullanır.
/// </summary>
public class DataLogger : MonoBehaviour
{
    // Verilerin hangi sıklıkla kaydedileceği (Örn: 0.1 saniye = Saniyede 10 kayıt)
    public float logInterval = 0.1f;
    
    // Bir sonraki kaydın alınacağı zaman
    private float _nextLogTime = 0f;
    
    // Dışa aktarılacak CSV verilerini biriktirdiğimiz liste
    private List<CarDataRecorder.FrameData> _csvData = new List<CarDataRecorder.FrameData>();
    
    // CSV dosyasının kaydedileceği klasör yolu
    public string folderPath = "C:/SimulasyonVerileri";
    
    // CSV dosyasının adı
    public string fileName = "SeritTakibi";

    /// <summary>
    /// Eğer zamanı geldiyse o anki durumu CSV için biriktirilen listeye ekler.
    /// BAĞLANTI: CarLaneTracker.cs FixedUpdate'inden her kare çağrılır ama sadece logInterval dolduğunda kayıt alır.
    /// </summary>
    public void LogData(CarLaneTracker tracker)
    {
        // Eğer araç takipçisi (tracker) yoksa veya kaydedilmiş hiç veri yoksa çık
        if (tracker == null || tracker.Recorder.RecordedData.Count == 0) return;

        // Belirlenen kayıt aralığı (logInterval) süresi dolmuşsa
        if (Time.time >= _nextLogTime)
        {
            // O anki en son frame'i (kaydı) alıp CSV listemize ekliyoruz
            var data = tracker.Recorder.RecordedData;
            _csvData.Add(data[data.Count - 1]);
            
            // Bir sonraki kayıt zamanını belirle
            _nextLogTime = Time.time + logInterval;
        }
    }

    /// <summary>
    /// Biriktirilen tüm verileri virgülle ayrılmış (CSV) metin formatına çevirip dışa aktarır.
    /// BAĞLANTI: CSVExportUIManager.cs tarafından (Butona basılınca) tetiklenir.
    /// </summary>
    public void ExportData()
    {
        // Kaydedilecek veri yoksa boşuna dosya oluşturma
        if (_csvData.Count == 0) return;

        // Bellek (RAM) verimliliği için metinleri StringBuilder ile birleştiriyoruz
        StringBuilder sb = new StringBuilder();
        
        // CSV Sütun başlıkları (İlk satır)
        sb.AppendLine("Zaman(s),Yanal Hata(m),Kontrol Cikisi(u),Arac X,Referans X,P Terimi,I Terimi,D Terimi,Direksiyon Acisi,GlobalAracX,GlobalAracZ,GlobalHedefX,GlobalHedefZ");

        // Listemizdeki her bir kaydı sırayla dön
        foreach (var d in _csvData)
        {
            // Verileri virgüllerle ayırarak (F3 = 3 ondalıklı format vs.) birleştir ve alt satıra geç
            sb.AppendLine($"{d.time:F3},{d.lateralError:F4},{d.controlOutput:F4},{d.vehiclePosX:F4},{d.referencePosX:F4},{d.pTerm:F4},{d.iTerm:F4},{d.dTerm:F4},{d.steeringAngle:F2},{d.vehicleGlobalX:F2},{d.vehicleGlobalZ:F2},{d.targetGlobalX:F2},{d.targetGlobalZ:F2}");
        }

        // Dosya isminin sonuna anlık Saat ve Tarihi ekle ki eski dosyaların üstüne yazmasın
        string fullFileName = $"{fileName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        
        // BAĞLANTI: Oluşturulan koca metni (sb.ToString()) diske yazması için servise gönder.
        FileIOService.SaveToFile(folderPath, fullFileName, sb.ToString());
    }

    /// <summary>
    /// Geçmiş CSV verilerini temizler (Simülasyon sıfırlandığında).
    /// </summary>
    public void ClearData()
    {
        _csvData.Clear();
        _nextLogTime = Time.time;
    }
}
