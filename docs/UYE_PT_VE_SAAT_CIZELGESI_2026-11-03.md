# 3 Kasım 2026 - Üye PT görünürlüğü ve saat çizelgesi

> Bu rapor önceki geliştirme aşamasını kaydeder. Ortak bakiye davranışı sonraki ayrı kategori hesapları çalışmasıyla değiştirilmiştir. Güncel durum: [Paket ve kategori hakları](PAKET_VE_KATEGORI_HAKLARI_2026-11-03.md).

## Sorun ve değişiklik

Üye özeti satın alınan paketin PT ve grup haklarını tek toplam halinde gösteriyordu. Rezervasyon ekranı yalnız grup rezervasyonları ve PT taleplerini listeliyor, yönetici/eğitmenin oluşturduğu gerçek birebir takvim seanslarını göstermiyordu. PT talep izni de seçilmiş paket varyantı yerine eski üyelik seviyesinin `IncludesPersonalTrainingSupport` işaretine bağlıydı.

- Ortak hak özetinde PT/grup paket toplamları ve her türün tamamlanan/planlanan sayıları ayrı gösterilir. Eski bir paket seçeneği kayıtlıysa haklar o kayıttan okunur; güncel katalogdan tahmin edilmez.
- Genel Bakış ve Rezervasyonlarım, üyenin kendisine ait ileri tarihli birebir takvim seanslarını eğitmen ve süreyle gösterir. İptal, geçmiş ve paket dönemi dışındaki seanslar listeye alınmaz. Grup rezervasyonlarının başlıkları buna göre açıkça adlandırılır.
- Üyelik yenileme seçenekleri PT ve grup içeriğini ayrı gösterir; mevcut paketin ortak özeti bu ekranda da vardır.
- PT talep izni varsa seçilmiş varyantın PT sayısından, yoksa eski paket seçeneğinden, o da yoksa eski üyelik seviyesi işaretinden hesaplanır. Sıfır PT içeren bir varyant eski genel izin nedeniyle PT talebine açılmaz.
- Admin/eğitmen gün ve hafta takvimlerinde Türkiye saatine hizalı çizelge vardır. Birebir, grup ve ön görüşme renklerle ayrılır; çakışan zaman aralıkları yan yana yerleştirilir.
- Dersin detay penceresi mevcut kartı ve formunu açar; ikinci bir kayıt formu oluşturulmaz. Kart listesine dönüş, gün/hafta gezinmesi, filtreler ve mevcut toplu işlemler korunur. Saat çizelgesi planlama formlarından önce gösterilir. JavaScript olmadan eski kart listesi çalışır.

## Doğrulama

- **704 .NET testi başarılı; 0 başarısız.** PT yetkisi, seçilmiş paket/eskiden kalan işaret çelişkisi, eski paket seçeneği, üyeye özel seans görünürlüğü ve ödeme sonrası kategori toplamları kapsanır. Son görsel düzenlemeden sonra web derlemesi: 0 hata, 0 uyarı.
- Üç Node testi: çakışan PT/grup/ön görüşme aralıklarının yan yana yerleşimi, aynı anda üç ders, bitiş/başlangıcı eşit ardışık derslerin çakışmaması.
- Ayrı `no23_member_calendar_qa_20261103` PostgreSQL veritabanı kullanıldı; migration'lar uygulandı. Sahte HYBRID 6 aylık sipariş ödeme alındı olarak işaretlenip gerçek üyelik aktifleştirme servisinden geçirildi. Sonuç **24 PT + 48 grup = 72 hak**; tekrar gerçek tahsilat veya sağlayıcı işlemi yapılmadı.
- Gerçek yerel web uygulamasında sahte üye hesabıyla Genel Bakış ve Rezervasyonlarım; admin ve eğitmen hesaplarıyla takvim kontrol edildi. Üye ekranında PT/grup toplamları ve 04.11.2026 09:00-09:50 PT seansı görüldü. Admin takviminde 09:00 PT ile 09:30 grup dersi yan yana açıldı; detay penceresinde mevcut düzenleme alanları doğrulandı. Admin gün/hafta, kart listesi dönüşü, eğitmenin yalnız kendi dersleri ve 390 piksel mobil taşma kontrol edildi.
- Tarayıcı kontrolü yerel uygulama ve izole test verisiyle yapıldı. Canlı müşteri oturumu bu doğrulamanın parçası değildir. UI üzerinden gerçek üye veya ders değişikliği yapılmadı.

## Açık kapsam

**PT ve grup için bağımsız kalan bakiyeler henüz yoktur.** Veritabanındaki `RemainingClassCredits` ortaktır. Bu çalışma paket içeriğini ve tür bazlı geçmiş/planları görünür hale getirir; ayrı hak defteri ve kategoriye özel hak düşümünü tamamlamaz. Kalan ortak hak açıkça bu adla gösterilir. Geçmiş bakiyeler otomatik yeniden dağıtılmaz.

Ay görünümü, sürükleyerek tarih/saat değiştirme, Google hesabıyla senkronizasyon ve gece yarısını aşan derslerin iki ayrı gün hücresinde gösterilmesi bu sürümde yoktur. Mevcut düzenleme formlarıyla tarih değiştirme korunur.

Bu değişikliklerde migration yoktur. Commit, push ve canlı yayın yapılmadı. Önceki görseldeki 8 toplam / 4 kullanılan / 3 kalan farkının canlı kayıttaki nedeni bu çalışmayla doğrulanmış değildir.

Kontrollerin ardından yerel QA uygulaması durduruldu ve yalnız bu iş için oluşturulan `no23_member_calendar_qa_20261103` veritabanı kaldırıldı. Gerçek yerel/canlı üye kayıtları değiştirilmedi.
