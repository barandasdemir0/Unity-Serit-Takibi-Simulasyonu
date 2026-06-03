using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Globalization;

/// <summary>
/// VehicleController'dan kaydedilen telemetri verilerini CSV dosyasına aktarır.
/// Bitirme projesi kapsamında analitik raporlama ve sistem kararlılık analizi için kullanışlıdır.
/// </summary>
public class CSVExporter : MonoBehaviour
{
    [Tooltip("Kaydedilen verileri içeren VehicleController referansı.")]
    public VehicleController vehicleController;

    [Tooltip("Dışa aktarılacak CSV dosyasının adı için ön ek.")]
    public string fileName = "PID_Simulation_Data";

    /// <summary>
    /// Mevcut RecordedData'yı projenin kök klasörüne CSV dosyası olarak aktarır.
    /// </summary>
    public void ExportData()
    {
        if (vehicleController == null || vehicleController.RecordedData == null || vehicleController.RecordedData.Count == 0)
        {
            Debug.LogWarning("CSVExporter: Dışa aktarılacak veri yok veya VehicleController atanmamış.");
            return;
        }

        // Dosyayı Assets klasörünün içine (Application.dataPath) veya kalıcı veri yoluna kaydedebiliriz.
        // Akademik proje için proje klasörünün yanına kaydetmek genellikle daha pratiktir.
        // Assets klasörünü karmaşıklaştırmamak adına bir üst dizine kaydediyoruz.
        // Application.dataPath ".../Assets" dizinini işaret eder.
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(projectPath, $"{fileName}_{timestamp}.csv");

        StringBuilder sb = new StringBuilder();
        // Başlık satırı — Tüm sistem sinyalleri dahil
        sb.AppendLine(
            "Time(s)," +
            "LateralError_e(m)," +       // e(t) = r(t) - y(t)
            "ControlOutput_u," +          // u(t) PID çıkışı [-1,1]
            "VehiclePos_y(m)," +          // y(t) yanal konum (spline frame)
            "ReferencePos_r(m)," +        // r(t) = 0 (sabit merkez)
            "P_Term," +                   // Kp * e(t)
            "I_Term," +                   // Ki * integral(e)
            "D_Term," +                   // Kd * de/dt
            "SteeringAngle_delta(deg)," + // δ(t) direksiyon açısı
            "VehicleGlobalX(m)," +        // Araç dünya X koordinatı
            "VehicleGlobalZ(m)," +        // Araç dünya Z koordinatı
            "TargetGlobalX(m)," +         // Referans dünya X koordinatı
            "TargetGlobalZ(m)"            // Referans dünya Z koordinatı
        );

        // Veri satırları
        foreach (var data in vehicleController.RecordedData)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F4},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4}",
                data.time,
                data.lateralError,
                data.controlOutput,
                data.vehiclePosX,
                data.referencePosX,
                data.pTerm,
                data.iTerm,
                data.dTerm,
                data.steeringAngle,
                data.vehicleGlobalX,
                data.vehicleGlobalZ,
                data.targetGlobalX,
                data.targetGlobalZ));
        }

        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"Veri başarıyla dışa aktarıldı: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV dışa aktarma başarısız: {e.Message}");
        }
    }
}
