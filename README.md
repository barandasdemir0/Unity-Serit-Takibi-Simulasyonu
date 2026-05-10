# Otonom Araç Şerit Takip Simülasyonu (Autonomous Vehicle Lane Tracking Simulation)

Bu proje, Otomatik Kontrol (Automatic Control) dersi final ödevi kapsamında geliştirilmiş, Unity tabanlı bir otonom araç şerit takip simülasyonudur. Projenin temel amacı, bir aracın referans yörüngeyi (spline) takip etmesini sağlamak için PID (Proportional-Integral-Derivative) kontrol algoritmasının tasarımını ve uygulanmasını göstermektir.

## Özellikler

- **PID Kontrolü:** Aracın şeritte kalmasını ve direksiyon/dönüş açısını hesaplamak için optimize edilmiş PID algoritması.
- **Dinamik Şerit Takibi:** Unity'nin Spline alt yapısı kullanılarak oluşturulmuş referans yörüngesinin izlenmesi.
- **Gerçek Zamanlı Veri Görselleştirme:** PID kontrolörünün referans ve gerçekleşen yörünge değerlerinin (y(t)) UI üzerinde gerçek zamanlı olarak grafik ile çizdirilmesi.
- **Veri Loglama (Data Logging):** Aracın hareketi sırasında elde edilen PID değerlerinin ve sapmaların analiz için kaydedilmesi (Loglama sistemi).
- **Özel Editör Araçları (Custom Editor Tools):** Araç modelini düzeltmek ve yol objelerini düzenlemek için oluşturulmuş özel Unity editör eklentileri.

## Proje Yapısı

Önemli script'ler `Assets/Scripts/` klasörü altında bulunmaktadır:

- `CarLaneTracker.cs`: Aracın spline (yol/şerit) üzerindeki hareketini, hızını ve hedef yönelimini yöneten ana script.
- `PIDController.cs`: Verilen referans değere ulaşmak için hata (error) hesaplaması yaparak gerekli yönlendirme/kontrol sinyalini üreten kontrolcü sınıfı.
- `DataLogger.cs`: Simülasyon sırasında oluşan verilerin performans analizi veya raporlama amacıyla dışarıya aktarılmasını (loglanmasını) sağlar.
- `UIGraphManager.cs`: Aracın PID performansını ve hatalarını anlık olarak kullanıcıya gösteren grafik arayüzünü (UI Graph) yönetir.
- `Editor/`:
  - `FixCarModelEditor.cs`: Araç prefab'ındaki veya modelindeki hizalama ve görünüm sorunlarını düzeltmek için kullanılan özel editör aracı.
  - `FixRoadPropsEditor.cs`: Çevresel yol objelerini düzenleyen, yerleşim sorunlarını gideren yardımcı editör aracı.

## Kurulum ve Çalıştırma

1. Projeyi bilgisayarınıza klonlayın veya indirin.
2. **Unity Hub**'ı açın ve `Add project from disk` (Diskten proje ekle) seçeneği ile projenin kök klasörünü seçerek projeyi listeye ekleyin.
3. Projeyi Unity ile açın.
4. Unity açıldıktan sonra ana simülasyon sahnesini (Scene) `Assets/Scenes/` klasöründen bularak açın.
5. Simülasyonu başlatmak için üst kısımdaki **Play** butonuna basın.

## Simülasyonun İşleyişi

1. Simülasyon başladığında araç, yol boyunca önceden belirlenmiş olan referans bir spline'ı izlemeye başlar.
2. Her karede (frame), aracın spline'a olan sapması hesaplanır ve `PIDController`'a hata (error) değeri olarak iletilir.
3. PID algoritması, aracın rotasını düzeltmek için uygun bir direksiyon/dönüş açısı hesaplar.
4. Aracın hareketine ait referans ve gerçekleşen konum sinyelleri `UIGraphManager` sayesinde ekranda anlık olarak gösterilir.
5. Aynı zamanda bu kontrol verileri `DataLogger` tarafından ödev raporunda kullanılmak üzere kaydedilir.

## Rapor ve Dokümantasyon

Proje klasörünün ana dizininde bulunan `Otomatik Kontrol_Final_ödevi.pdf` dosyası, projenin temel gereksinimlerini, ödev kısıtlamalarını ve teorik arka planını içermektedir. Simülasyon bu pdf'te belirtilen kriterlere (PID entegrasyonu, loglama, grafikler vb.) uygun şekilde tamamlanmıştır.

---
**Not:** Bu proje akademik/eğitim amaçlı geliştirilmiştir.
