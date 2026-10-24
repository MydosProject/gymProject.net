# 24 Ekim 2026 kabul testleri

Listedeki 15 geliştirme kodda tamamlandı. Aşağıdaki işlemler yayınlanan sürümde test hesabıyla uygulanabilir. Gerçek üyelerin eski bakiyelerini düzeltmeden önce geçmiş manuel işlemleri kontrol edin. Kod ve otomatik/yerel doğrulama teslimat belgesinde; canlı dağıtım ve işletme kabulü ayrıdır.

| # | Madde / ekran | Yöntem ve kısa test |
|---|---|---|
| 1 | İptal grup dersi — Yönetici → Grup Dersleri / Takvim; Eğitmen → Takvimim | Grubu pasife alıp yeniden aktifleştirin. İptal seanslar görünmemeli; eski iptal kendiliğinden açılmamalı. Rezervasyon hakları bir kez iade edilir. |
| 2 | Kitchen kaydı — Ana sayfa → Kitchen menünü oluştur → öğün seçimi → kayıt | Kitchen'den gelen kişi spor paketi seçmeden hesabını açıp aynı öğün/teslimat seçimiyle ödeme ekranına dönmeli. Normal spor kaydında paket ve süre zorunlu. Kitchen hesabında spor hakkı oluşmaz. |
| 3 | PRO görünümü — Ana sayfa ve Paketler → Üyelik | PRO diğer kartlar gibi koyu görünmeli; yıldızlı “En Çok Tercih Edilen” rozeti bulunmalı. |
| 4 | Nakit/havale/özel tutar — Yönetici → Üyeler → Yeni üye / düzenleme | Yönetici spor dahil paketlerde ödeme yöntemini ve ana satış tutarını seçebilir. Örneğin 37.500 ₺ katalog fiyatına 25.000 ₺ satış girin: indirim 12.500 ₺ olmalı, paket süresi/hakkı değişmemeli. Taksit toplamı satış tutarına eşit olmalı. |
| 5 | Tahsilat geçmişi — Yönetici → Üyeler → Ödeme geçmişi | Listede alınan/kalan/son tahsilat; detayda yöntem, tutar, tarih ve taksitler görünmeli. Bir test taksidini tahsil edilmiş kaydedince özet güncellenmeli. Tahsilat tarihi olmayan eski kayıt için tarih uydurulmaz. |
| 6 | Ders düşümü — Yönetici → Üyeler → Ders hakları; Takvim | Test üyesinin geçmiş saatli birebirini tamamlayın: bir hak düşmeli; ikinci gönderimde tekrar düşmemeli. Grup rezervasyonu hak ayırır, tamamlanınca ikinci kez düşmez. İptal hak kaybettirmez. |
| 7 | Mobil iki gün — Yönetici → Takvim / Eğitmen → Takvimim | Telefon genişliğinde Hafta görünümünde iki gün görünmeli, diğer günlere yatay kaydırılmalı. Gün seçildiğinde tek günlük ajanda açılmalı; sayfa yatay taşmamalı. |
| 8 | Kalan seans — Üye → Genel Bakış; Yönetici → Üyeler; Eğitmen → Üyelerim / detay | Aynı üyede toplam/kullanılan/kalan/planlı/planlanabilir değerlerini karşılaştırın. Örnek 70 toplam, 2 kullanılan, 4 planlı → 68 kalan, 64 yeni planlanabilir. Takvimde hafta değiştirince bu sayılar değişmemeli. |
| 9 | Ölçümde üye arama — Yönetici → Ölçüm ve Gelişim; Eğitmen → Ölçümler | En az 2 karakterle ad/soyad, telefon veya #numara arayın. Aynı isimliler telefon/üye numarası/paketle ayrılmalı. Eğitmen yalnız atanmış üyelerini bulmalı. Klavyeyle sonuç seçilebilmeli. |
| 10 | Bekleyen talepler — Yönetici → Genel Bakış | Yeni misafir randevu/paket/birebir talebi gönderin. Üst sayaç ve bekleyen listesinde görünmeli; ilgili bağlantı doğru listeyi açmalı. Kesin ön görüşme planlanınca bekleyen görüşme sayısı azalmalı. |
| 11 | Üye olmayan kişinin ön görüşmesi — Yönetici → Randevu Talepleri → Planla | Üye açmadan eğitmen/tarih/saat/süre seçin. Yönetici ve ilgili eğitmen takviminde mavi kart görünmeli; hak tüketmemeli. Çakışan saat reddedilmeli; iptalde kart kalkmalı, geçmiş kalmalı. |
| 12 | Eğitmenin üyeleri — Eğitmen → Üyelerim | Atanmış üyeleri arayın, detay/paket/kalan seans/takvim/ölçüm bağlantılarını açın. Başka eğitmenin üyesi doğrudan adresle de açılamamalı. |
| 13 | Esnek takvim — Yönetici → Takvim / Eğitmen → Takvimim | Gün/Hafta, Bugün, önceki/sonraki ve tarih seçimini deneyin. Geçmiş için “Tamamlanan dersleri göster” seçin. Kart detayında saat/not/geçmişi açın. Saat değişimi veya iptal sonrası tarih/görünüm/filtreler korunmalı. Toplu işlemde seçili ders / bu ve sonrası / tüm gelecek kapsamını seçin. |
| 14 | Haftalık Kitchen ürün tablosu — Üye → Beslenme → Haftalık Kitchen Planı | Aktif/seçili abonelikte ürün/öğün/adet ve teslimat seçimi görünmeli. Önceki/sonraki hafta, eski abonelik ve pas geçilen öğünü deneyin. Ödeme/plan bekleniyorsa açık durum mesajı çıkmalı; mobilde etiketli gün kartları olmalı. |
| 15 | Kitchen anlatımı — Ana sayfa → NO:23 Kitchen | “Öğünlerin hazır, günün sana kalsın.” açıklaması ve “Kitchen menünü oluştur” bağlantısı görünmeli, öğün oluşturma akışını açmalı. |

