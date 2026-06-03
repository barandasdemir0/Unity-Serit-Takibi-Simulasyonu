using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// İki modlu kamera kontrolcüsü:
///   1. Üçüncü Şahıs (Takip): Aracın arkasından yumuşak takip
///   2. Kuş Bakışı (Tepeden): Tam dikey yukarıdan bakış
/// 
/// Modlar arasında C tuşu veya UI butonu ile geçiş yapılır.
/// Kamera konumları arasındaki geçişler Lerp/Slerp ile yumuşatılır.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Kamera Modları
    // ─────────────────────────────────────────────
    public enum CameraMode
    {
        ThirdPerson,  // Aracın arkasından takip eden kamera
        TopDown       // Yukarıdan kuş bakışı
    }

    [Header("Hedef")]
    [Tooltip("Takip edilecek araç Transform'u")]
    public Transform target;

    [Header("Kamera Modu")]
    public CameraMode currentMode = CameraMode.ThirdPerson;

    // ─────────────────────────────────────────────
    // Üçüncü Şahıs Kamera Ayarları
    // ─────────────────────────────────────────────
    [Header("Üçüncü Şahıs (Takip) Ayarları")]
    [Tooltip("Aracın arkasındaki mesafe")]
    public float chaseDistance = 8f;

    [Tooltip("Araç üzerindeki yükseklik")]
    public float chaseHeight = 4f;

    [Tooltip("Konum takibi için yumuşatma hızı")]
    [Range(1f, 20f)]
    public float positionSmoothSpeed = 5f;

    [Tooltip("Dönüş takibi için yumuşatma hızı")]
    [Range(1f, 20f)]
    public float rotationSmoothSpeed = 8f;

    [Tooltip("İleri bakış mesafesi (kameranın aracın ne kadar önüne bakacağı)")]
    public float lookAheadDistance = 5f;

    // ─────────────────────────────────────────────
    // Kuş Bakışı Kamera Ayarları
    // ─────────────────────────────────────────────
    [Header("Kuş Bakışı (Tepeden) Ayarları")]
    [Tooltip("Kuş bakışı modunda araç üzerindeki yükseklik")]
    public float topDownHeight = 40f;

    [Tooltip("Kuş bakışı kamerasının aracı yatay olarak takip edip etmeyeceği")]
    public bool topDownFollowsTarget = true;

    // ─────────────────────────────────────────────
    // Geçiş Ayarları
    // ─────────────────────────────────────────────
    [Header("Geçiş")]
    [Tooltip("Kamera modları arasındaki yumuşak geçiş hızı")]
    [Range(1f, 10f)]
    public float transitionSpeed = 3f;

    // ─────────────────────────────────────────────
    // İç Durum Değişkenleri
    // ─────────────────────────────────────────────
    private Vector3 _currentVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        // C tuşuna basıldığında kamera modunu değiştir
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleCameraMode();
        }

        // Mevcut moda göre kamerayı güncelle
        switch (currentMode)
        {
            case CameraMode.ThirdPerson:
                UpdateThirdPerson();
                break;
            case CameraMode.TopDown:
                UpdateTopDown();
                break;
        }
    }

    /// <summary>
    /// Üçüncü Şahıs ve Kuş Bakışı kamera modları arasında geçiş yapar.
    /// </summary>
    public void ToggleCameraMode()
    {
        currentMode = currentMode == CameraMode.ThirdPerson 
            ? CameraMode.TopDown 
            : CameraMode.ThirdPerson;
    }

    /// <summary>
    /// Üçüncü şahıs takip kamerasını günceller.
    /// Aracın arkasından yumuşakça takip eder, aracın önündeki bir noktaya bakar.
    /// </summary>
    private void UpdateThirdPerson()
    {
        // İstenen konum: aracın arkasında ve yukarısında
        Vector3 desiredPosition = target.position 
            - target.forward * chaseDistance 
            + Vector3.up * chaseHeight;

        // Konum geçişini yumuşat
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref _currentVelocity, 
            1f / positionSmoothSpeed
        );

        // Aracın biraz önündeki bir noktaya bak
        Vector3 lookTarget = target.position + target.forward * lookAheadDistance;

        // Dönüş geçişini yumuşat
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            desiredRotation, 
            Time.deltaTime * rotationSmoothSpeed
        );
    }

    /// <summary>
    /// Kuş bakışı kamerasını günceller.
    /// Araç üzerinde sabit bir yükseklikten doğrudan aşağıya bakar.
    /// </summary>
    private void UpdateTopDown()
    {
        Vector3 desiredPosition;

        if (topDownFollowsTarget)
        {
            // Aracı yatay olarak takip et, sabit yükseklikte kal
            desiredPosition = new Vector3(
                target.position.x,
                topDownHeight,
                target.position.z
            );
        }
        else
        {
            // Pist merkezinin üzerinde sabit konum
            desiredPosition = new Vector3(20f, topDownHeight, 10f);
        }

        // Yumuşak geçiş
        transform.position = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            Time.deltaTime * transitionSpeed
        );

        // Doğrudan aşağı bak
        Quaternion desiredRotation = Quaternion.Euler(90f, 0f, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            desiredRotation, 
            Time.deltaTime * transitionSpeed
        );
    }
}
