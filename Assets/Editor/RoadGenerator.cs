using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;

public class RoadGenerator {
    [MenuItem("Tools/Generate Big Fun ZigZag Track")]
    public static void Generate() {
        // Spline objesini bul veya yarat
        GameObject splineObj = GameObject.Find("TrackSpline");
        if (splineObj == null) splineObj = GameObject.Find("Spline");
        if (splineObj == null) {
            splineObj = new GameObject("Spline");
        }
        
        SplineContainer container = splineObj.GetComponent<SplineContainer>();
        if (container == null) container = splineObj.AddComponent<SplineContainer>();
        
        Spline spline = container.Spline;
        if (spline == null) {
            spline = new Spline();
            container.Splines = new System.Collections.Generic.List<Spline>() { spline };
        }
        
        spline.Clear();
        spline.Closed = true;

        // ============================================================
        // GÜZEL, BÜYÜK, KESİŞMEYEN YARIŞ PİSTİ
        // Gerçek bir yarış pisti gibi: saat yönünde dönen oval tabanlı,
        // 15 zig-zag virajlı, kendi üstüne ASLA binmeyen tasarım.
        // Plane boyutu 500x500 (koordinatlar -250..+250 arası)
        // ============================================================

        // BAŞLANGIÇ DÜZLÜKLERİ (aşağı ortadan yukarı doğru düz çıkış)
        spline.Add(new BezierKnot(new float3(  0, 0, -200)));  // START
        spline.Add(new BezierKnot(new float3(  0, 0, -120)));  // düzlük sonu

        // 1. viraj: sola kıvrıl
        spline.Add(new BezierKnot(new float3(-80, 0,  -60)));

        // 2. viraj: sağa geri dön
        spline.Add(new BezierKnot(new float3( 60, 0,  -20)));

        // 3. viraj: sola keskin
        spline.Add(new BezierKnot(new float3(-90, 0,   30)));

        // 4. viraj: sağa geniş
        spline.Add(new BezierKnot(new float3( 80, 0,   70)));

        // 5. viraj: sola
        spline.Add(new BezierKnot(new float3(-70, 0,  110)));

        // 6. viraj: sağa
        spline.Add(new BezierKnot(new float3( 90, 0,  150)));

        // 7. viraj: sola doğru tepeye çık
        spline.Add(new BezierKnot(new float3(-50, 0,  190)));

        // TEPE U-DÖNÜŞÜ (haritanın üst kısmında geniş sağ dönüş)
        // 8-9-10. virajlar
        spline.Add(new BezierKnot(new float3( 40, 0,  220)));
        spline.Add(new BezierKnot(new float3(150, 0,  200)));
        spline.Add(new BezierKnot(new float3(180, 0,  150)));

        // DÖNÜŞ YOLU (sağ taraftan aşağı inerek geri gel)
        // 11. viraj
        spline.Add(new BezierKnot(new float3(120, 0,   90)));

        // 12. viraj
        spline.Add(new BezierKnot(new float3(200, 0,   30)));

        // 13. viraj
        spline.Add(new BezierKnot(new float3(130, 0,  -30)));

        // 14. viraj
        spline.Add(new BezierKnot(new float3(190, 0,  -90)));

        // 15. viraj: son keskin dönüş, başlangıca yaklaş
        spline.Add(new BezierKnot(new float3(100, 0, -150)));

        // ALT U-DÖNÜŞÜ (başlangıç düzlüğüne bağlan)
        spline.Add(new BezierKnot(new float3( 50, 0, -210)));

        // AutoSmooth: virajları doğal kıvrımlı yap
        for (int i = 0; i < spline.Count; ++i) {
            spline.SetTangentMode(i, TangentMode.AutoSmooth);
        }

        EditorUtility.SetDirty(splineObj);

        // Çevreyi ve Mesh'i yeniden oluştur
        var type = System.Type.GetType("UserTaskHandler, Assembly-CSharp-Editor");
        if (type != null) {
            var method = type.GetMethod("Execute", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (method != null) {
                method.Invoke(null, null);
                Debug.Log("15 virajlı büyük yarış pisti oluşturuldu!");
            }
        }
    }
}
