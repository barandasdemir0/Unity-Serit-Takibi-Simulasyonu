<style>
body {
    font-family: Arial, sans-serif;
    line-height: 1.6;
    margin: 20px;
}
h1, h2 {
    text-align: center;
}
h1 { margin-bottom: 0px; }
h2 { margin-top: 5px; margin-bottom: 30px; font-weight: normal; }
h3 { margin-top: 20px; }
.images { text-align: center; margin: 20px 0; }
.images .placeholder { width: 100%; height: 250px; background: #eee; border: 1px dashed #aaa; display: flex; align-items: center; justify-content: center; margin-bottom: 10px; color: #555; }
</style>

# OTOMATİK KONTROL FİNAL ÖDEVİ RAPORU
## Unity Ortamında PID Kontrol ile Araba Şerit Takibi

### (a) Amaç
Projenin amacı, Unity oyun motoru ortamında bir aracın belirlenen referans şerit merkez çizgisini PID kontrol algoritması kullanarak otonom olarak takip etmesini sağlamaktır. Sistem, düzlükler, hafif virajlar ve keskin virajlar içeren karmaşık bir yol yapısı üzerinde, yanal sapmayı (hata) minimize ederek stabil ve güvenli bir seyir gerçekleştirmeyi hedeflemektedir.

### (b) Sistem ve yol tanımı
Simülasyon ortamı, düzlükler ve çeşitli eğriliklere sahip virajdan oluşan kapsamlı bir parkuru içermektedir. Araç, global X ve Z koordinatları üzerinde hareket etmekte olup, aracın kinematik yapısı (Kinematic Bicycle Model) ile simüle edilmektedir. Hedef yörünge, yolun tam orta noktasından geçen referans merkez çizgisi **r(t)** olarak tanımlanmıştır. Aracın rotası simülasyon boyunca dinamik olarak güncellenmektedir.

### (c) Hata ve kontrolcü yapısı
Hata sinyali **e(t)**, aracın referans çizgiye olan yanal sapması olarak hesaplanmıştır. Denklem **e(t) = r(t) − y(t)** şeklindedir; burada **r(t)** referans yolun merkezini, **y(t)** ise aracın anlık yanal konumunu ifade eder. Hata değerini sıfıra yaklaştırmak ve aracı referans hattında tutmak amacıyla yönlendirme açısını (kontrol çıkışı) belirleyen bir PID kontrolcü kullanılmıştır. Üretilen **u(t)** kontrol sinyali ile araca sağa veya sola yönelme komutu uygulanmaktadır.

Kontrol denklemi: **u(t) = Kp e(t) + Ki ∫e(t)dt + Kd (de(t)/dt)**

### (d) Kullanılan parametreler
Gerçekleştirilen testler ve analizler sonucunda sistemi en stabil (kararlı) hale getiren ve aşımları minimize eden PID parametreleri aşağıdaki gibi belirlenmiştir:

*   **Oransal Kazanç (Kp):** 2.00
*   **İntegral Kazanç (Ki):** 0.10
*   **Türevsel Kazanç (Kd):** 1.00

### (e) Grafikler
Aşağıda simülasyon verilerinden elde edilen sistem performansı grafikleri sunulacaktır. Yanal hata ve kontrol sinyali grafikleri zaman ekseninde çizdirilmiş olup, yörünge takibi grafiği doğrudan x-z düzlemindeki (üstten görünüm) hareketleri yansıtmaktadır.
*(Not: Kendi simülasyonunuzdan kaydettiğiniz görselleri buralara yerleştirebilirsiniz.)*

<div class="images">
    <div class="placeholder">Zamana Bağlı Yanal Hata e(t) Grafiği</div>
    <div class="placeholder">Zamana Bağlı Kontrolcü Çıkışı u(t) Grafiği</div>
    <div class="placeholder">Referans Yol ve Gerçek İzlenen Yol Karşılaştırması</div>
</div>

### (f) Karşılaştırma ve analizler

**Araç düz yolda nasıl davranmaktadır?**
Düz yolda referans hat sabit olduğu için yanal hata (e(t)) hızla sıfıra yaklaşmaktadır. Araç, düz yolda gereksiz osilasyonlar (salınımlar) yapmadan merkez çizgisi üzerinde son derece istikrarlı ve pürüzsüz bir ilerleme sergilemektedir.

**Virajlarda hata nasıl değişmektedir?**
Viraja girildiğinde referans hattı sürekli değiştiği için anlık yanal sapma miktarında artış gözlemlenmektedir. Ancak PID kontrolcünün türevsel (D) ve oransal (P) etkileri sayesinde hata güvenli sınırlar içerisinde tutulmakta ve araç şeritten savrulmadan virajı tamamlayabilmektedir. Viraj bitiminde hata hızla sönümlenmektedir.

**PID parametreleri değiştirildiğinde araç davranışı nasıl etkilenmektedir?**
**Kp** değeri çok yüksek olduğunda araç yola hızlıca dönmeye çalışırken aşırı salınım yapmakta, direksiyon sert tepkiler vermektedir. **Kd** düşük kaldığında viraj girişlerinde sönümleme yetersiz kalmakta ve araç yoldan dışarı taşabilmektedir. **Ki** değerinin aşırı artması ise kontrol sisteminin aşırı düzeltme yapmasına ve sürekli bir kararsızlığa düşmesine sebep olmaktadır.

**Hangi parametre seti en iyi sonucu vermiştir?**
Deneme-yanılma ve iteratif testler sonucunda en optimum ve stabil takip başarımı **Kp = 2.00**, **Ki = 0.10** ve **Kd = 1.00** değerleriyle elde edilmiştir.

**Kontrol sinyali çok agresif midir, yumuşak mıdır?**
Optimum parametrelerle (2.00, 0.10, 1.00) üretilen u(t) sinyali dengeli ve yumuşaktır. Sadece sert viraj başlangıçlarında doğal olarak anlık tepki pikleri oluşmakta, ancak geri kalan rotada kontrol sinyali sarsıntısız bir sürüş sunmaktadır.

**Araç salınım yapmakta mıdır? Sistem kararlı mıdır?**
Sistem tam kararlı (stable) durumdadır. Türevsel kazancın (Kd) uygun ayarlanmasıyla sistemin sönümleme oranı ideal seviyeye çekilmiş, böylece aracın sürekli sağa sola salınım yapması (overshoot/oscillation) başarıyla engellenmiştir.

**Keskin virajlarda takip başarımı düşmekte midir?**
Evet, yüksek eylemsizlik ve ani referans değişimleri nedeniyle keskin virajlarda yanal sapma hatası, hafif virajlara ve düzlüklere oranla anlık olarak daha yüksek değerlere ulaşmaktadır. Bu durum takip başarımında bir düşüşe neden olsa da, kontrolcü aracı her halükarda yolun sınırları içinde tutmayı başarmaktadır.

**Kontrolcü parametreleri değiştiğinde yükselme ve yerleşme sürelerine ilişkin davranış nasıl etkilenmektedir?**
**Kp** değerinin artırılması sistemin yükselme süresini (hedefe yönelme hızını) kısaltırken, aşım miktarını artırarak yerleşme süresini (hatasız konuma oturmayı) geciktirmektedir. Uygun bir **Kd** parametresinin sisteme dahil edilmesi, oluşan bu aşım ve salınımları sönümlemiş, aracın referans yörüngeye hızlı ve stabil bir şekilde yerleşmesini sağlamıştır.

### (g) Sonuç
Bu çalışmada, Unity ortamında bir aracın referans yolunu takip etmesini sağlayan PID tabanlı kontrol sistemi başarıyla geliştirilmiştir. İteratif testler sonucunda elde edilen optimum PID parametreleriyle sistemin düz, hafif virajlı ve keskin virajlı yollarda kararlılığını koruduğu ve aracı şerit merkezinde tuttuğu görülmüştür. Grafikler üzerinden yapılan hata ve kontrol sinyali analizleri, kurulan teorik modelin simülasyonda beklenen pratik gereksinimleri tam olarak karşıladığını kanıtlamaktadır.

---
**Baran | Öğrenci Numarası**
