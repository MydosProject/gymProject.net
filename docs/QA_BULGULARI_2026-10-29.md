# QA bulguları — 29 Ekim 2026

İlk bulgu sürümü: `eee4bbf`. Ayrı PostgreSQL 18 veritabanı, sahte üyeler ve yerel uygulama kullanıldı. Canlı üye/tahsilat verisi kullanılmadı. **Üç bulgu da düzeltildi ve yeniden test edildi.** Aşağıdaki ilk hata kanıtları geçmiş kayıt olarak korunmuştur; güncel sonuçlar her bulgunun altında belirtilir.

## BUG-01 — Paket başlamadan önceki grup dersine rezervasyon ve hak düşümü

**Öncelik:** P1. **Vaka:** K-06. **Durum:** Düzeltildi; gerçek PostgreSQL servis çağrısıyla yeniden test geçti.

**Önkoşul:** Spor paketi olan üye; başlangıç 03.11.2026, bitiş 03.12.2026; 70 hak. Aktif grupta 31.10.2026 tarihli, boş kontenjanlı, planlanmış ders. İstek günü 29.10.2026.

**Adımlar:** Üyenin kullanıcı ID'si ve bu seansın ID'siyle `ClassReservationService.ReserveAsync` çağrılır; sonuç, rezervasyon ve bakiye okunur.

**Beklenen:** Ders paket başlangıcından önce olduğu için reddedilir; rezervasyon oluşmaz, hak 70 kalır.

**Düzeltme öncesi gerçek:** `Succeeded=True`; rezervasyon oluştu ve bakiye **70→69** oldu.

**Düzeltme öncesi kaynak:** [MemberPackageEntitlement.IsActive](../src/NO23.Web/Services/MemberPackageEntitlement.cs) yalnız Kitchen türü ve bitiş tarihini kontrol ediyordu. [ClassReservationService](../src/NO23.Web/Services/ClassReservationService.cs) hem şimdi hem ders tarihi için bu fonksiyonu kullanıyor, başlangıç sınırını ayrıca kontrol etmiyordu.

**Düzeltme kabulü:** Ders başlangıcı paket başlangıcından önceyse reddet. Başlangıca eşit/sonra ve bitiş sınırında ayrı kontroller ekle. “Paket henüz başlamadı ama paket dönemindeki gelecek derse önceden rezervasyon yapılabilir mi?” ayrı bir ürün kararıdır; burada kanıtlanan hata, dersin kendisinin paket dönemi dışında olmasıdır. Mevcut sınırsız/eski başlangıç tarihi olmayan/Kitchen profilleri için regresyon testleri korunmalı.

**Yapılan düzeltme ve yeni sonuç:** Rezervasyon servisi ders tarihini kayıtlı paket başlangıcına karşı kontrol ediyor. Dönem öncesi ders `Succeeded=False` döndü; rezervasyon oluşmadı, 70 hak korundu. Aynı seans tam paket başlangıcına alındığında kabul edildi ve bir kez hak ayırdı: 70→69. Paket dönemi içindeki ileri bir ders için önceden rezervasyon davranışı korunmuştur. Bitiş sınırı mevcut kontrolü kullanır; yeni global aktivasyon veya süre politikası uygulanmadı.

**Regresyon:** 6 başlangıç/bitiş sınırı, 2 sınırsız/eski dönem ve 1 Kitchen senaryosu eklendi. Düzeltmeden önce dönem öncesi iki senaryo başarısızdı; düzeltmeden sonra 9'u da geçti.

## BUG-02 — Mobil ölçüm değerleri etiketlerini kaybediyor

**Öncelik:** P1. **Vaka:** G-09. **Durum:** Düzeltildi; yönetici ve eğitmen ekranlarında gerçek tarayıcıyla yeniden test geçti.

**Ekranlar:** `/Admin/MemberProgress?memberId=...`, `/Trainer/Measurements?memberId=...`.

**Önkoşul:** Aynı üyenin üç ölçümü: 08.10.2026 → 80 kg, 18.10.2026 → 78 kg, 28.10.2026 → 76 kg. Pencere 390 px.

**Adımlar:** Ölçüm karşılaştırması ve geçmiş tablosuna ilerlenir; değerlerin hangi tarih/ölçüme ait olduğuna bakılır.

**Beklenen:** İlk/önceki/son ve değişim etiketleri, geçmişte tarih/ölçüm adı ve birimleri görünür.

**Düzeltme öncesi gerçek:** Yönetici karşılaştırmasında `80 / 78 / 76 / -2 / -4` alt alta, hangi kolon oldukları belirtilmeden görünüyordu. Geçmişte kilo/bel/kalça/basen sayıları da etiketlerini kaybediyordu. Eğitmende aynı sorun vardı.

**Düzeltme öncesi teknik kanıt:** Mobil ortak CSS `thead`'i `clip: rect(0px, 0px, 0px, 0px)` ile gizliyor; hücre etiketini `td::before { content: attr(data-label) }` üzerinden bekliyordu. Bu iki ekranın ölçüm hücrelerinde `data-label` yoktu; hesaplanan içerik boştu. Sayfa yatay taşmasa da bilgi okunabilirliği bozuluyordu.

