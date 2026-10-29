# QA bulguları — 29 Ekim 2026

Kaynak sürüm: `eee4bbf`. Ayrı PostgreSQL 18 veritabanı, sahte üyeler ve yerel uygulama kullanıldı. Canlı üye/tahsilat verisi kullanılmadı. Bu teslimat test analizi ve senaryo listesidir; aşağıdaki üç yeni sorun düzeltme bekliyor.

## BUG-01 — Paket başlamadan önceki grup dersine rezervasyon ve hak düşümü

**Öncelik:** P1. **Vaka:** K-06. **Durum:** Gerçek PostgreSQL servis çağrısıyla doğrulandı.

**Önkoşul:** Spor paketi olan üye; başlangıç 03.11.2026, bitiş 03.12.2026; 70 hak. Aktif grupta 31.10.2026 tarihli, boş kontenjanlı, planlanmış ders. İstek günü 29.10.2026.

**Adımlar:** Üyenin kullanıcı ID'si ve bu seansın ID'siyle `ClassReservationService.ReserveAsync` çağrılır; sonuç, rezervasyon ve bakiye okunur.

**Beklenen:** Ders paket başlangıcından önce olduğu için reddedilir; rezervasyon oluşmaz, hak 70 kalır.

**Gerçek:** `Succeeded=True`; rezervasyon oluştu ve bakiye **70→69** oldu. Mevcut serviste bu sonuç başarılı kabul ediliyor.

**Kaynak:** [MemberPackageEntitlement.IsActive](../src/NO23.Web/Services/MemberPackageEntitlement.cs) yalnız Kitchen türü ve bitiş tarihini kontrol ediyor. [ClassReservationService](../src/NO23.Web/Services/ClassReservationService.cs) hem şimdi hem ders tarihi için bu fonksiyonu kullanıyor; başlangıç sınırı ayrıca kontrol edilmiyor.

**Düzeltme kabulü:** Ders başlangıcı paket başlangıcından önceyse reddet. Başlangıca eşit/sonra ve bitiş sınırında ayrı kontroller ekle. “Paket henüz başlamadı ama paket dönemindeki gelecek derse önceden rezervasyon yapılabilir mi?” ayrı bir ürün kararıdır; burada kanıtlanan hata, dersin kendisinin paket dönemi dışında olmasıdır. Mevcut sınırsız/eski başlangıç tarihi olmayan/Kitchen profilleri için regresyon testleri korunmalı.

## BUG-02 — Mobil ölçüm değerleri etiketlerini kaybediyor

**Öncelik:** P1. **Vaka:** G-09. **Durum:** Yönetici ve eğitmen ekranlarında gerçek tarayıcıyla doğrulandı.

**Ekranlar:** `/Admin/MemberProgress?memberId=...`, `/Trainer/Measurements?memberId=...`.

**Önkoşul:** Aynı üyenin üç ölçümü: 08.10.2026 → 80 kg, 18.10.2026 → 78 kg, 28.10.2026 → 76 kg. Pencere 390 px.

**Adımlar:** Ölçüm karşılaştırması ve geçmiş tablosuna ilerlenir; değerlerin hangi tarih/ölçüme ait olduğuna bakılır.

**Beklenen:** İlk/önceki/son ve değişim etiketleri, geçmişte tarih/ölçüm adı ve birimleri görünür.

**Gerçek:** Yönetici karşılaştırmasında `80 / 78 / 76 / -2 / -4` alt alta, hangi kolon oldukları belirtilmeden görünüyor. Geçmişte kilo/bel/kalça/basen sayıları da etiketlerini kaybediyor. Eğitmende son/önceki/değişim ve geçmişte aynı sorun var.

**Teknik kanıt:** Mobil ortak CSS `thead`'i `clip: rect(0px, 0px, 0px, 0px)` ile gizliyor; hücre etiketini `td::before { content: attr(data-label) }` üzerinden bekliyor. Bu iki ekranın ölçüm hücrelerinde `data-label` yok; hesaplanan içerik boş. Sayfa yatay taşmasa da bilgi okunabilirliği bozuluyor.

