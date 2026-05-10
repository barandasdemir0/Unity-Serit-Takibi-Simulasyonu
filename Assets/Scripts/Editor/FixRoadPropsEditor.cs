using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class FixRoadPropsEditor : EditorWindow
{
    [MenuItem("Tools/Yoldaki Binalari Temizle")]
    public static void CleanRoad()
    {
        var spline = FindAnyObjectByType<SplineContainer>();
        if (spline == null) 
        {
            Debug.LogError("Spline bulunamadi!");
            return;
        }

        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int movedCount = 0;

        foreach (var go in rootObjects)
        {
            // Araba, Zemin, Yol, Işık veya Arayüz haricindekileri kontrol et
            if (go.name.Contains("Araba") || go.name.Contains("Spline") || go.name.Contains("Plane") || 
                go.name.Contains("Terrain") || go.name.Contains("Directional Light") || go.name.Contains("Canvas"))
                continue;

            // Sadece içinde MeshRenderer olan gerçek objeleri temizle
            if (go.GetComponentsInChildren<MeshRenderer>(true).Length == 0)
                continue;

            float3 localPos = math.transform(spline.transform.worldToLocalMatrix, go.transform.position);
            SplineUtility.GetNearestPoint(spline.Spline, localPos, out float3 nearestLocal, out float t);
            Vector3 nearestWorld = spline.transform.TransformPoint(nearestLocal);

            Vector3 offset = go.transform.position - nearestWorld;
            offset.y = 0; // Yukseklik onemli degil
            
            // Eger yol merkezine 8 metreden yakinsa yoldan disari it
            if (offset.magnitude < 8.5f)
            {
                if (offset.magnitude < 0.1f) offset = new Vector3(1, 0, 0); // Tam ortadaysa rastgele saga it
                
                // Objeyi yolun 12 metre disina it (büyük evler de yoldan çiksin)
                go.transform.position = nearestWorld + offset.normalized * 12.0f;
                movedCount++;
            }
        }

        Debug.Log($"<color=lime>[Islem Tamam]</color> {movedCount} adet bina/agac bütün olarak yolun ortasindan kenarlara itildi!");
    }
}