**Kaynak:** [Ortak tablo CSS'i](../src/NO23.Web/wwwroot/css/admin.css), [yönetici ölçüm görünümü](../src/NO23.Web/Areas/Admin/Views/MemberProgress/Index.cshtml), [eğitmen ölçüm görünümü](../src/NO23.Web/Areas/Trainer/Views/Measurements/Index.cshtml).

**Düzeltme kabulü:** Her mobil değerin ölçüm/tarih etiketi olmalı veya karşılaştırmaya özel, başlıkları koruyan tablo düzeni kullanılmalı. Masaüstü karşılaştırma ve boş ölçümlerin `—` gösterimi korunmalı. Mobilde tüm değerlerin tek sayfada görünmesi zorunlu değildir; her biri anlaşılır ve erişilebilir olmalıdır.

**Görsel kanıt:** Çalışma alanındaki `test-bulgu-olcum-mobil-20261029.png` ekran görüntüsünde bu değerler etiketsizdir.

**Yapılan düzeltme ve yeni sonuç:** Karşılaştırma/geçmiş hücreleri tarih, ölçüm adı ve birimlerini `data-label` ile taşır; masaüstü başlığı ve mobil etiket aynı tarih metnini kullanır. Yönetici ve eğitmende 390/768/1366 px'de etiketsiz veri hücresi sayısı sıfır; sayfa ekran genişliğini aşmadı. Boş ve tek ölçümlü kayıtlar da doğrulandı; olmayan önceki ölçüm `—` kalır. Yönetici formundan 77 kg ölçüm kaydı başarıyla oluşturuldu. Mobil yönetici işlem hücresinin koyu zemini korundu.

**Düzeltme sonrası görsel:** Çalışma alanındaki `olcum-etiketleri-duzeltildi-20261029.png`.

## BUG-03 — Mobil randevu kartında alan başlıkları kayboluyor

**Öncelik:** P2. **Vaka:** G-08. **Durum:** Düzeltildi; gerçek tarayıcıyla yeniden test geçti.

**Ekran:** `/Admin/AppointmentRequests`, 390 px.

**Adımlar:** Tercih edilen zamanı ve kesin planı olan randevuyu mobilde aç.

**Beklenen:** İletişim, hizmet, tercih edilen zaman, not, durum ve kesin takvim planı açıkça etiketlenir.

**Düzeltme öncesi gerçek:** Başlıklar gizleniyor, hücrelerde `data-label` bulunmadığından tarih ve durum blokları başlıksız kalıyordu. Tercih edilen ve planlanan zamanı ayırt etmek zordu. Kaydet düğmesi erişilebilirdi.

**Kaynak:** [Randevu talepleri görünümü](../src/NO23.Web/Areas/Admin/Views/AppointmentRequests/Index.cshtml) ve ortak mobil tablo CSS'i.

**Düzeltme kabulü:** Her hücreye uygun mobil etiket veya randevuya özel kart görünümü; uzun not, aynı/ayrı tercih-plan tarihleri ve boş plan testleri.

**Yapılan düzeltme ve yeni sonuç:** Tüm alanlar etiketlendi; “Tercih edilen zaman” ile “Kesin plan” ayrıldı, boş not `—` gösterir. Planlı/plansız randevu ve uzun açıklama ile 390/768/1366 px'de etiketsiz hücre veya yatay sayfa taşması bulunmadı. Durum “Yeni”den “İletişime geçildi”ye formdan kaydedildi; başarılı sonuç ve kalıcı seçim doğrulandı.

**Düzeltme sonrası görsel:** Çalışma alanındaki `randevu-etiketleri-duzeltildi-20261029.png`.

## Ek riskler — henüz hata olarak doğrulanmadı

| Risk | Kodda görülen durum | Sonraki kontrol |
|---|---|---|
| Üye ödeme/taksit geçmişinde benzer mobil etiket kaybı | `Members/Payments.cshtml` taksit hücreleri de `data-label` içermiyor. Bu incelemede veri dolu ödeme tablosuyla tarayıcıda uygulanmadı. | O-05'i mobilde de çalıştır; vade ile tahsilat tarihini ayırt edebil. |
| Büyük üye listesinde performans | Yönetici Members Index tüm üyeleri `ToListAsync` ile alıyor; görünür sayfalama yok. Performans sorunu ölçülmedi. | Y-08: 1000 üye/10.000 dersle süre, sorgu ve HTML boyutunu ölç. |

## Geçen kontroller ve otomasyon sınırı

- G-01/G-02/G-03/G-05: Son üye tablosu düzeltmesi 1024 px masaüstü, 991 px tablet, 390 px mobilde geçti. Tüm işlem düğmeleri kabın içinde; sayfa yatay taşmıyor. Klavye sağ okları tabloyu 0→80 px kaydırdı.
- İlk incelemede 664 otomatik test geçmesine rağmen bu üç kabul senaryosu başarısızdı. Düzeltmeden sonra 9 yeni regresyon senaryosu dahil **673 test geçti, 0 başarısız, 0 atlanan**. Mobil kontroller gerçek tarayıcıda ayrıca uygulandı.
- Test planında uygulanmış 7 senaryonun güncel sonucu **7 geçti, 0 başarısız**; diğer 53 satırın tam kabulü bekliyor.
- Daha önce bildirilen `0xffffffff` süreç çıkışının nedeni bu incelemede doğrulanmış sayılmadı; önceki SQL çıktısı tek başına bir hata kaydı değildir. Y-06/Y-07 senaryoları çalışma anı dayanıklılık kontrollerini kapsar.

[60 test senaryosu ve uygulama sırası](TEST_CASELERI_2026-10-29.md)
