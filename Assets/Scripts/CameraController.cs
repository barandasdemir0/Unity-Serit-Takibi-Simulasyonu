using UnityEngine;

/// <summary>
/// Kameranın davranışlarını yöneten kontrolcü modül.
/// 3. Şahıs (Araba arkası) ve Kuş Bakışı (Top-Down) modları arasında geçişi ve kamera takibini sağlar.
/// </summary>
public class CameraController : MonoBehaviour
{
    // Kamera Modları Listesi
    public enum CameraMode { ThirdPerson, TopDown }

    [Header("Referanslar")]
    // Takip edilecek hedef araç
    public Transform target;
    // Mevcut aktif kamera modu
    public CameraMode currentMode = CameraMode.ThirdPerson;

    [Header("Üçüncü Şahıs Ayarları")]
    public float chaseDistance = 8f;         // Aracın ne kadar arkasından takip edeceği
    public float chaseHeight = 4f;           // Kameranın araçtan yüksekliği
    public float positionSmoothSpeed = 5f;   // Kameranın hareket yumuşaklığı (Düşük = daha pürüzsüz)
    public float rotationSmoothSpeed = 8f;   // Kameranın dönüş yumuşaklığı
    public float lookAheadDistance = 5f;     // Kameranın aracın tam üstüne değil, azıcık ilerisine bakması için

    [Header("Kuş Bakışı Ayarları")]
    public float topDownHeight = 40f;        // Kuş bakışında kameranın yerden yüksekliği
    public bool topDownFollowsTarget = true; // Kuş bakışında kamera aracı takip etsin mi?
    public float transitionSpeed = 3f;       // Kuş bakışından 3. Şahısa geçerkenki animasyon geçiş hızı

    // Yumuşak geçiş (SmoothDamp) fonksiyonu için anlık hızı tutan sistem değişkeni
    private Vector3 _currentVelocity;

    /// <summary>
    /// Tüm objelerin fizik ve normal güncellemeleri (Update) bittikten sonra çalışır.
    /// Kamera takibinde titreme olmaması için daima LateUpdate kullanılır.
    /// </summary>
    void LateUpdate()
    {
        // Hedef yoksa hiçbir şey yapma
        if (target == null) return;

        // Klavyeden 'C' tuşuna basıldığında kamera modunu (ThirdPerson <-> TopDown) değiştir
        if (Input.GetKeyDown(KeyCode.C))
        {
            currentMode = currentMode == CameraMode.ThirdPerson ? CameraMode.TopDown : CameraMode.ThirdPerson;
        }

        // Seçili moda göre ilgili güncelleme metodunu çağır
        if (currentMode == CameraMode.ThirdPerson) 
            UpdateThirdPerson();
        else 
            UpdateTopDown();
    }

    /// <summary>
    /// 3. Şahıs (Araba arkası) kamerasının fiziksel konumunu ve yönünü hesaplar.
    /// </summary>
    private void UpdateThirdPerson()
    {
        // 1. POZİSYON HESABI
        // İstenen Kamera Pozisyonu = Arabanın Pozisyonu - (Arabanın İleri Yönü * Mesafe) + (Yukarı Yön * Yükseklik)
        Vector3 targetPosition = target.position - (target.forward * chaseDistance) + (Vector3.up * chaseHeight);
        
        // Mevcut pozisyondan istenen pozisyona yumuşak bir şekilde (SmoothDamp) geçiş yap
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, 1f / positionSmoothSpeed);

        // 2. DÖNÜŞ (ROTASYON) HESABI
        // Kameranın tam olarak nereye bakması gerektiğini bul (Arabanın pozisyonu + biraz ilerisi)
        Vector3 lookAtTarget = target.position + (target.forward * lookAheadDistance);
        
        // Bakılacak noktaya doğru bir vektör oluştur
        Vector3 lookDirection = (lookAtTarget - transform.position).normalized;
        
        // Vektörü Quaternion rotasyonuna çevir
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        
        // Mevcut rotasyondan hedeflenen rotasyona yumuşak bir şekilde (Slerp) geç
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    /// <summary>
    /// Kuş bakışı (Top-Down) kamerasının fiziksel konumunu ve yönünü hesaplar.
    /// </summary>
    private void UpdateTopDown()
    {
        // 1. POZİSYON HESABI
        // Eğer takip aktifse hedefin X/Z koordinatlarını al, değilse kameranın kendi X/Z'sini tut
        float tx = topDownFollowsTarget ? target.position.x : transform.position.x;
        float tz = topDownFollowsTarget ? target.position.z : transform.position.z;

        // Hedef pozisyon = Arabanın tam üstü + Kuş bakışı yüksekliği
        Vector3 targetPosition = new Vector3(tx, topDownHeight, tz);
        
        // Lerp kullanarak yumuşakça tepeye çık/in
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * transitionSpeed);

        // 2. DÖNÜŞ (ROTASYON) HESABI
        // Kuş bakışı modu için kamerayı tam 90 derece aşağı baktır (Euler x:90)
        Quaternion targetRotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Hedef rotasyona yumuşakça (Slerp) geç
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }
}
