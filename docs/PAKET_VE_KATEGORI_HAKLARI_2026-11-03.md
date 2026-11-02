# Üyelik paketleri ve ayrı ders hakları — 3 Kasım 2026

Bu rapor yerel uygulama değişikliklerini açıklar. Canlı sunucuya dağıtım veya canlı üye bakiyesi düzenlemesi yapılmadı.

## Tamamlanan davranışlar

- Yeni üye kayıt ekranı Üyelik (HYBRID / PRO / BLACK), Personal Training, Grup / Performance / Reformer ve Kids Club kategorilerini gösterir. Katalogdaki fiyatı tanımlı seçenek satın alma ekranına taşınır; teklif gerektiren seçenek için bilgi başvurusu bulunur.
- Üye kaydı ödeme bekleyen hesap oluşturur. Ödeme doğrulanmadan ders hakkı veya aktif paket dönemi başlamaz. Yönetici tarafından onaylanan manuel üye oluşturma mevcut işleyişini korur.
- Her paket aktivasyonu beş ayrı dönem hesabı açar: birebir PT, Performance, Reformer, genel grup, Kids Club. Örnek 6 aylık HYBRID: 24 PT + 48 Performance = 72 ders.
- Rezervasyon yalnız dersin kategorisinden hak ayırır. PT planı yalnız PT hesabını kullanır, sonuçlandırmada aynı hesaptan düşer. Başka kategoriden otomatik tamamlama yapılmaz.
- İptal ve silmeler, rezervasyonun kaynak hesabına iade edilir. Eski dönemin iadesi yeni paket hakkını artırmaz. Tekrarlanan iptal / ödeme bildirimi / aynı dönüşüm tekrar hak değiştirmez.
- Aylık kategori sınırları satın alma anındaki tanımdan saklanır. Takvim değişiklikleri de bu sınırları korur. Satın alımdan sonra katalog düzenlense bile siparişin hak ve süre tanımı değişmez.
- Üye ana sayfası, rezervasyonlar, satın alma ekranı ve yönetici ders hakları ekranı ayrı hakları gösterir. Yönetici üye listesindeki özet de kategori bazındadır. İlk paket toplamı, dönüşüm farkı, kullanılan, kalan, planlı ve yeni planlanabilir sayıları ayrıdır.
- Yönetici, Üyeler > Ders hakları altında kaynak/hedef kategori ve oranla dönüşüm kuralı oluşturabilir, pasife alabilir ve gerekçeli dönüşüm yapabilir. Planlı PT hakları dönüşüme açılamaz. Aylık hedef sınırını artırma tercihi kuralda açıkça seçilir. İşlemi yapan kişi ve hak hareketleri kayıtlıdır.
- Önceki aşamada eklenen admin/eğitmen gün/hafta saat çizelgesi ve üyeye gerçek PT seanslarını gösterme çalışması korunur.

## Eski kayıtların geçişi

Şema değişikliği `20261102223601_AddCategorizedLessonCredits` migration dosyasındadır. Eski toplam haklar bütün üyelere keyfi oranlarla dağıtılmaz. Seçilmiş paket, dönem, kullanım kategorileri ve kayıtlı toplam birebir eşleşiyorsa ilk hak işlemi sırasında güvenli geçiş yapılır. Eşleşmeyen kayıt yeni rezervasyona kapatılır ve yöneticiye bildirilir. Yönetici aynı ekranda beş kategorinin dağılımını, başlangıç tarihini ve düzeltme gerekçesini doğrulayarak geçiş yapabilir.

Migration, katalogdaki bilinen ders adlarını kategoriler: Bootcamp / Metcon / Kalça & Crunch / Mat Pilates Performance, Reformer Pilates Reformer. Diğer mevcut dersler genel grup olarak kalır; özel derslerin kategorisi Ders Yönetimi ekranından doğrulanmalıdır. Aktif rezervasyon varken dersin kategorisi değiştirilemez.

## Doğrulama

- 729 .NET testi ve 3 takvim yerleşimi JavaScript testi geçti. Son mobil etiket düzeltmesinden sonra uygulama yeniden derlendi ve tarayıcıda kontrol edildi.
- Geçici PostgreSQL veritabanında migration zinciri, model uyumu, gerçek 6 aylık HYBRID aktivasyonu, Performance rezervasyonunun doğru hesaptan düşmesi, aynı son hak için eşzamanlı rezervasyonlar ve tek seferlik dönüşüm kaydı doğrulandı.
- Gerçek uygulama tarayıcıda açılarak kayıt paket seçimi, üye hak tablosu ve yönetici dönüşüm formu kontrol edildi.
- Gerçek karttan ödeme veya canlı veri üzerinde işlem yapılmadı. Ödeme sağlayıcısının canlı yapılandırması bu yerel testin kapsamında doğrulanmış sayılmaz.
