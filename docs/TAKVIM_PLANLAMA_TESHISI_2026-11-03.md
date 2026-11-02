# 3 Kasım 2026 - Haftalık birebir planlama açıklamaları

> Bu rapor önceki geliştirme aşamasını kaydeder. Ortak bakiye davranışı sonraki ayrı kategori hesapları çalışmasıyla değiştirilmiştir. Güncel durum: [Paket ve kategori hakları](PAKET_VE_KATEGORI_HAKLARI_2026-11-03.md).

## Görselden doğrulanan durum

Kullanıcının paylaştığı üye ekranında FLEX - 8 Ders paketinin bitişi 16.11.2026. Önceki planlama görselinde başlangıç haftası 30.11.2026 seçilmiş. Seçilen hafta paket dönemi dışında kaldığı için yeni ders üretilemiyor. Eski mesaj, hangi başlangıç ve bitiş tarihlerini karşılaştırdığını göstermiyordu.

8 birebir hak, 4 tamamlanan/kullanılan birebir ve 3 kalan ortak hak ayrıca değerlendirilmelidir. Canlı üyenin ders/hak geçmişi okunmadığından bir haklık farkın kaynağı doğrulanmadı. Bakiye, eski veri veya manuel işlem olduğu varsayılarak değiştirilmedi.

## Değişiklik

- Önizleme, paket başlangıç/bitişini ve mevcut ortak hak özetini de döndürür. Birebir sayaçları ile birebir + grup ortak sayaçları açıkça ayrılır.
- Paket toplamı ile tamamlanan ve kalan toplamı uyuşmuyorsa haftalık plan formunda uyarı ve yönetici için ders hakları/geçmişi bağlantısı gösterilir.
- Haklara göre birebir üst sınırı ve seçilen gün/tarihlerle gerçekten planlanabilen yeni ders sayısı ayrı gösterilir.
- Paket bitişinden sonraki hafta seçildiğinde başlangıç haftası ve paket dönemi açıkça yazılır. Aylık sınır doluluğu ayrı bir gerekçedir. Kısmi programda eklenmeyen hak, uygun yeni tarih sayısı ve aylık sınıra takılan tarih sayısı belirtilir.
- Başarısız planlarda yeni ders sayısı sıfırdır. Önizleme bağlantı hatasında kayıt düğmesi kapalı kalır.
- Paket süresi, aylık limitler ve hak bakiyesi değiştirilmedi. Canlıda ders oluşturulmadı, hak düzeltilmedi; migration gerekmez.

## Doğrulama ve sınır

- Beş yeni otomatik senaryo: 24 hak/17 uygun tarih/7 eklenmeyen hak; dönem dışı başlangıç; 8/4/3 bakiye farkı; karma paket sayaçları; dolu aylık limit.
- 16 Kasım bitiş/30 Kasım başlangıç örneğinde engel açıklanır; başlangıç 2 Kasım'a alındığında 3 ders planlanabilir, ayrı bakiye uyarısı korunur.
- Son çözüm testleri: **700 başarılı, 0 başarısız**. JavaScript sözdizimi ve diff boşluk kontrolü geçti.
- Ayrı `no23_calendar_diagnostics_20261102` PostgreSQL veritabanında migration'lar, gerçek Npgsql/Serializable kaydetme yolu, 17 dersin kaydı, bakiyenin 24 kalması, dönem dışı kaydın reddi ve 8/4/3 uyarısı doğrulandı. Bu veritabanı yalnız bu inceleme için oluşturuldu.
- Tarayıcıda gerçek önizleme servisinden üretilen JSON örnekleriyle çalışan izole formda sayaçlar, uyarı, tarih açıklaması ve kayıt düğmesinin durumu kontrol edildi. Bu kontrol canlı web uygulamasında uçtan uca kabul testi değildir.
- Yerel ana veritabanında ilgili canlı üye bulunmadı. Chrome'daki canlı oturum araç tarafından açılamadı; canlı tespitler yalnız kullanıcının paylaştığı görsellere dayanıyor.
- Değişiklikler yerelde; commit/push ve canlı yayın yapılmadı. Canlıdaki bir haklık farkın kaynağı açık inceleme maddesidir.
