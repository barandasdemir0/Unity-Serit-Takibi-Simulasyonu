using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class DataLogger : MonoBehaviour
{
    private struct LogEntry
    {
        public float time;
        public float error;        // e(t)
        public float controlSignal; // u(t)
        public Vector3 carPos;     // y(t) — araç konumu
        public Vector3 refPos;     // r(t) — referans konumu
        public string mode;        // P / PI / PID
        public float speed;        // v(t) — anlık hız
        public float targetSpeed;  // hedef hız
    }

    private List<LogEntry> logs = new List<LogEntry>();
    private float startTime;

    [Header("Ayarlar")]
    public float logInterval = 0.05f; // 20 Hz kayıt
    private float nextLogTime = 0f;

    [Tooltip("Masaüstüne kaydedilecek dosya adı.")]
    public string fileName = "PID_Sonuclari.csv";

    void Start()
    {
        startTime = Time.time;
    }

    public void LogData(float error, float controlSignal, Vector3 carPosition, Vector3 referencePosition, string controllerMode = "PID", float speed = 0f, float targetSpeed = 0f)
    {
        if (Time.time >= nextLogTime)
        {
            logs.Add(new LogEntry
            {
                time = Time.time - startTime,
                error = error,
                controlSignal = controlSignal,
                carPos = carPosition,
                refPos = referencePosition,
                mode = controllerMode,
                speed = speed,
                targetSpeed = targetSpeed
            });
            nextLogTime = Time.time + logInterval;
        }
    }

    void OnApplicationQuit()
    {
        SaveToCSV();
    }

    public void SaveToCSV()
    {
        if (logs.Count == 0) return;

        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fullPath = Path.Combine(desktopPath, fileName);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Zaman(s);Mod;Hata_e(t);Kontrol_u(t);Arac_X;Arac_Z;Referans_X;Referans_Z;Hiz(m/s);HedefHiz(m/s)");

        foreach (var log in logs)
        {
            string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:F3};{1};{2:F4};{3:F4};{4:F3};{5:F3};{6:F3};{7:F3};{8:F2};{9:F2}",
                log.time, log.mode, log.error, log.controlSignal,
                log.carPos.x, log.carPos.z,
                log.refPos.x, log.refPos.z,
                log.speed, log.targetSpeed);
            sb.AppendLine(line);
        }

        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("PID Verileri Kaydedildi: " + fullPath + " (" + logs.Count + " satır)");
    }
}
