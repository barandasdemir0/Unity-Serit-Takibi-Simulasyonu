using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Araç Kütlesi (VehicleMass) arayüz ayarlarını yöneten modül.
/// BAĞLANTI: SimulationUIController tarafından yaratılır.
/// Değişiklikleri anında CarLaneTracker.cs'teki VehicleMass değişkenine aktarır.
/// </summary>
public class VehicleMassUIManager
{
    // Hangi aracı güncelleyeceğimizi tutan referans
    private CarLaneTracker _tracker;
    
    // UI Elemanları
    private Slider _slMass;
    private InputField _inMass;
    private Text _massLabel;

    /// <summary>
    /// Kütle arayüzünü (Başlık, Input, Slider ve Bilgi Metni) oluşturur.
    /// </summary>
    public VehicleMassUIManager(Transform parent, Vector2 posBase, CarLaneTracker tracker, SimulationUIController mainUI)
    {
        _tracker = tracker;

        // BAĞLANTI: Arayüz üretim işlerini UIElementFactory'ye yollayan mainUI metodlarını kullanırız.
        
        // 1. Ana Başlık ("Araç Dinamikleri")
        mainUI.MakeLabel(parent, "lblDynamics", "─── Araç Dinamikleri ───", posBase, new Vector2(360, 26), 15, new Color(0.6f, 0.8f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter);
        
        // 2. Kütle (Mass) Etiketi
        mainUI.MakeLabel(parent, "Mass_lbl", "Kütle:", posBase + new Vector2(2, -28), new Vector2(50, 32), 15, Color.white);
        
        // 3. Elle değer girmek için kutucuk (InputField)
        _inMass = mainUI.MakeInputField(parent, "Mass_input", posBase + new Vector2(52, -24), new Vector2(60, 26));
        
        // 4. Fareyle kaydırmak için çubuk (Slider) [500kg ile 5000kg arası]
        _slMass = mainUI.MakeSliderAt(parent, "Mass_sl", posBase + new Vector2(122, -24), new Vector2(224, 26), 500f, 5000f, false);
        
        // 5. Atalet (Inertia) bilgisini gösteren alt metin
        _massLabel = mainUI.MakeLabel(parent, "massInfo", "Kütle: 1500 kg | Atalet: 0.78x", posBase + new Vector2(0, -56), new Vector2(360, 22), 14, new Color(0.6f, 0.85f, 1f));

        // UI etkileşimlerini bağla
        BindEvents();
    }

    /// <summary>
    /// Slider kaydırıldığında veya kutuya değer girildiğinde ne olacağını ayarlar (Event Binding).
    /// </summary>
    private void BindEvents()
    {
        if (_tracker == null) return;
        
        // İlk değerleri araçtan okuyup UI'a yazdır
        _slMass.value = _tracker.VehicleMass;
        _inMass.text = _tracker.VehicleMass.ToString("F0");
        
        // Slider değiştiğinde:
        _slMass.onValueChanged.AddListener(v => { 
            _tracker.VehicleMass = v; // Araba kütlesini güncelle
            if (!_inMass.isFocused) _inMass.text = v.ToString("F0"); // Input kutusunu da güncelle
            UpdateLabel(); // Atalet metnini güncelle
        });
        
        // Input Kutusuna yazı yazılıp Enter'a basıldığında:
        _inMass.onEndEdit.AddListener(s => { 
            // Eğer yazılan şey rakamsa (float)
            if (float.TryParse(s, out float val)) { 
                val = Mathf.Clamp(val, 500f, 5000f); // 500 - 5000 arasına sınırla
                _slMass.value = val; // Slider'ı bu değere eşitle, gerisini o halleder
            } 
        });

        UpdateLabel();
    }

    /// <summary>
    /// Kütleye bağlı olarak "Atalet (Inertia)" hesaplamasını yapar ve ekrana yazdırır.
    /// BAĞLANTI: SimulationUIController Update içinden çağrılır.
    /// </summary>
    public void UpdateLabel()
    {
        if (_tracker == null) return;
        
        // Kütle 500 ise atalet 1.0 (Çok çevik), 5000 ise atalet 0.1 (Çok hantal) olur.
        float inertia = Mathf.Lerp(1.0f, 0.1f, (_tracker.VehicleMass - 500f) / 4500f);
        
        // Metni güncelle
        _massLabel.text = $"Kütle: {_tracker.VehicleMass:F0} kg | Atalet: {inertia:F2}x";
    }
}
