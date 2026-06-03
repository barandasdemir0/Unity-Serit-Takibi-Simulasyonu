using System.IO;
using UnityEngine;

/// <summary>
/// Verileri diskteki bir dosyaya fiziksel olarak kaydeden servis sınıfı.
/// BAĞLANTI: DataLogger.cs tarafından, metin (string) haline getirilmiş CSV'yi bilgisayara yazmak için kullanılır.
/// Sadece File I/O (Girdi/Çıktı) işlemlerinden sorumludur. (Single Responsibility Principle)
/// </summary>
public static class FileIOService
{
    /// <summary>
    /// Verilen metin içeriğini, belirtilen klasördeki belirtilen dosyaya yazar.
    /// </summary>
    /// <param name="folderPath">Dosyanın kaydedileceği klasör (Örn: C:/SimulasyonVerileri)</param>
    /// <param name="fileName">Kaydedilecek dosyanın adı (Örn: SeritTakibi_Tarih.csv)</param>
    /// <param name="content">Dosyanın içine yazılacak olan içerik (Koca bir metin / CSV formatı)</param>
    public static void SaveToFile(string folderPath, string fileName, string content)
    {
        try
        {
            // Eğer belirtilen klasör yolu bilgisayarda yoksa (Örn: C:/SimulasyonVerileri klasörü silinmişse)
            if (!Directory.Exists(folderPath))
            {
                // Önce o klasörü yarat
                Directory.CreateDirectory(folderPath);
            }

            // Klasör yolu ile dosya adını birleştirerek tam yolu (Full Path) oluştur (Örn: C:/Klasör/Dosya.csv)
            string fullPath = Path.Combine(folderPath, fileName);
            
            // C# System.IO kütüphanesini kullanarak tüm metni tek seferde diske yaz
            File.WriteAllText(fullPath, content);
            
            // İşlem başarılıysa Unity konsoluna yeşil log düş
            Debug.Log($"[FileIOService] Veriler başarıyla kaydedildi: {fullPath}");
        }
        catch (System.Exception e)
        {
            // Dosyaya yazma işlemi sırasında hata olursa (Örn: Yetki yok, disk dolu vs.) kırmızı hata (Error) logu düş
            Debug.LogError($"[FileIOService] Veri kaydedilirken hata oluştu: {e.Message}");
        }
    }
}
