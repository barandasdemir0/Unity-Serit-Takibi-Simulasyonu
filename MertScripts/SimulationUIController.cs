using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main UI controller that binds the UI Toolkit interface to the simulation.
/// Manages:
///   - PID parameter sliders (Kp, Ki, Kd)
///   - Vehicle speed control
///   - Controller mode selection (P / PI / PID)
///   - Camera mode toggle
///   - Real-time diagnostic displays
///   - Graph texture display
/// 
/// Uses UI Toolkit (UXML/USS) for a modern, glassmorphism-themed interface.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SimulationUIController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // References
    // ─────────────────────────────────────────────
    [Header("References")]
    public VehicleController vehicleController;
    public CameraController cameraController;
    public GraphRenderer graphRenderer;
    public CSVExporter csvExporter;

    // ─────────────────────────────────────────────
    // UI Elements (cached)
    // ─────────────────────────────────────────────
    private Slider _kpSlider;
    private Slider _kiSlider;
    private Slider _kdSlider;
    private Slider _speedSlider;
    private Slider _massSlider;
    private Slider _offsetSlider;

    // Varsayılan (başlangıç) PID değerleri — Reset'te geri dönülecek
    private float _defaultKp;
    private float _defaultKi;
    private float _defaultKd;

    private Label _kpValueLabel;
    private Label _kiValueLabel;
    private Label _kdValueLabel;
    private Label _speedValueLabel;
    private Label _massValueLabel;
    private Label _offsetValueLabel;

    private Label _errorLabel;
    private Label _controlLabel;
    private Label _steeringLabel;
    private Label _modeLabel;

    private DropdownField _modeDropdown;
    private Button _cameraButton;
    private Button _resetButton;
    private Button _exportButton;
    private Button _toggleGraphsButton;

    // Graph display elements
    private VisualElement _controlGraphImage;
    private VisualElement _errorGraphImage;
    private VisualElement _pathGraphImage;
    private VisualElement _pGraphImage;
    private VisualElement _iGraphImage;
    private VisualElement _dGraphImage;

    // Graph value labels (now only)
    private Label _ctrlCurLabel;
    private Label _errCurLabel;
    private Label _pathCurLabel;
    private Label _pCurLabel;
    private Label _iCurLabel;
    private Label _dCurLabel;

    // Graph hover tooltip
    private VisualElement _graphTooltip;
    private Label _tooltipTimeLabel;
    private Label _tooltipValueLabel;

    // Tab elements
    private Button _tabGeneral;
    private Button _tabPid;
    private VisualElement _contentGeneral;
    private VisualElement _contentPid;

    private UIDocument _uiDocument;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;

        var root = _uiDocument.rootVisualElement;

        // ── Bind PID Sliders ──────────────────────
        _kpSlider    = root.Q<Slider>("kp-slider");
        _kiSlider    = root.Q<Slider>("ki-slider");
        _kdSlider    = root.Q<Slider>("kd-slider");
        _speedSlider = root.Q<Slider>("speed-slider");
        _massSlider  = root.Q<Slider>("mass-slider");
        _offsetSlider = root.Q<Slider>("offset-slider");

        _kpValueLabel    = root.Q<Label>("kp-value");
        _kiValueLabel    = root.Q<Label>("ki-value");
        _kdValueLabel    = root.Q<Label>("kd-value");
        _speedValueLabel = root.Q<Label>("speed-value");
        _massValueLabel  = root.Q<Label>("mass-value");
        _offsetValueLabel = root.Q<Label>("offset-value");

        // ── Bind Diagnostic Labels ────────────────
        _errorLabel = root.Q<Label>("error-display");
        _controlLabel = root.Q<Label>("control-display");
        _steeringLabel = root.Q<Label>("steering-display");
        _modeLabel = root.Q<Label>("mode-display");

        // ── Bind Controls ─────────────────────────
        _modeDropdown = root.Q<DropdownField>("mode-dropdown");
        _cameraButton = root.Q<Button>("camera-button");
        _resetButton = root.Q<Button>("reset-button");
        _exportButton = root.Q<Button>("export-button");
        _toggleGraphsButton = root.Q<Button>("toggle-graphs-button");

        // ── Bind Graph Areas ─────────────────────
        _controlGraphImage = root.Q<VisualElement>("control-graph");
        _errorGraphImage   = root.Q<VisualElement>("error-graph");
        _pathGraphImage    = root.Q<VisualElement>("path-graph");   // r(t) vs y(t)
        _pGraphImage       = root.Q<VisualElement>("p-graph");
        _iGraphImage       = root.Q<VisualElement>("i-graph");
        _dGraphImage       = root.Q<VisualElement>("d-graph");

        // ── Bind Graph Value Labels ───────────────
        _ctrlCurLabel  = root.Q<Label>("ctrl-cur-label");
        _errCurLabel   = root.Q<Label>("err-cur-label");
        _pathCurLabel  = root.Q<Label>("path-cur-label");
        _pCurLabel     = root.Q<Label>("p-cur-label");
        _iCurLabel     = root.Q<Label>("i-cur-label");
        _dCurLabel     = root.Q<Label>("d-cur-label");

        // ── Bind Tooltip ──────────────────────────
        _graphTooltip = root.Q<VisualElement>("graph-tooltip");
        _tooltipTimeLabel = root.Q<Label>("tooltip-time");
        _tooltipValueLabel = root.Q<Label>("tooltip-value");

        RegisterGraphHover(_controlGraphImage, d => d.controlOutput, "u(t)");
        RegisterGraphHover(_errorGraphImage, d => d.lateralError, "e(t)", " m");
        RegisterGraphHover(_pathGraphImage, d => d.vehiclePosX, "y(t)", " m");
        RegisterGraphHover(_pGraphImage, d => d.pTerm, "P");
        RegisterGraphHover(_iGraphImage, d => d.iTerm, "I");
        RegisterGraphHover(_dGraphImage, d => d.dTerm, "D");

        // ── Bind Tabs ─────────────────────────────
        _tabGeneral = root.Q<Button>("tab-general");
        _tabPid = root.Q<Button>("tab-pid");
        _contentGeneral = root.Q<VisualElement>("content-general");
        _contentPid = root.Q<VisualElement>("content-pid");

        if (_tabGeneral != null && _tabPid != null)
        {
            _tabGeneral.clicked += () => SwitchTab(true);
            _tabPid.clicked += () => SwitchTab(false);
        }

        // ── Initialize Values ─────────────────────
        if (vehicleController != null)
        {
            // Başlangıç PID değerlerini kaydet (Reset için)
            _defaultKp = vehicleController.pidController.Kp;
            _defaultKi = vehicleController.pidController.Ki;
            _defaultKd = vehicleController.pidController.Kd;

            if (_kpSlider != null)
            {
                _kpSlider.value = vehicleController.pidController.Kp;
                _kpSlider.RegisterValueChangedCallback(evt =>
                {
                    vehicleController.pidController.Kp = evt.newValue;
                    if (_kpValueLabel != null) _kpValueLabel.text = evt.newValue.ToString("F2");
                });
            }

            if (_kiSlider != null)
            {
                _kiSlider.value = vehicleController.pidController.Ki;
                _kiSlider.RegisterValueChangedCallback(evt =>
                {
                    vehicleController.pidController.Ki = evt.newValue;
                    if (_kiValueLabel != null) _kiValueLabel.text = evt.newValue.ToString("F2");
                });
            }

            if (_kdSlider != null)
            {
                _kdSlider.value = vehicleController.pidController.Kd;
                _kdSlider.RegisterValueChangedCallback(evt =>
                {
                    vehicleController.pidController.Kd = evt.newValue;
                    if (_kdValueLabel != null) _kdValueLabel.text = evt.newValue.ToString("F2");
                });
            }

            if (_speedSlider != null)
            {
                _speedSlider.value = vehicleController.vehicleSpeed;
                _speedSlider.RegisterValueChangedCallback(evt =>
                {
                    vehicleController.vehicleSpeed = evt.newValue;
                    if (_speedValueLabel != null) _speedValueLabel.text = evt.newValue.ToString("F1") + " m/s";
                });
            }

            // ── Kütle Slider ───────────────────────
            if (_massSlider != null)
            {
                _massSlider.value = vehicleController.vehicleMass;
                _massSlider.RegisterValueChangedCallback(evt =>
                {
                    vehicleController.vehicleMass = evt.newValue;
                    if (_massValueLabel != null)
                        _massValueLabel.text = evt.newValue.ToString("F0") + " kg";
                });
            }

            // ── Başlangıç Yanal Offset Slider ──────────────
            if (_offsetSlider != null)
            {
                _offsetSlider.value = vehicleController.initialLateralOffset;
                _offsetSlider.RegisterValueChangedCallback(evt =>
                {
                    vehicleController.initialLateralOffset = evt.newValue;
                    if (_offsetValueLabel != null)
                        _offsetValueLabel.text = evt.newValue.ToString("+0.00;-0.00;0.00") + " m";
                });
            }
        }

        // ── Mode Dropdown ─────────────────────────
        if (_modeDropdown != null)
        {
            _modeDropdown.choices = new System.Collections.Generic.List<string>
            {
                "Sadece P", "PI", "PID"
            };
            _modeDropdown.index = 2; // Varsayılan: PID
            _modeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (vehicleController == null) return;
                switch (evt.newValue)
                {
                    case "Sadece P":
                        vehicleController.pidController.mode = PIDController.ControllerMode.P_Only;
                        break;
                    case "PI":
                        vehicleController.pidController.mode = PIDController.ControllerMode.PI_Only;
                        break;
                    case "PID":
                        vehicleController.pidController.mode = PIDController.ControllerMode.PID;
                        break;
                }
                vehicleController.pidController.Reset();
            });
        }

        // ── Camera Toggle Button ──────────────────
        if (_cameraButton != null)
        {
            _cameraButton.clicked += () =>
            {
                if (cameraController != null)
                    cameraController.ToggleCameraMode();
            };
        }

        // ── Reset Button ──────────────────────────
        if (_resetButton != null)
        {
            _resetButton.clicked += () =>
            {
                if (vehicleController != null)
                    vehicleController.ResetSimulation();
                // Kp, Ki, Kd başlangıç değerlerine döndür
                ResetPIDSliders();
            };
        }

        // ── Export Button ─────────────────────────
        if (_exportButton != null)
        {
            _exportButton.clicked += () =>
            {
                if (csvExporter != null)
                    csvExporter.ExportData();
            };
        }

        // ── Toggle Graphs Button ──────────────────
        if (_toggleGraphsButton != null)
        {
            _toggleGraphsButton.clicked += () =>
            {
                var rightPanel = root.Q<VisualElement>("right-panel");
                if (rightPanel != null)
                {
                    bool isVisible = rightPanel.style.display != DisplayStyle.None;
                    rightPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
                }
            };
        }
    }

    private void SwitchTab(bool showGeneral)
    {
        if (_contentGeneral == null || _contentPid == null) return;

        if (showGeneral)
        {
            _contentGeneral.style.display = DisplayStyle.Flex;
            _contentPid.style.display = DisplayStyle.None;
            _tabGeneral.AddToClassList("tab-active");
            _tabPid.RemoveFromClassList("tab-active");
        }
        else
        {
            _contentGeneral.style.display = DisplayStyle.None;
            _contentPid.style.display = DisplayStyle.Flex;
            _tabGeneral.RemoveFromClassList("tab-active");
            _tabPid.AddToClassList("tab-active");
        }
    }

    private void Update()
    {
        if (vehicleController == null) return;

        // ── Update Diagnostic Labels ──────────────
        if (_errorLabel != null)
            _errorLabel.text = $"e(t): {vehicleController.CurrentLateralError:F3} m";

        if (_controlLabel != null)
            _controlLabel.text = $"u(t): {vehicleController.CurrentControlOutput:F3}";

        if (_steeringLabel != null)
            _steeringLabel.text = $"δ: {vehicleController.CurrentSteeringAngle:F1}°";

        if (_modeLabel != null)
            _modeLabel.text = $"Mod: {vehicleController.pidController.mode}";

        // ── Update Graph Value Labels ─────────────
        UpdateGraphValueLabels();

        // ── Update Graph Textures ─────────────────
        if (graphRenderer != null)
        {
            if (_controlGraphImage != null && graphRenderer.ControlOutputTexture != null)
                _controlGraphImage.style.backgroundImage = new StyleBackground(graphRenderer.ControlOutputTexture);

            if (_errorGraphImage != null && graphRenderer.LateralErrorTexture != null)
                _errorGraphImage.style.backgroundImage = new StyleBackground(graphRenderer.LateralErrorTexture);

            // r(t) vs y(t) — Referans / Gerçek Yol Karşılaştırması
            if (_pathGraphImage != null && graphRenderer.PathComparisonTexture != null)
                _pathGraphImage.style.backgroundImage = new StyleBackground(graphRenderer.PathComparisonTexture);

            if (_pGraphImage != null && graphRenderer.PGraphTexture != null)
                _pGraphImage.style.backgroundImage = new StyleBackground(graphRenderer.PGraphTexture);

            if (_iGraphImage != null && graphRenderer.IGraphTexture != null)
                _iGraphImage.style.backgroundImage = new StyleBackground(graphRenderer.IGraphTexture);

            if (_dGraphImage != null && graphRenderer.DGraphTexture != null)
                _dGraphImage.style.backgroundImage = new StyleBackground(graphRenderer.DGraphTexture);
        }
    }

    // ─────────────────────────────────────────────
    // Graph Tooltip Helpers
    // ─────────────────────────────────────────────

    private void RegisterGraphHover(VisualElement graphArea, System.Func<VehicleController.FrameData, float> valueSelector, string prefix, string suffix = "")
    {
        if (graphArea == null) return;

        graphArea.RegisterCallback<PointerEnterEvent>(evt =>
        {
            if (_graphTooltip != null) _graphTooltip.style.display = DisplayStyle.Flex;
        });

        graphArea.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            if (_graphTooltip != null) _graphTooltip.style.display = DisplayStyle.None;
        });

        graphArea.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (_graphTooltip == null || vehicleController == null || graphRenderer == null) return;
            var data = vehicleController.RecordedData;
            if (data == null || data.Count == 0) return;

            // Farenin X pozisyonunun [0, 1] arası normalize değeri
            float normalizedX = evt.localPosition.x / graphArea.resolvedStyle.width;
            normalizedX = Mathf.Clamp01(normalizedX);

            // Grafikte kaç veri noktası gösteriliyor?
            int visiblePoints = Mathf.Min(data.Count, graphRenderer.visibleDataPoints);
            int startIndex = data.Count - visiblePoints;

            // Farenin denk geldiği veri noktası
            int localIndex = Mathf.RoundToInt(normalizedX * (visiblePoints - 1));
            int dataIndex = Mathf.Clamp(startIndex + localIndex, 0, data.Count - 1);

            var pointData = data[dataIndex];
            float val = valueSelector(pointData);

            if (_tooltipTimeLabel != null)
                _tooltipTimeLabel.text = $"t: {pointData.time:F1}s";
            
            if (_tooltipValueLabel != null)
                _tooltipValueLabel.text = $"{prefix}: {val:+0.000;-0.000}{suffix}";

            // Tooltip'i fare imlecinin biraz üstüne/sağına al
            _graphTooltip.style.left = evt.position.x + 15f;
            _graphTooltip.style.top = evt.position.y - 30f;
        });
    }

    /// <summary>
    /// Kp, Ki, Kd slider'larını başlangıç değerlerine döndürür.
    /// VehicleController'daki PID değerlerini de senkronize eder.
    /// </summary>
    private void ResetPIDSliders()
    {
        if (vehicleController == null) return;

        // PID değerlerini sıfırla
        vehicleController.pidController.Kp = _defaultKp;
        vehicleController.pidController.Ki = _defaultKi;
        vehicleController.pidController.Kd = _defaultKd;

        // Slider'ları güncelle (UI ile kod senkronizasyonu)
        if (_kpSlider != null)
        {
            _kpSlider.SetValueWithoutNotify(_defaultKp);
            if (_kpValueLabel != null) _kpValueLabel.text = _defaultKp.ToString("F2");
        }
        if (_kiSlider != null)
        {
            _kiSlider.SetValueWithoutNotify(_defaultKi);
            if (_kiValueLabel != null) _kiValueLabel.text = _defaultKi.ToString("F2");
        }
        if (_kdSlider != null)
        {
            _kdSlider.SetValueWithoutNotify(_defaultKd);
            if (_kdValueLabel != null) _kdValueLabel.text = _defaultKd.ToString("F2");
        }
    }

    /// <summary>Null-safe label güncelleme.</summary>
    private static void SetLabel(Label lbl, string text)
    {
        if (lbl != null) lbl.text = text;
    }

    /// <summary>
    /// Her frame anlık değerleri etiketlere yazar.
    /// </summary>
    private void UpdateGraphValueLabels()
    {
        if (vehicleController == null) return;
        var data = vehicleController.RecordedData;
        if (data.Count == 0) return;

        var latest = data[data.Count - 1];

        // ── Control Output u(t) ────────────────────
        SetLabel(_ctrlCurLabel, $"şim:{latest.controlOutput:+0.000;-0.000}");

        // ── Yanal Hata e(t) ───────────────────────
        SetLabel(_errCurLabel, $"şim:{latest.lateralError:+0.000;-0.000} m");

        // ── r(t) / y(t) Referans & Gerçek Yol ────
        SetLabel(_pathCurLabel, $"şim:{latest.vehiclePosX:+0.000;-0.000} m");

        // ── PID Oransal P ──────────────────────────
        SetLabel(_pCurLabel, $"şim:{latest.pTerm:+0.000;-0.000}");

        // ── PID İntegral I ─────────────────────────
        SetLabel(_iCurLabel, $"şim:{latest.iTerm:+0.000;-0.000}");

        // ── PID Türevsel D ─────────────────────────
        SetLabel(_dCurLabel, $"şim:{latest.dTerm:+0.000;-0.000}");
    }
}

