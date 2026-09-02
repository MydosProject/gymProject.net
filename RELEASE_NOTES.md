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
