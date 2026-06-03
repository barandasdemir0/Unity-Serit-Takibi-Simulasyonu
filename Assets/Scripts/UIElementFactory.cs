using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Arayüz elemanlarını (Label, Slider, InputField, RectTransform) 
/// standart bir formatta üreten Fabrika (Factory) sınıfı.
/// BAĞLANTI: Diğer tüm UIManager sınıfları (Örn: VehicleSpeedUIManager) bu fabrikayı çağırır.
/// Amacı kod tekrarını ve kalabalığını (Boilerplate) %90 oranında azaltmaktır.
/// </summary>
public static class UIElementFactory
{
    /// <summary>
    /// Görünmez, sadece konum belirten (Div benzeri) bir RectTransform kutusu oluşturur.
    /// </summary>
    public static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name).AddComponent<RectTransform>();
        rt.transform.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    /// <summary>
    /// Ekrana statik bir yazı (Metin / Label) ekler.
    /// </summary>
    public static Text MakeLabel(Transform parent, string name, string txt, Vector2 pos, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var t = new GameObject(name).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        
        // Unity'nin standart Arial benzeri yerleşik fontunu kullan
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = txt; t.color = color; t.fontSize = fontSize; t.fontStyle = style; t.alignment = anchor;
        
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return t;
    }

    /// <summary>
    /// Kullanıcının klavyeden sayı girebileceği siyah arkaplanlı bir kutu (InputField) oluşturur.
    /// </summary>
    public static InputField MakeInputField(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        // Kutunun arka plan görselini oluştur
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Kutunun içine yazılacak olan yazının görselini oluştur
        var textGo = new GameObject("Text"); textGo.transform.SetParent(go.transform, false);
        var txt = textGo.AddComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white; txt.fontSize = 15; txt.alignment = TextAnchor.MiddleCenter;
        
        var txtRt = textGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; 
        txtRt.offsetMin = new Vector2(2, 0); txtRt.offsetMax = new Vector2(-2, 0);

        // Sisteme "Bu kutu klavye girdisi alacak (InputField) ve sadece ondalıklı sayı (Decimal) kabul edecek" de.
        var input = go.AddComponent<InputField>(); 
        input.textComponent = txt; 
        input.characterValidation = InputField.CharacterValidation.Decimal;
        return input;
    }

    /// <summary>
    /// Kullanıcının fareyle sağa-sola kaydırabileceği değer çubuğu (Slider) oluşturur.
    /// </summary>
    public static Slider MakeSliderAt(Transform parent, string name, Vector2 pos, Vector2 size, float min, float max, bool isPID = false)
    {
        // Slider ana objesi
        var slGo = new GameObject(name); slGo.transform.SetParent(parent, false);
        var sl = slGo.AddComponent<Slider>();
        
        var srt = slGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(0, 1); srt.pivot = new Vector2(0, 1);
        srt.anchoredPosition = pos; srt.sizeDelta = size;

        // Çubuğun gri arka planı (Bg)
        var bgObj = new GameObject("Bg"); bgObj.transform.SetParent(slGo.transform, false);
        var bgImg = bgObj.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        var bgRt = bgObj.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;

        // Dolan kısmın (Fill) alanı
        var fa = new GameObject("FillArea"); fa.transform.SetParent(slGo.transform, false);
        var faRt = fa.AddComponent<RectTransform>(); faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f); faRt.sizeDelta = Vector2.zero;

        // Dolan kısmın rengi (PID ise mavi, normal ayarsa turuncu)
        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = isPID ? new Color(0.2f, 0.75f, 1f) : new Color(1f, 0.6f, 0.15f);
        var fillRt = fill.GetComponent<RectTransform>(); fillRt.sizeDelta = Vector2.zero;

        // Kaydırma topuzunun (Handle) alanı
        var ha = new GameObject("HandleArea"); ha.transform.SetParent(slGo.transform, false);
        var haRt = ha.AddComponent<RectTransform>(); haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.sizeDelta = Vector2.zero;

        // Kaydırma topuzunun rengi (Beyaz)
        var handle = new GameObject("Handle"); handle.transform.SetParent(ha.transform, false);
        var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>(); handleRt.sizeDelta = new Vector2(18, 0);

        // Oluşturulan parçaları Slider'ın kendisine ata ve sınırları ayarla (min - max)
        sl.targetGraphic = handleImg; sl.fillRect = fillRt; sl.handleRect = handleRt; 
        sl.minValue = min; sl.maxValue = max;
        
        return sl;
    }
}
