# NO23 V8 – Takvim, ders yönetimi, mesajlaşma ve üyelik yenileme (taslak)

Hazırlanma tarihi: 22 Eylül 2026
Durum: Kod `no23-V8` çalışma ağacında; henüz commit/push veya canlı yayın yapılmadı.

## İstek / yapılan geliştirme

| Gelen istek | Yapılan geliştirme |
| --- | --- |
| Birebir dersleri paket hakkına göre haftalık sabit gün ve saatte planlamak | Admin ve eğitmen takvimine haftalık birebir planlama eklendi. Seçilen günlerde 1–52 hafta için seanslar oluşturuluyor; paket hakkı, aylık birebir sınırı ve çakışmalar kontrol ediliyor. Hata varsa kısmi kayıt yapılmıyor. |
| Grup dersini silebilmek | Seansı olmayan grup dersi silinebiliyor. Geçmişi olan ders arşivleniyor; gelecek seanslar ve rezervasyonlar iptal edilip ders hakları iade ediliyor, ilgililere panel bildirimi gönderiliyor. |
| Grup derslerini 12 haftadan uzun, sezon boyunca açmak | Tek işlemde planlama üst sınırı 12 haftadan 52 haftaya çıkarıldı. |
| Takvimde dersleri eklenme sırası yerine saate göre göstermek | Haftalık takvimde grup ve birebir dersler ortak saat sırasına alındı. |
| Adminin üyeye atanmış eğitmeninden farklı eğitmenle ders yazabilmesi | Admin tekil ve haftalık birebir derslerde farklı eğitmen seçebiliyor. Üyenin kalıcı eğitmen ataması değişmiyor. |
| Eğitmenin tamamladığı grup ve birebir ders sayılarını ayrı görmek | Eğitmen dashboard'unda haftalık ve aylık grup/birebir tamamlanan ders sayıları ayrıldı. |
| Takvimde grup/birebir renklerini daha belirgin yapmak | Kart zeminleri, kenarlıkları ve tür etiketleri farklılaştırıldı. |
| Paket bitince üyenin yeni paket alabilmesi | Üye paneline üyelik yenileme bağlantısı ve paket/ödeme ekranı eklendi. Üye dönem toplamını iyzico üzerinden kartla ödüyor; doğrulanan ödeme paketi, bitiş tarihini ve ders haklarını otomatik etkinleştiriyor. Tekrarlanan callback ve gecikmiş ödeme mutabakatı ele alındı. |
| Ders süresinin varsayılan 60 yerine 50 dakika olması | Yeni birebir derslerin ve formların varsayılan süresi 50 dakika yapıldı; yeni oluşturulan grup dersleri ile başlangıç Reformer örneği 50 dakika. |
| Eğitmenin “Derslerim” ekranında birebir dersleri de görmesi | Eğitmenin grup derslerinin yanına birebir seans listesi eklendi. |
| Eğitmenin mesaj göndermek için üye seçebilmesi | Atanmış üyelerden seçim yapıp konuşma başlatma veya mevcut konuşmayı açma eklendi. Mevcut SignalR canlı mesaj, okundu ve panel içi bildirim akışı korunuyor. |
| Admin ders programındaki uzun seans listesini sınıf düzeninde görmek | Program ders türü ve haftalık gün/saat başlıklarına ayrıldı; tarihli seans, kontenjan ve katılımcılar açılarak görülebiliyor. Geçmiş/iptal edilenler isteğe bağlı gösteriliyor. |

## Açık kalanlar ve yayın öncesi işler

- **Aylık otomatik kart tahsilatı yok.** Yenileme ekranındaki ücret, seçilen dönem için tek çekim toplam tutardır. Önceki “her ay otomatik ödeme” talebi ayrı bir abonelik/tahsilat geliştirmesi gerektiriyor.
- **Site kapalıyken dış bildirim yok.** Mesaj sayacı ve “Yeni mesaj” panel bildirimi mevcut; tarayıcı push, e-posta veya SMS bildirimi eklenmedi.
- **Üyeden PT'ye ilk mesaj** için tek tıkla konuşma açma henüz yok. Şu an eğitmen atanmış üyeyi seçerek konuşma başlatabiliyor; üye mevcut konuşmadan yazabiliyor.
- **Farklı eğitmenle yazılan birebir dersin mesajlaşma yetkisi** henüz ilişkilendirilmedi. Mesaj başlatma atanmış eğitmene bağlı; ders bazlı geçici erişim kuralı ayrıca geliştirilmeli.
- Daha önce kaydedilmiş grup derslerinin süreleri topluca değiştirilmedi; gerekirse admin ekranından güncellenmeli.
- Yeni üyelik migration'ı ve gerçek iyzico ödeme akışı canlı/staging ortamında henüz doğrulanmadı. Bitiş tarihi bilinmeyen eski üyelerde tarih admin tarafından girilebilir.
- Bu taslak henüz commit edilmedi, GitHub'a gönderilmedi ve sunucuya alınmadı.

