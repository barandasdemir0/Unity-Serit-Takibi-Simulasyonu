using UnityEditor;
using UnityEngine;

public class FixCarModelEditor : EditorWindow
{
    [MenuItem("Tools/Arabayi Duzelt (Fix Car)")]
    public static void FixCar()
    {
        GameObject arabaCube = GameObject.Find("ArabaCube");
        if (arabaCube == null)
        {
            Debug.LogError("ArabaCube bulunamadi!");
            return;
        }

        Transform existingCar = null;
        foreach (Transform child in arabaCube.transform)
        {
            if (child.name.Contains("Free Racing Car"))
            {
                existingCar = child;
                break;
            }
        }

        if (existingCar != null)
        {
            Undo.RecordObject(existingCar, "Fix Car Positions");
            existingCar.localPosition = Vector3.zero;
            existingCar.localRotation = Quaternion.identity;

            foreach (Transform child in existingCar)
            {
                Undo.RecordObject(child, "Fix Car Positions");
                
                // Gövde ve rüzgarlık merkezde olmalı
                if (child.name.Contains("Body") || child.name.Contains("Spoiler") || child.name.Contains("Wheels"))
                {
                    child.localPosition = Vector3.zero;
                }

                // Tekerlekleri de düzeltelim (Wheels objesinin içindekiler)
                if (child.name.Contains("Wheels"))
                {
                    foreach (Transform wheel in child)
                    {
                        Undo.RecordObject(wheel, "Fix Wheel Positions");
                        // Tekerleklerin standart konumları (Free Racing Car modeli için)
                        if (wheel.name.Contains("FrontLeft")) wheel.localPosition = new Vector3(-0.846f, 0.369f, 1.479f);
                        if (wheel.name.Contains("FrontRight")) wheel.localPosition = new Vector3(0.846f, 0.369f, 1.479f);
                        if (wheel.name.Contains("RearLeft")) wheel.localPosition = new Vector3(-0.846f, 0.369f, -1.381f);
                        if (wheel.name.Contains("RearRight")) wheel.localPosition = new Vector3(0.846f, 0.369f, -1.381f);
                    }
                }
            }

            Debug.Log("<color=lime>[Islem Tamam]</color> Arabanin parcalari eski yerine oturtuldu (Materyaller korundu)!");
        }
    }

    [MenuItem("Tools/Pembe Arabayi Duzelt (Fix Pink Car)")]
    public static void FixPinkCar()
    {
        GameObject arabaCube = GameObject.Find("ArabaCube");
        if (arabaCube == null) return;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        // Orijinal dokuları yükle (kırmızı araba için Col3, siyah şeritli olan)
        Texture2D col3Tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ARCADE - FREE Racing Car/Textures/Color Variations/AFRC_Tex_Col3.png");
        Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ARCADE - FREE Racing Car/Textures/AFRC_Tex_Emission.png");

        MeshRenderer[] renderers = arabaCube.GetComponentsInChildren<MeshRenderer>(true);
        int fixedCount = 0;
        foreach (MeshRenderer rend in renderers)
        {
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat != null)
                {
                    Undo.RecordObject(mat, "Fix Pink Material");
                    if (mat.shader.name != "Universal Render Pipeline/Lit")
                    {
                        mat.shader = urpLit;
                        fixedCount++;
                    }

                    // Gövde materyali ise güzel texture'ı atayalım
                    if (mat.name.Contains("Col"))
                    {
                        mat.SetColor("_BaseColor", Color.white); // Düz kırmızılığı kaldır
                        if (col3Tex != null) mat.SetTexture("_BaseMap", col3Tex); // Siyah şeritli kaplamayı uygula
                        mat.SetFloat("_Smoothness", 0.5f);
                    }
                    
                    // Emisyon (farlar, camlar vs)
                    if (mat.name.Contains("Emission"))
                    {
                        mat.SetColor("_BaseColor", Color.white);
                        if (emissionTex != null) mat.SetTexture("_BaseMap", emissionTex);
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.white * 2f);
                        if (emissionTex != null) mat.SetTexture("_EmissionMap", emissionTex);
                    }
                }
            }
        }

        Debug.Log($"<color=cyan>[Islem Tamam]</color> Arabanin orijinal siyah seritli guzel kaplamasi geri getirildi!");
    }
}
