using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RoadSpline üzerinden prosedürel (procedural) olarak 3D yol mesh'i (ağı) üretir.
/// Şerit çizgileriyle (merkez çizgi + kenar çizgileri) birlikte yol yüzeyini oluşturur.
/// Yol mesh'i, hem görsel olarak render edilmek hem de pist sınırlarını belirlemek için kullanılır.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RoadSpline))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoadMeshGenerator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Yol Boyutları
    // ─────────────────────────────────────────────
    [Header("Yol Boyutları")]
    [Tooltip("Metre cinsinden yol yüzeyinin toplam genişliği")]
    [Range(4f, 20f)]
    public float roadWidth = 8f;

    [Tooltip("Her bir spline segmenti (parçası) başına üretilecek mesh (ağ) çözünürlüğü")]
    [Range(5, 50)]
    public int meshResolution = 30;

    // ─────────────────────────────────────────────
    // Şerit Çizgileri
    // ─────────────────────────────────────────────
    [Header("Şerit Çizgileri")]
    [Tooltip("Referans yol r(t) için görünür bir merkez çizgi oluşturur")]
    public bool showCenterLine = true;

    [Tooltip("Metre cinsinden merkez çizginin genişliği")]
    [Range(0.05f, 0.5f)]
    public float centerLineWidth = 0.15f;

    [Tooltip("Şerit çizgilerinin yol yüzeyinden yukarıdaki yükseklik ofseti")]
    public float lineHeightOffset = 0.02f;

    // ─────────────────────────────────────────────
    // Referanslar
    // ─────────────────────────────────────────────
    [Header("Materyaller")]
    [Tooltip("Yol yüzeyi için materyal")]
    public Material roadMaterial;

    [Tooltip("Şerit çizgileri için materyal")]
    public Material lineMaterial;

    private RoadSpline _spline;
    private GameObject _centerLineObject;

    private void OnEnable()
    {
        _spline = GetComponent<RoadSpline>();
        EnsureMaterials();
        GenerateRoad();
    }

    private void EnsureMaterials()
    {
        if (roadMaterial == null)
        {
            Shader standard = Shader.Find("UI/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (standard != null)
            {
                roadMaterial = new Material(standard);
            }
        }
        if (roadMaterial != null)
        {
            roadMaterial.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f)); // Koyu asfalt rengi
            roadMaterial.color = new Color(0.05f, 0.05f, 0.05f);                 // Geri dönüş (Fallback)
            roadMaterial.SetFloat("_Smoothness", 0.1f);
        }

        if (lineMaterial == null)
        {
            Shader unlitColor = Shader.Find("UI/Default") ?? Shader.Find("Unlit/Color");
            if (unlitColor != null)
            {
                lineMaterial = new Material(unlitColor);
            }
        }
        if (lineMaterial != null)
        {
            lineMaterial.SetColor("_BaseColor", Color.white);
            lineMaterial.color = Color.white;
        }
    }

    /// <summary>
    /// Spline verilerinden eksiksiz yol mesh'ini üretir.
    /// Yol yüzeyi + merkez çizgisi oluşturur.
    /// </summary>
    [ContextMenu("Force Regenerate Road")]
    public void GenerateRoad()
    {
        if (_spline == null)
            _spline = GetComponent<RoadSpline>();

        if (_spline.controlPoints.Count < 4) return;

        GenerateRoadMesh();

        if (showCenterLine)
            GenerateCenterLine();
    }

    /// <summary>
    /// Spline boyunca bir genişlik profilini uzatarak (extrude) yol yüzeyi mesh'ini oluşturur.
    /// </summary>
    private void GenerateRoadMesh()
    {
        int segCount = _spline.SegmentCount;
        int totalSteps = segCount * meshResolution;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float halfWidth = roadWidth * 0.5f;
        float uvDistance = 0f;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;

            // Segment indeksini ve yerel t'yi al
            int segIndex = Mathf.Min((int)(t * segCount), segCount - 1);
            float localT = (t * segCount) - segIndex;

            // Spline pozisyonunu ve teğetini hesapla
            Vector3 point = _spline.EvaluateCatmullRom(segIndex, localT);
            Vector3 tangent = _spline.EvaluateTangent(segIndex, localT);

            // Dik yönü hesapla (Yol genişliği yönü)
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            // Sol ve sağ kenar köşeleri (vertices)
            Vector3 leftVertex  = point - right * halfWidth;
            Vector3 rightVertex = point + right * halfWidth;

            // Yere tam oturması için hafif Y ofseti
            leftVertex.y  = 0.01f;
            rightVertex.y = 0.01f;

            vertices.Add(leftVertex);
            vertices.Add(rightVertex);

            // UV eşlemesi (yol uzunluğu boyunca uzat)
            if (i > 0)
                uvDistance += Vector3.Distance(point, prevPoint);

            float uvY = uvDistance / roadWidth;
            uvs.Add(new Vector2(0f, uvY));
            uvs.Add(new Vector2(1f, uvY));

            // Üçgenleri (triangles) oluştur (Her dörtgen (quad) için iki tane)
            if (i > 0)
            {
                int baseIndex = (i - 1) * 2;
                // 1. Üçgen
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                // 2. Üçgen
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            prevPoint = point;
        }

        // Mesh'i (ağı) inşa et ve ata
        Mesh roadMesh = new Mesh();
        roadMesh.name = "ProceduralRoad";
        roadMesh.vertices = vertices.ToArray();
        roadMesh.triangles = triangles.ToArray();
        roadMesh.uv = uvs.ToArray();
        roadMesh.RecalculateNormals();
        roadMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = roadMesh;
        if (roadMaterial != null)
        {
            GetComponent<MeshRenderer>().sharedMaterial = roadMaterial;
        }

        // Zemin algılaması için mesh collider ekle
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = roadMesh;
    }

    /// <summary>
    /// Görünür bir referans çizgisi olarak spline merkezi boyunca ince bir mesh şeridi oluşturur.
    /// Bu, aracın takip etmesi gereken ideal yolu r(t) temsil eder.
    /// </summary>
    private void GenerateCenterLine()
    {
        if (_centerLineObject != null)
            DestroyImmediate(_centerLineObject);

        _centerLineObject = new GameObject("CenterLine");
        _centerLineObject.transform.SetParent(transform);
        _centerLineObject.transform.localPosition = Vector3.zero;

        MeshFilter mf = _centerLineObject.AddComponent<MeshFilter>();
        MeshRenderer mr = _centerLineObject.AddComponent<MeshRenderer>();

        if (lineMaterial != null)
            mr.material = lineMaterial;

        int segCount = _spline.SegmentCount;
        int totalSteps = segCount * meshResolution;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float halfLine = centerLineWidth * 0.5f;
        float currentDistance = 0f;
        Vector3 prevPoint = _spline.EvaluateCatmullRom(0, 0f);

        for (int i = 0; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;
            int segIndex = Mathf.Min((int)(t * segCount), segCount - 1);
            float localT = (t * segCount) - segIndex;

            Vector3 point = _spline.EvaluateCatmullRom(segIndex, localT);
            
            if (i > 0)
                currentDistance += Vector3.Distance(point, prevPoint);
            prevPoint = point;

            Vector3 tangent = _spline.EvaluateTangent(segIndex, localT);
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            Vector3 left  = point - right * halfLine;
            Vector3 rightV = point + right * halfLine;

            left.y  = lineHeightOffset + 0.02f;
            rightV.y = lineHeightOffset + 0.02f;

            vertices.Add(left);
            vertices.Add(rightV);

            // Desen: 1.5m çizgi, 1.5m boşluk (Kesik çizgiler için)
            float dashLength = 1.5f;
            float gapLength = 1.5f;
            float totalPattern = dashLength + gapLength;

            if (i > 0)
            {
                int baseIdx = (i - 1) * 2;
                // Kesik çizgi efekti için boşluklar bırak
                if (currentDistance % totalPattern < dashLength)
                {
                    triangles.Add(baseIdx);
                    triangles.Add(baseIdx + 2);
                    triangles.Add(baseIdx + 1);
                    triangles.Add(baseIdx + 1);
                    triangles.Add(baseIdx + 2);
                    triangles.Add(baseIdx + 3);
                }
            }
        }

        Mesh lineMesh = new Mesh();
        lineMesh.name = "CenterLine";
        lineMesh.vertices = vertices.ToArray();
        lineMesh.triangles = triangles.ToArray();
        lineMesh.RecalculateNormals();

        mf.sharedMesh = lineMesh;
    }
}
