using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// İstenen Hız (DesiredSpeed) arayüz ayarlarını yöneten modül.
/// BAĞLANTI: SimulationUIController tarafından yaratılır.
/// Değişiklikleri anında CarLaneTracker.cs'teki DesiredSpeed değişkenine aktarır.
/// </summary>
public class VehicleSpeedUIManager
{
    private CarLaneTracker _tracker;
    private Slider _slSpeed;
    private InputField _inSpeed;

    /// <summary>
    /// Hız arayüzünü (Başlık, Input ve Slider) oluşturur.
    /// </summary>
    public VehicleSpeedUIManager(Transform parent, Vector2 posBase, CarLaneTracker tracker, SimulationUIController mainUI)
    {
        _tracker = tracker;

        // BAĞLANTI: UIElementFactory üzerinden elemanları üret (mainUI wrapper)
        mainUI.MakeLabel(parent, "lblSpeed", "─── Araç Hızı ───", posBase, new Vector2(360, 26), 15, new Color(1f, 0.7f, 0.3f), FontStyle.Bold, TextAnchor.MiddleCenter);
        mainUI.MakeLabel(parent, "Speed_lbl", "Hız:", posBase + new Vector2(2, -28), new Vector2(40, 32), 15, Color.white);
        
        // Hız girmek için kutu
        _inSpeed = mainUI.MakeInputField(parent, "Speed_input", posBase + new Vector2(42, -24), new Vector2(60, 26));
        
        // Hız kaydırma çubuğu (1 ile 30 m/s arasında)
        _slSpeed = mainUI.MakeSliderAt(parent, "Speed_sl", posBase + new Vector2(112, -24), new Vector2(234, 26), 1f, 30f, false);

        BindEvents();
    }

    /// <summary>
    /// Slider ve InputField etkileşimlerini araca (CarLaneTracker) bağlar.
    /// </summary>
    private void BindEvents()
    {
        if (_tracker == null) return;
        
        // UI elemanlarına arabadaki anlık hızı (DesiredSpeed) yaz
        _slSpeed.value = _tracker.DesiredSpeed;
        _inSpeed.text = _tracker.DesiredSpeed.ToString("F1");
        
        // Slider hareket ettiğinde:
        _slSpeed.onValueChanged.AddListener(v => { 
            _tracker.DesiredSpeed = v; // Araba hızını güncelle
            if (!_inSpeed.isFocused) _inSpeed.text = v.ToString("F1"); // Kutudaki yazıyı güncelle
        });
        
        // Kutuya klavyeden değer girilip onaylandığında:
        _inSpeed.onEndEdit.AddListener(s => { 
            if (float.TryParse(s, out float val)) { 
                val = Mathf.Clamp(val, 1f, 30f); // 1-30 arasına sınırla
                _slSpeed.value = val; // Slider'ı eşitle
            } 
        });
    }
}