**Kaynak:** [Ortak tablo CSS'i](../src/NO23.Web/wwwroot/css/admin.css), [yönetici ölçüm görünümü](../src/NO23.Web/Areas/Admin/Views/MemberProgress/Index.cshtml), [eğitmen ölçüm görünümü](../src/NO23.Web/Areas/Trainer/Views/Measurements/Index.cshtml).

**Düzeltme kabulü:** Her mobil değerin ölçüm/tarih etiketi olmalı veya karşılaştırmaya özel, başlıkları koruyan tablo düzeni kullanılmalı. Masaüstü karşılaştırma ve boş ölçümlerin `—` gösterimi korunmalı. Mobilde tüm değerlerin tek sayfada görünmesi zorunlu değildir; her biri anlaşılır ve erişilebilir olmalıdır.

**Görsel kanıt:** Çalışma alanındaki `test-bulgu-olcum-mobil-20261029.png` ekran görüntüsünde bu değerler etiketsizdir.

## BUG-03 — Mobil randevu kartında alan başlıkları kayboluyor

**Öncelik:** P2. **Vaka:** G-08. **Durum:** Gerçek tarayıcıyla doğrulandı.

**Ekran:** `/Admin/AppointmentRequests`, 390 px.

**Adımlar:** Tercih edilen zamanı ve kesin planı olan randevuyu mobilde aç.

**Beklenen:** İletişim, hizmet, tercih edilen zaman, not, durum ve kesin takvim planı açıkça etiketlenir.

**Gerçek:** Başlıklar gizleniyor; hücreler `data-label` içermediği için tarih ve durum blokları başlıksız kalıyor. Tercih edilen ve planlanan zamanın ayrımı zorlaşıyor. Kaydet düğmesi erişilebilir; bulgu düğmenin kaybolması değil, bilgilerin etiketsiz kalmasıdır.

**Kaynak:** [Randevu talepleri görünümü](../src/NO23.Web/Areas/Admin/Views/AppointmentRequests/Index.cshtml) ve ortak mobil tablo CSS'i.

**Düzeltme kabulü:** Her hücreye uygun mobil etiket veya randevuya özel kart görünümü; uzun not, aynı/ayrı tercih-plan tarihleri ve boş plan testleri.

## Ek riskler — henüz hata olarak doğrulanmadı

| Risk | Kodda görülen durum | Sonraki kontrol |
|---|---|---|
| Üye ödeme/taksit geçmişinde benzer mobil etiket kaybı | `Members/Payments.cshtml` taksit hücreleri de `data-label` içermiyor. Bu incelemede veri dolu ödeme tablosuyla tarayıcıda uygulanmadı. | O-05'i mobilde de çalıştır; vade ile tahsilat tarihini ayırt edebil. |
| Büyük üye listesinde performans | Yönetici Members Index tüm üyeleri `ToListAsync` ile alıyor; görünür sayfalama yok. Performans sorunu ölçülmedi. | Y-08: 1000 üye/10.000 dersle süre, sorgu ve HTML boyutunu ölç. |

## Geçen kontroller ve otomasyon sınırı

- G-01/G-02/G-03/G-05: Son üye tablosu düzeltmesi 1024 px masaüstü, 991 px tablet, 390 px mobilde geçti. Tüm işlem düğmeleri kabın içinde; sayfa yatay taşmıyor. Klavye sağ okları tabloyu 0→80 px kaydırdı.
- Mevcut 664 otomatik test yeniden geçti. Buna rağmen yukarıdaki üç kabul senaryosu başarısızdır: mevcut testlerde paket başlangıcı dışındaki grup rezervasyonu ve bu mobil etiketler için kapsama açığı var.
- Test planındaki 60 senaryonun yalnız 7'si bu kabul turunda uygulandı: 4 geçti, 3 başarısız. Diğer 53 satırın benzer servis testi bulunabilse de tam kabulü bekliyor.
- Daha önce bildirilen `0xffffffff` süreç çıkışının nedeni bu incelemede doğrulanmış sayılmadı; önceki SQL çıktısı tek başına bir hata kaydı değildir. Y-06/Y-07 senaryoları çalışma anı dayanıklılık kontrollerini kapsar.

[60 test senaryosu ve uygulama sırası](TEST_CASELERI_2026-10-29.md)