## Teknik doğrulama

- Çözüm 0 hata, 0 uyarıyla derlendi.
- Otomatik testler: **270/270 başarılı**.
- EF Core modelinde migration sonrası bekleyen değişiklik bulunmadı.
- Yeni migration: `20260922082848_AddMembershipRenewalCheckout`.

---

# NO23 V6 Release Notes

Yayın tarihi: 2 Eylül 2026

## Identity sayfaları statik dosya düzeltmesi

- Üye kayıt ve giriş sayfalarının canlı ortamda stilsiz görünmesine neden olan
  .NET 10 statik varlık endpoint dönüşümü kaldırıldı.
- CSS ve JavaScript dosyalarının mevcut `UseStaticFiles` middleware'i üzerinden
  doğrudan ve güvenilir biçimde sunulması sağlandı.
- Controller ve Razor Pages rotalarındaki `WithStaticAssets` kullanımları
  kaldırılarak boş içerik döndüren parmak izli statik dosya URL'leri engellendi.

## Teknik doğrulama

- Çözüm **0 hata, 0 uyarı** ile başarıyla derlendi.
- Otomatik testlerin **207/207** tamamı başarıyla geçti.

---

# NO23 V1 Release Notes

Yayın tarihi: 17 Ağustos 2026

## Öne çıkan yenilikler

### Galeri deneyimi

- Halka açık ana navigasyona **Galeri** sekmesi eklendi.
- 11 salon fotoğrafı web için optimize edilerek responsive galeriye dönüştürüldü.
- Fotoğraflar için klavye ve mobil uyumlu lightbox deneyimi eklendi.
- Galeri sayfasına geniş **NO23 Experience** video alanı eklendi.
- Video autoplay olmadan, yalnız kullanıcı oynat düğmesine bastığında yükleniyor.
- Dikey video masaüstünde atmosferik arka planla, mobilde doğal 9:16 oranında gösteriliyor.
- Ana sayfaya galeri videosuna yönlendiren posterli NO23 Experience çağrısı eklendi.

### Marka görünürlüğü

- NO23 amblemi web kullanımı için optimize edildi.
- Amblem footer, giriş/kayıt ekranları ve galeri kapanış alanına eklendi.
- Halka açık site, admin, üye ve eğitmen panellerine favicon ve mobil cihaz ikonu eklendi.

### Dokümantasyon

- Ziyaretçi, üye, eğitmen ve admin ekranlarını açıklayan kapsamlı kullanım kılavuzu eklendi.
- Eksik ticari süreçleri P0–P2 öncelikleriyle değerlendiren business gap analizi eklendi.
- Üyelik, ödeme, ders/PT, Kitchen, raporlama, yetkilendirme ve KVKK alanları için geliştirme yol haritası belgelendi.

### Altyapı

- PostgreSQL 18 Docker volume yolu `/var/lib/postgresql` olacak şekilde güncellendi.
- Yerel PostgreSQL container’ının `5433` portundan sağlıklı çalışması doğrulandı.

## Performans ve uyumluluk

- Galeri fotoğrafları WebP formatına dönüştürüldü; orijinal yüksek boyutlu dosyalar doğrudan servis edilmiyor.
- NO23 Experience videosunda `preload="none"` ve gecikmeli kaynak atama kullanılıyor.
- Galeri, video, marka görselleri ve navigasyon mobil ekranlara uyumlu hale getirildi.
- Hareket azaltma tercihi bulunan kullanıcılar için animasyon azaltma desteği korundu.

## Teknik doğrulama

- Proje ayrı derleme çıktısında başarıyla derlendi.
- Derleme sonucu: **0 hata, 0 uyarı**.

## Operasyon notları

- Değişiklikleri görmek için uygulamanın yeniden başlatılması gerekir.
- NO23 Experience videosu yaklaşık 8.2 MB’tır ve kullanıcı tıklamadan indirilmez.
- Iyzico ve SMTP canlı ortam ayarları deployment secret’larıyla ayrıca yapılandırılmalıdır.