Kitchen paket oluşturma/düzenleme ekranındaki `0.01 is not a valid value for Decimal` hatası da önceki teslimatta düzeltildi. Türkçe ortamda kuruşlu bir fiyatı (ör. 3.000,25 ₺) kaydedip tekrar açın; form hata vermemeli.

## Ders hakkı düzeltmesi ve mevcut süre kuralı

**Üyeler → Ders hakları** kayıtlı bakiye ve paket dönemindeki son 200 dersi gösterir. Eski tamamlanmış/katılmadı dersler işlenmemişse, önceki manuel işlemi kontrol ederek “bir kez düş” veya “bakiyeyi koru” seçin. İptal grubunda açık kalan rezervasyonlar için “bir kez iade et” veya “bakiyeyi koru” bulunur. Yanlışlıkla yeniden gönderilen aynı form ikinci kez hak işlemez. Dönemi bilinmeyen kayıtlar otomatik düzeltilmez.

Kalan seans, henüz tamamlanmamış planlı dersleri de içerir. Grup rezervasyonunun kayıtlı bakiyeyi azaltması, dersin tamamlandığı anlamına gelmez; ortak özet bu ayrımı yapar. PT/grup toplam hakkı ortaktır; paket türünün ders türü/aylık sınırları ayrıca uygulanır.

Paketin bitiş tarihi gelince kullanılmamış haklar kayıttan silinmez. Yeni ders/rezervasyon için aktif paket koşulu sürer. Süre sonrası yeni kullanım veya süre uzatma konusunda yeni bir kural uygulanmadı.

## Doğrulama kaydı

- Son otomatik test turu: **664 başarılı, 0 başarısız, 0 atlanan**.
- Ayrı PostgreSQL 18'de son hak için 12 eşzamanlı kayıt yarışı: her birinde tek kazanan. Otomatik tamamlama tekrarında mükerrer hak düşmedi.
- Gerçek tarayıcıda yönetici/eğitmen/üye ortak hakları, eski düzeltme/iade, günlük saat değişimi/iptal, geçmiş ders ve mobil düzen doğrulandı. Önceki Kitchen/ödeme/talep doğrulamaları teslimat belgesinde kayıtlıdır.
- Testler sahte verilerle ayrı yerel veritabanında yapıldı; canlı üye veya gerçek ödeme kullanılmadı.
