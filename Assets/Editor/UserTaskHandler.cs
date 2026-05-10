using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

public class UserTaskHandler {
    [MenuItem("Tools/Execute User Task")]
    public static void Execute() {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader fallbackShader = Shader.Find("Standard");
        Shader targetShader = urpShader != null ? urpShader : fallbackShader;

        // 1. ZEMİN (Plane)
        GameObject plane = GameObject.Find("Plane");
        if (plane != null) {
            MeshRenderer mr = plane.GetComponent<MeshRenderer>();
            if (mr != null) {
                Material mat = new Material(targetShader);
                mat.color = new Color(0.15f, 0.4f, 0.15f); // Daha doğal koyu çimen yeşili
                mr.material = mat;
            }
        }

        // 2. KÜP -> ARABA
        GameObject cube = GameObject.Find("ArabaCube");
        if (cube == null) {
            cube = new GameObject("ArabaCube");
        } else {
            MeshRenderer mr = cube.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);
            MeshFilter mf = cube.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
            Collider col = cube.GetComponent<BoxCollider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        bool alreadyDressed = false;
        foreach (Transform child in cube.transform) {
            if (child.name.Contains("Free Racing Car") || child.name.Contains("Variant")) alreadyDressed = true;
        }

        if (!alreadyDressed) {
            string[] guids = AssetDatabase.FindAssets("Free Racing Car Red Variant t:Prefab");
            if (guids.Length > 0) {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (carPrefab != null) {
                    GameObject carInstance = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
                    PrefabUtility.UnpackPrefabInstance(carInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    carInstance.transform.SetParent(cube.transform, false);
                    carInstance.transform.localPosition = Vector3.zero;
                    carInstance.transform.localRotation = Quaternion.identity;
                    carInstance.transform.localScale = new Vector3(2f, 2f, 2f); 
                }
            }
        }

        // Uyarıları kalıcı olarak silmek için WheelCollider'ları kökten yok et
        WheelCollider[] wCols = cube.GetComponentsInChildren<WheelCollider>(true);
        foreach (WheelCollider wc in wCols) {
            Object.DestroyImmediate(wc.gameObject);
        }

        // Araba yerçekimi ve fiziğini korusun
        Rigidbody[] rbs = cube.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs) {
            rb.isKinematic = false;
        }

        if (urpShader != null) {
            Renderer[] carRenderers = cube.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in carRenderers) {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (mats[i] != null && mats[i].shader.name != urpShader.name) {
                        Material newMat = new Material(urpShader);
                        if (mats[i].HasProperty("_Color")) newMat.SetColor("_BaseColor", mats[i].color);
                        if (mats[i].HasProperty("_MainTex")) newMat.SetTexture("_BaseMap", mats[i].mainTexture);
                        mats[i] = newMat;
                    }
                }
                r.sharedMaterials = mats;
            }
        }

        // 3. YOLU OLUŞTUR
        GameObject splineObj = GameObject.Find("Spline");
        if (splineObj != null) {
            Component oldInst = splineObj.GetComponent("SplineInstantiate");
            if (oldInst != null) Object.DestroyImmediate(oldInst);
            while (splineObj.transform.childCount > 0) {
                Object.DestroyImmediate(splineObj.transform.GetChild(0).gameObject);
            }

            SplineContainer container = splineObj.GetComponent<SplineContainer>();
            if (container != null) {
                MeshFilter mf = splineObj.GetComponent<MeshFilter>();
                if (mf == null) mf = splineObj.AddComponent<MeshFilter>();
                
                MeshRenderer mr = splineObj.GetComponent<MeshRenderer>();
                if (mr == null) mr = splineObj.AddComponent<MeshRenderer>();

                MeshCollider mc = splineObj.GetComponent<MeshCollider>();
                if (mc == null) mc = splineObj.AddComponent<MeshCollider>();

                // YOL TEXTURE ÜRETİMİ
                Texture2D roadTex = new Texture2D(256, 256);
                roadTex.wrapMode = TextureWrapMode.Repeat;
                for (int y = 0; y < 256; y++) {
                    for (int x = 0; x < 256; x++) {
                        Color c = new Color(0.15f, 0.15f, 0.17f); // Daha koyu, gerçekçi asfalt
                        if (x > 124 && x < 132) {
                            if (y % 64 < 32) c = new Color(0.9f, 0.9f, 0.9f); // Daha ince beyaz kesik çizgi
                        }
                        if (x > 10 && x < 15) {
                            c = new Color(0.9f, 0.8f, 0.1f); // Sol sürekli SARI şerit
                        }
                        if (x > 240 && x < 245) {
                            c = new Color(0.9f, 0.9f, 0.9f); // Sağ sürekli BEYAZ şerit
                        }
                        if (x < 6 || x > 250) {
                            c = new Color(0.7f, 0.1f, 0.1f); // Kırmızı bordür
                        }
                        roadTex.SetPixel(x, y, c);
                    }
                }
                roadTex.Apply();

                Material roadMat = new Material(targetShader);
                if (roadMat.HasProperty("_BaseMap")) roadMat.SetTexture("_BaseMap", roadTex);
                if (roadMat.HasProperty("_MainTex")) roadMat.SetTexture("_MainTex", roadTex);
                mr.material = roadMat;

                // SPLINE MESH ÖRÜMÜ
                Mesh roadMesh = new Mesh();
                List<Vector3> verts = new List<Vector3>();
                List<int> tris = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                float roadWidth = 8f; 
                int resolution = 400; 
                float splineLength = container.CalculateLength();
                
                GameObject oldProps = GameObject.Find("EnvironmentProps");
                while(oldProps != null) {
                    Object.DestroyImmediate(oldProps);
                    oldProps = GameObject.Find("EnvironmentProps");
                }

                GameObject environmentProps = new GameObject("EnvironmentProps");
                // World space parent — Create* fonksiyonları world position kullanıyor
                environmentProps.transform.SetParent(null);

                for (int i = 0; i <= resolution; i++) {
                    float t = i / (float)resolution;
                    float3 posLocal, tangentLocal, upLocal;
                    
                    SplineUtility.Evaluate(container.Spline, t, out posLocal, out tangentLocal, out upLocal);
                    
                    // Local -> World dönüşümü
                    Vector3 worldPos     = splineObj.transform.TransformPoint(posLocal);
                    Vector3 worldForward = splineObj.transform.TransformDirection(math.normalize(tangentLocal));
                    if (worldForward == Vector3.zero) worldForward = Vector3.forward;
                    Vector3 worldRight   = Vector3.Cross(Vector3.up, worldForward).normalized;

                    // Mesh vertex hesabı (local uzayda kalmalı)
                    Vector3 pos     = posLocal;
                    Vector3 forward = worldForward;
                    Vector3 right   = worldRight;

                    Vector3 leftPoint = pos - right * (roadWidth / 2f) + new Vector3(0, 0.05f, 0);
                    Vector3 rightPoint = pos + right * (roadWidth / 2f) + new Vector3(0, 0.05f, 0);

                    verts.Add(leftPoint);
                    verts.Add(rightPoint);

                    float v = (t * splineLength) / 4f; 
                    uvs.Add(new Vector2(0, v));
                    uvs.Add(new Vector2(1, v));

                    if (i < resolution) {
                        int baseIdx = i * 2;
                        tris.Add(baseIdx);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 1);

                        tris.Add(baseIdx + 1);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 3);
                    }

                    // === YOL KENARI ORTAMI (world koordinatlarında) ===

                    // 1. BARIYER (her 4 adımda bir)
                    if (i % 4 == 0) {
                        Vector3 lBarrier = worldPos - worldRight * (roadWidth / 2f + 0.7f) + Vector3.up * 0.3f;
                        Vector3 rBarrier = worldPos + worldRight * (roadWidth / 2f + 0.7f) + Vector3.up * 0.3f;
                        CreateBarrier(lBarrier, worldForward, environmentProps.transform, targetShader);
                        CreateBarrier(rBarrier, worldForward, environmentProps.transform, targetShader);
                    }

                    // 2. AĞAÇ SİRASI (her 12 adımda bir)
                    if (i % 12 == 0) {
                        float treeOff = roadWidth / 2f + UnityEngine.Random.Range(4f, 9f);
                        CreateTree(worldPos - worldRight * treeOff, environmentProps.transform, targetShader);
                        CreateTree(worldPos + worldRight * treeOff, environmentProps.transform, targetShader);
                    }

                    // 3. SOKAK LAMBALARI (her 16 adımda bir virajın dışına/içine)
                    if (i % 16 == 0) {
                        float lampOff = roadWidth / 2f + 1.5f;
                        // Sadece bir tarafa koyalım (sağ taraf)
                        CreateStreetLight(worldPos + worldRight * lampOff, worldRight, environmentProps.transform, targetShader);
                    }

                    // 4. ÇALILAR (Rastgele dağılmış)
                    if (i % 5 == 0) {
                        float bushOff = roadWidth / 2f + UnityEngine.Random.Range(2f, 15f);
                        bool isLeft = UnityEngine.Random.value > 0.5f;
                        Vector3 bushPos = worldPos + (isLeft ? -worldRight : worldRight) * bushOff;
                        CreateBush(bushPos, environmentProps.transform, targetShader);
                    }
                }

                roadMesh.SetVertices(verts);
                roadMesh.SetTriangles(tris, 0);
                roadMesh.SetUVs(0, uvs);
                roadMesh.RecalculateNormals();
                mf.sharedMesh = roadMesh;
                mc.sharedMesh = roadMesh;

                // 4. GÖKYÜZÜ VE IŞIKLANDIRMA (GECE/GÜNDÜZ HİSSİYATI)
                Material skyMat = new Material(Shader.Find("Skybox/Procedural"));
                skyMat.SetFloat("_SunSize", 0.04f);
                skyMat.SetFloat("_SunSizeConvergence", 5f);
                skyMat.SetFloat("_AtmosphereThickness", 1.2f);
                skyMat.SetColor("_SkyTint", new Color(0.6f, 0.7f, 0.9f));
                skyMat.SetColor("_GroundColor", new Color(0.2f, 0.3f, 0.2f));
                skyMat.SetFloat("_Exposure", 1.3f);
                RenderSettings.skybox = skyMat;
                DynamicGI.UpdateEnvironment();

                Light light = Object.FindAnyObjectByType<Light>();
                if (light == null) {
                    GameObject lightObj = new GameObject("Directional Light");
                    light = lightObj.AddComponent<Light>();
                }
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(35, -30, 0);
                light.color = new Color(1f, 0.95f, 0.9f);
                light.intensity = 1.2f;
                RenderSettings.sun = light;

                // 5. ARABAYI BAŞLANGICA KOY VE SCRIPTLERİ AYARLA
                // Araç konumu CarLaneTracker.Start() içinde otomatik ayarlanır.
                // Başlangıç noktasını sadece sahne görünümü için burada da uygulayalım.
                /* 
        // Aracı spline'ın başlangıcına yerleştir
        if (targetSpline != null)
        {
            float3 startPosLocal = SplineUtility.EvaluatePosition(targetSpline.Spline, 0f);
            float3 startTanLocal = SplineUtility.EvaluateTangent(targetSpline.Spline, 0f);

            Vector3 startPos = targetSpline.transform.TransformPoint(startPosLocal);
            Vector3 startFwd = targetSpline.transform.TransformDirection(math.normalize(startTanLocal));

            transform.position = startPos + Vector3.up * 0.5f;
            transform.rotation = Quaternion.LookRotation(startFwd, Vector3.up);
        }
        */         if (cube.GetComponent<DataLogger>() == null) cube.AddComponent<DataLogger>();
                CarLaneTracker tracker = cube.GetComponent<CarLaneTracker>();
                if (tracker == null) tracker = cube.AddComponent<CarLaneTracker>();
                tracker.targetSpline = container;
                
                GameObject mainCam = GameObject.Find("Main Camera");
                if (mainCam != null) {
                    mainCam.transform.SetParent(cube.transform);
                    mainCam.transform.localPosition = new Vector3(0, 4f, -10f);
                    mainCam.transform.LookAt(cube.transform.position + new Vector3(0, 1f, 0));
                }

                // 6. UI GRAFİK YÖNETİCİSİNİ EKLE
                GameObject uiManagerObj = GameObject.Find("UIGraphSystem");
                if (uiManagerObj == null) uiManagerObj = new GameObject("UIGraphSystem");
                UIGraphManager uiManager = uiManagerObj.GetComponent<UIGraphManager>();
                if (uiManager == null) uiManager = uiManagerObj.AddComponent<UIGraphManager>();
                uiManager.carTracker = tracker;

                Debug.Log("Görsellik artırıldı, ağaçlar eklendi, gökyüzü iyileştirildi ve yol kesişimleri düzeltildi.");
            }
        }
    }

    // ====================================================
    // BARIYER: metal korkuluk şeridi
    // ====================================================
    private static void CreateBarrier(Vector3 pos, Vector3 forward, Transform parent, Shader shader) {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.transform.SetParent(parent, false);
        bar.transform.position = pos;
        bar.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        bar.transform.localScale = new Vector3(0.25f, 0.6f, 3f);
        Material m = new Material(shader);
        m.color = new Color(0.75f, 0.75f, 0.78f); // metalik gri
        bar.GetComponent<MeshRenderer>().material = m;
        Object.DestroyImmediate(bar.GetComponent<BoxCollider>());
    }

    // ====================================================
    // AĞAÇ: normal boyutlu, scale mirası olmadan
    // ====================================================
    private static void CreateTree(Vector3 worldPos, Transform parent, Shader shader) {
        float trunkH = UnityEngine.Random.Range(5f, 9f);  // 5-9 m
        float trunkR = 0.55f;

        // Gövde — parent'a doğrudan bağla, world pozisyonunu manuel ver
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.position = worldPos + Vector3.up * (trunkH / 2f);
        trunk.transform.localScale = new Vector3(trunkR * 2f, trunkH / 2f, trunkR * 2f);
        Material trunkMat = new Material(shader);
        trunkMat.color = new Color(0.32f, 0.16f, 0.05f);
        trunk.GetComponent<MeshRenderer>().material = trunkMat;
        Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());

        // Alt yaprak kümesi — parent'a bağla (trunk'a değil!), world pos hesapla
        float green1 = UnityEngine.Random.Range(0.35f, 0.6f);
        SpawnLeafWorld(worldPos + Vector3.up * (trunkH * 0.75f),
                       new Vector3(5f, 4.5f, 5f), green1, parent, shader);

        // Üst yaprak kümesi
        float green2 = UnityEngine.Random.Range(0.5f, 0.75f);
        SpawnLeafWorld(worldPos + Vector3.up * (trunkH * 1.1f),
                       new Vector3(3.5f, 3.5f, 3.5f), green2, parent, shader);
    }

    // Yaprak küresini doğrudan dünya koordinatlarında oluştur
    private static void SpawnLeafWorld(Vector3 worldPos, Vector3 scale, float green, Transform parent, Shader shader) {
        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.transform.SetParent(parent, false);
        leaves.transform.position = worldPos;   // world position, localScale etkilenmiyor
        leaves.transform.localScale = scale;    // direkt boyut, miras YOK
        Material m = new Material(shader);
        m.color = new Color(0.04f + green * 0.08f, green, 0.04f);
        leaves.GetComponent<MeshRenderer>().material = m;
        Object.DestroyImmediate(leaves.GetComponent<SphereCollider>());
    }

    // ====================================================
    // BİNA: basit kapısız dikdörtgen bloık bina
    // ====================================================
    private static void CreateBuilding(Vector3 pos, Transform parent, Shader shader) {
        float w = UnityEngine.Random.Range(8f, 16f);
        float h = UnityEngine.Random.Range(8f, 22f);
        float d = UnityEngine.Random.Range(8f, 14f);

        // Ana gövde
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent, false);
        body.transform.position = pos + Vector3.up * (h / 2f);
        body.transform.localScale = new Vector3(w, h, d);
        Material bMat = new Material(shader);
        // Krem / beton / kahve tonlarında rastgele
        float tone = UnityEngine.Random.Range(0f, 1f);
        if      (tone < 0.33f) bMat.color = new Color(0.78f, 0.72f, 0.62f); // krem
        else if (tone < 0.66f) bMat.color = new Color(0.55f, 0.55f, 0.58f); // beton gri
        else                   bMat.color = new Color(0.42f, 0.32f, 0.24f); // kahve
        body.GetComponent<MeshRenderer>().material = bMat;
        Object.DestroyImmediate(body.GetComponent<BoxCollider>());

        // Çatı (koyu band)
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(body.transform, false);
        roof.transform.localPosition = new Vector3(0, 0.52f, 0);
        roof.transform.localScale = new Vector3(1.04f, 0.08f, 1.04f);
        Material rMat = new Material(shader);
        rMat.color = new Color(0.2f, 0.2f, 0.22f);
        roof.GetComponent<MeshRenderer>().material = rMat;
        Object.DestroyImmediate(roof.GetComponent<BoxCollider>());
    }

    // ====================================================
    // SOKAK LAMBASI
    // ====================================================
    private static void CreateStreetLight(Vector3 pos, Vector3 rightDir, Transform parent, Shader shader) {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(parent, false);
        pole.transform.position = pos + Vector3.up * 3.5f;
        pole.transform.localScale = new Vector3(0.15f, 3.5f, 0.15f);
        Material mPole = new Material(shader);
        mPole.color = new Color(0.25f, 0.25f, 0.28f);
        pole.GetComponent<MeshRenderer>().material = mPole;
        Object.DestroyImmediate(pole.GetComponent<CapsuleCollider>());

        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.transform.SetParent(pole.transform, false);
        bulb.transform.localPosition = new Vector3(0, 1.02f, 0); 
        bulb.transform.localScale = new Vector3(3f, 0.2f, 3f); 
        
        Material mLamp = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if(mLamp == null || mLamp.shader == null) mLamp = new Material(Shader.Find("Unlit/Color"));
        mLamp.color = new Color(1f, 0.95f, 0.7f); // Sıcak sarımsı ışık
        bulb.GetComponent<MeshRenderer>().material = mLamp;
        Object.DestroyImmediate(bulb.GetComponent<SphereCollider>());
        
        // Asıl lamba kolu
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(pole.transform, false);
        arm.transform.localPosition = new Vector3(0, 0.95f, 0);
        arm.transform.rotation = Quaternion.LookRotation(rightDir, Vector3.up);
        arm.transform.localScale = new Vector3(0.5f, 0.05f, 4f); 
        arm.GetComponent<MeshRenderer>().material = mPole;
        Object.DestroyImmediate(arm.GetComponent<BoxCollider>());
    }

    // ====================================================
    // ÇALI (Bush)
    // ====================================================
    private static void CreateBush(Vector3 pos, Transform parent, Shader shader) {
        float size = UnityEngine.Random.Range(0.8f, 1.5f);
        GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bush.transform.SetParent(parent, false);
        bush.transform.position = pos + Vector3.up * (size * 0.4f);
        bush.transform.localScale = new Vector3(size * 1.2f, size * 0.8f, size * 1.1f);
        
        Material mBush = new Material(shader);
        float green = UnityEngine.Random.Range(0.4f, 0.6f);
        mBush.color = new Color(0.1f, green, 0.1f);
        bush.GetComponent<MeshRenderer>().material = mBush;
        Object.DestroyImmediate(bush.GetComponent<SphereCollider>());
    }
}
