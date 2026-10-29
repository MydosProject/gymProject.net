# NO23 test senaryoları — 29 Ekim 2026

İlk inceleme sürümü: `eee4bbf` (`no23-V8`). Üç doğrulanmış hatanın düzeltmesi sonrasında **60 senaryonun 7'si geçti, 0'ı başarısız, 53'ü çalıştırılmayı bekliyor.** Bekleyen senaryolar hata bulunduğu anlamına gelmez. İlk turdaki başarısız sonuçlar ve düzeltme kanıtları bulgu raporunda korunmuştur.

Güncel otomatik test paketi: **673 geçti, 0 başarısız, 0 atlanan**; paket başlangıcı için 9 regresyon senaryosu eklendi. Bu sayı aşağıdaki 60 kabul senaryosunun tamamının geçtiği anlamına gelmez. Mevcut testler ağırlıklı olarak servis/controller davranışını kontrol ediyor; ekran genişliği, CSS, gerçek oturum ve tarayıcı etkileşimi ayrıca test edilmeli.

Doğrulanmış sorunlar, tekrar üretme adımları ve yeniden test sonuçları: [QA bulguları](QA_BULGULARI_2026-10-29.md).

## Uygulama ve sonuç kuralları

- **P0:** Para, ders hakkı, yetki/veri kaybı. Başarısızsa yayın öncesi çözülmeli.
- **P1:** Temel işlemi engelleyen veya paket/kayıt kuralını bozan hata.
- **P2:** Okunabilirlik ve kullanım sorunu.
- **GEÇTİ / BAŞARISIZ:** Bu incelemede ayrı test ortamında adımlar uygulandı. **BEKLİYOR:** Kabul senaryosu henüz uygulanmadı; benzer bir otomatik testin bulunması bu durumu değiştirmez.
- Her denemede tarih, commit, tarayıcı/sürüm, ekran genişliği, hesap rolü, gerçek sonuç, ekran görüntüsü ve varsa hata kaydı tutulmalı. Başarısız satıra bulgu numarası eklenmeli.
- Gerçek para/üye kullanmadan ayrı test veritabanı ve ödeme sağlayıcısının test ortamı kullanılmalı. Üretim verisine bu senaryolar için toplu silme veya bakiye düzeltmesi uygulanmamalı.

## Ortam ve test verisi

**Tarayıcı:** Edge ve Chrome. Her sürüm önce masaüstü ve mobilde denenmeli. Genişlik matrisi: 390, 768, 991, 992, 1024, 1280, 1366, 1920 px. Masaüstünde ayrıca %125, %150 ve %200 yakınlaştırma. Bu incelemede yeni kabul kontrolleri 390/991/1024 px'de uygulandı; 29 Ekim önceki üye ekranı teslimatının yedi genişlik kontrolü ayrıca kayıtlıdır.

**Hesaplar:** Bir yönetici, iki farklı aktif antrenör, her birine atanmış üyeler ve yalnız Kitchen hesabı. Parolalar rapora yazılmaz.

**Üyeler:** A: 70 hak, 2 tamamlanan, 4 planlı; B: aynı ad/soyad, farklı telefon ve numara; C: yalnız 1 hakkı kalan; D: başlangıcı gelecekte; E: süresi bitmiş ama bakiyesi olan; F: eski manuel hak düzeltmesi bulunan.

**Takvim:** Tekil ders ve en az dört haftalık Pazartesi/Çarşamba/Cuma serisi; iptal ve tamamlanmış dersler; dolu grup; çakışan saat; üyesiz ön görüşme.

**Ödeme/Kitchen:** 37.500 ₺ katalog, 25.000 ₺ özel satış; 3 taksit; kısmi tahsilat; ödeme bekleyen/aktif/eski Kitchen abonelikleri; kayıtlı haftalık ürünler ve pas geçilen öğün.

## G — Görsel kullanım ve ekranlar (12)

| ID | Öncelik | Ekran / rol | Önkoşul ve adımlar | Beklenen sonuç | Durum |
|---|---|---|---|---|---|
| G-01 | P1 | Yönetici → Üyeler | Kayıtlı üyelerle 1024 px'de listeyi aç; sağdaki işlem düğmelerini kontrol et. | Düzenle, parola, ölçüm ve sil düğmelerinin tamamı kabın içinde; işlem sütunu sağda sabit. | GEÇTİ |
| G-02 | P1 | Yönetici → Üyeler | 991 px genişliğe geç; her kartın işlem alanını kontrol et. | Tablo kartlara dönüşür; düğmeler kırpılmaz ve üst üste binmez. | GEÇTİ |
| G-03 | P1 | Yönetici → Üyeler | 390 px'de karta aşağı kaydır; Tab ile parola bağlantısına ilerle. | Dört düğme tam görünür; odak erişilebilir; sayfa ekran genişliğini aşmaz. | GEÇTİ |
| G-04 | P1 | Yönetici listeleri | 1366 px'de %125/%150/%200 yakınlaştırma uygula; Üyeler, Siparişler ve Paketler'i gez. | İşlemler erişilebilir kalır; tablo kayar veya karta dönüşür; metin/kontrol kesilmez. | BEKLİYOR |
| G-05 | P2 | Yönetici → Üyeler | 1024 px'de tablo bölgesine klavyeyle odaklan; iki kez sağ ok, sonra sol ok kullan. | Tablo gerçekten yatay kayar; işlem sütunu görünür ve odak belirgindir. | GEÇTİ |
| G-06 | P1 | Üyeler / Paketler | 60 karakter ad, 80 karakter antrenör adı ve uzun paket/iletişim bilgisiyle 390/1024/1366 px'de aç. | Adın tam hali erişilebilir; seçim/kaydetme düğmeleri görünür; komşu hücreler bozulmaz. | BEKLİYOR |
| G-07 | P2 | Yönetici / Eğitmen listeleri | Hiç kayıt olmayan ve filtrede sonuç çıkmayan durumları aç. | Anlaşılır boş durum gösterilir; tablo/işlem alanı kırılmaz; var olmayan kayıt sayılmaz. | BEKLİYOR |
| G-08 | P2 | Yönetici → Randevu Talepleri | Tek randevuyu 390 px'de aç; tercih edilen zaman ile planlanan zamanı ayırt et. | Hizmet, not, durum, tercih ve kesin plan alanlarının etiketleri görünür. | GEÇTİ |
| G-09 | P1 | Yönetici → Ölçüm ve Gelişim; Eğitmen → Ölçümler | Üç tarihte 80/78/76 kg ölçüm girilmiş üyeyi 390 px'de aç; karşılaştırma ve geçmişi incele. | Her sayının ölçüm türü, tarihi ve ilk/önceki/son/değişim etiketi okunur. | GEÇTİ |
| G-10 | P1 | Ölçümde üye arama | 20 benzer sonuç, uzun adlar; alt sıradaki sonucu fare ve ok/Enter ile seç; Escape dene. | Liste kesilmeden kayar; telefon/#numara görünür; seçili ID doğrudur; kapanış/odak çalışır. | BEKLİYOR |
| G-11 | P1 | Yönetici / Eğitmen → Takvim | 390 px'de Hafta'da iki güne bak, diğer günlere kaydır; Gün'e geçip detay aç. | Haftada iki gün; günde tek ajanda; saat, not, seçim ve kaydet düğmeleri erişilebilir. | BEKLİYOR |
| G-12 | P1 | Üye/ödeme/ölçüm formları | Boş veya geçersiz alanlarla gönder; uzun hata metni ve %200 yakınlaştırma kullan. | Hata doğru alanla ilişkilidir; kaydedilmeyen veri korunur; hata/kaydet düğmesi ekrandan taşmaz. | BEKLİYOR |

**Otomasyon açığı:** G senaryoları için gerçek tarayıcı/geometri kontrolleri gerekir. C# servis testlerinin geçmesi bu koşulları doğrulamaz.

## K — Kayıt, paket ve hesap (8)

| ID | Öncelik | Ekran / rol | Önkoşul ve adımlar | Beklenen sonuç | Durum |
|---|---|---|---|---|---|
| K-01 | P1 | Spor kaydı / ziyaretçi | Paket veya süre seçmeden kaydı gönder; sonra geçerli paket/süreyle tekrar dene. | Eksik seçim reddedilir; geçerli seçim doğru süre ve ders haklarıyla kayıt olur. | BEKLİYOR |
| K-02 | P1 | Kitchen → öğün seçimi → kayıt | Öğün ve teslimat seçip Kitchen kayıt akışını tamamla. | Spor paketi sorulmaz; spor hakkı oluşmaz; aynı öğün/kalori/teslimatla ödeme özetine dönülür. | BEKLİYOR |
| K-03 | P1 | Kayıt formu | Geçersiz parola ile gönder; alanları düzeltip yeniden kaydet; tarayıcı geri tuşunu dene. | Akış, seçili paket/öğünler ve yerel dönüş adresi korunur; iki hesap oluşmaz. | BEKLİYOR |
| K-04 | P1 | Üye kaydı / yönetici | Doğum tarihini boş, gelecekte ve geçerli bir tarih olarak dene. | Yeni kayıtta tarih zorunlu; gelecek tarih reddedilir; geçerli tarih kaydedilip tekrar görünür. | BEKLİYOR |
| K-05 | P1 | Parola doğrulama | Diğer kuralları sağlayan, yalnız bir büyük harf içeren parola kullan; Türkçe büyük harfle de dene. | Bir büyük harf yeterli olur; eksik diğer kurallar açıklanır; form ile sunucu tutarlıdır. | BEKLİYOR |
| K-06 | P1 | Grup rezervasyonu / üye | Paket başlangıcı 03.11.2026 olan, 70 haklı üye için 31.10.2026 grup dersine rezervasyon isteği gönder. | Paket dönemi dışındaki ders reddedilir; rezervasyon açılmaz, 70 hak korunur. | GEÇTİ |
| K-07 | P1 | Yönetici → Geçici parola yenile | Test üyesine iki kez geçici parola üret; eski ve son parolayla girişleri dene. | Yeniden üretim mümkündür; yalnız son parola geçerlidir; başka üyenin hesabı etkilenmez. | BEKLİYOR |
| K-08 | P0 | Kayıt / paket seçimi | İstek içinde başka pakete ait, pasif veya silinmiş varyant ID'si gönder; Kitchen akışına spor varyantı ekle. | Sunucu uyumsuz seçimi reddeder veya Kitchen'de spor seçimini yok sayar; fazladan hak oluşmaz. | BEKLİYOR |

**Kısmi otomasyon:** `MemberRegistrationFlowTests`, `RegistrationPackageCatalogTests`, `MemberBirthDateTests`, `MemberTemporaryPasswordTests`, `MemberAccessAndLookupTests`. K-06 artık `ClassSessionFlowTests` içinde başlangıç/bitiş sınırları, sınırsız, başlangıcı bilinmeyen eski kayıt ve Kitchen hesabıyla kapsanıyor. Düzeltme öncesinde yeni testler iki hatayı üretti; düzeltme sonrasında hepsi geçti. Gerçek PostgreSQL'de dönem öncesi ret ve başlangıca eşit tarihte kabul ayrıca doğrulandı.

## T — Takvim, ders ve haklar (14)

| ID | Öncelik | Ekran / rol | Önkoşul ve adımlar | Beklenen sonuç | Durum |
|---|---|---|---|---|---|
| T-01 | P0 | Haftalık birebir program / yönetici | 70 haklı, mevcut programı olan üyeye haftada 3 gün seç; başlangıç haftasını değiştir; önizle ve kaydet. | Toplam hak sabit; yalnız yeni planlanabilir hak kadar ders; önizleme ve kayıt aynı sınırı kullanır. | BEKLİYOR |
| T-02 | P0 | Üye / Yönetici / Eğitmen panelleri | A üyesini üç panelde aç; farklı hafta/aylara git; sayfaları yenile. | 70 toplam, 2 kullanılan, 68 kalan, 4 planlı, 64 yeni planlanabilir aynı; tarih gezinmesi hak değiştirmez. | BEKLİYOR |
| T-03 | P0 | Grup / birebir rezervasyonu | Son 1 hakkı birebir ders için ayrılmış C üyesiyle gruba katıl; sonra birebiri iptal edip tekrar dene. | İlk grup reddedilir; iptal sonrası bir grup kabul edilir; aynı hak iki kez planlanmaz. | BEKLİYOR |
| T-04 | P0 | Otomatik ve manuel tamamlama | Süresi biten planlı dersi tamamlayıcıya işlettir; aynı dersi manuel ve ikinci otomatik turda yeniden tamamlat. | Hak ve geçmiş bir kez işlenir; ikinci giriş ek düşüm yapmaz. | BEKLİYOR |
| T-05 | P0 | Birebir → Katılmadı | Geçmiş planlı birebiri Katılmadı yap; tekrar gönder; otomatik tamamlamayı çalıştır. | Tamamlandı ile aynı bir hak düşer; durum/geçmiş korunur; mükerrer düşüm olmaz. | BEKLİYOR |
| T-06 | P0 | Ders / rezervasyon iptali | Planlı birebir ve rezervasyonlu grubu iptal et; formu yeniden gönder. | İptal kartı kalkar; hak bir kez serbest kalır/iade edilir; tekrar iade yapılmaz. | BEKLİYOR |
| T-07 | P1 | Grup dersleri / yönetici | Grubu pasife al, takvimleri kontrol et, yeniden aktife al. | İptal seanslar geri gelmez; aktiflik değişimi eski rezervasyonları kendiliğinden açmaz. | BEKLİYOR |
| T-08 | P0 | Toplu ders işlemleri | Aynı seri ve farklı seri kur; ayrı ayrı seçili ders / bu ve sonrası / tüm gelecek kapsamıyla güncelle ve sil. | Yalnız doğru seri/tarih kapsamı değişir; geçmiş sonuçlar ve diğer üyeler korunur; grup iadesi bir kez olur. | BEKLİYOR |
| T-09 | P0 | Toplu / tekil takvim değişimi | Birebir, grup veya ön görüşmeyle çakışan saate bir toplu programı taşı. | Anlaşılır hata; hiçbir ders kısmen taşınmaz; hak ve geçmiş tutarlı kalır. | BEKLİYOR |
| T-10 | P1 | Geçmiş dersler | Tamamlanmış dersin bulunduğu geçmiş güne git; Tamamlananları göster'i aç/kapat. | Tamamlanan kartlar varsayılan gizli, açınca detay/geçmişle görünür; iptaller geri gelmez. | BEKLİYOR |
| T-11 | P1 | Gün / hafta gezinmesi | Üye/antrenör ve tamamlanan filtresi seç; Gün'de saat değiştir veya iptal et; Bugün ve ileri/geri kullan. | Kayıt sonrası görünüm/tarih/filtreler korunur; gezinme uygun gün/haftaya ilerler. | BEKLİYOR |
| T-12 | P1 | Ön görüşme planlama / yönetici | Hesabı olmayan kişi için randevu planla, saat değiştir, tamamla veya iptal et. | İlgili iki takvimde mavi kart; üye/ders hakkı gerekmez; iptalde kart kalkar; geçmiş kalır. | BEKLİYOR |
| T-13 | P0 | Yönetici → Ders hakları | Eski işlenmemiş ders ve iptal rezervasyonunda düş/iade et ile bakiyeyi koru yöntemlerini ayrı verilerle dene; eski formu tekrar gönder. | Yalnız seçili yöntem uygulanır; eski form reddedilir; otomatik tahminle ikinci düşüm/iade yapılmaz. | BEKLİYOR |
| T-14 | P1 | Paket bitişi / ders süresi | Bitişten önce, tam bitişte ve sonra ders planla; süre dolunca kalan bakiyeyi aç. | Ders paket dönemine uyar; süresi bitmiş pakette yeni rezervasyon reddedilir; kalan kayıt sıfırlanmaz. | BEKLİYOR |

**Kısmi otomasyon:** `WeeklyPersonalTrainingPlanTests`, `LessonCreditSummaryTests`, `SessionCompletionServiceTests`, `CalendarBulkOperationTests`, `CalendarRecurrenceOperationTests`, `CalendarCompletedVisibilityTests`, `CalendarNavigationTests`, `AppointmentSchedulingTests`, `GroupClassRemovalServiceTests`. Gerçek PostgreSQL eşzamanlılık ve tarayıcı kullanımı ayrıca çalıştırılmalı.

## O — Ödeme, indirim ve taksit (10)

| ID | Öncelik | Ekran / rol | Önkoşul ve adımlar | Beklenen sonuç | Durum |
|---|---|---|---|---|---|
| O-01 | P0 | Yönetici → Üye/paket satışı | Spor dahil her kategoride nakit/havale/kart yöntemiyle test satış kaydı oluştur. | Yönetici yöntemi kaydedilir; sipariş/borç doğru; tahsil edilmemiş tutar alınmış sayılmaz. | BEKLİYOR |
| O-02 | P0 | Yönetici → Ana satış tutarı | 37.500 ₺ katalog paketine 25.000 ₺ özel tutar gir; yüzde indirimle beraber de dene. | Satış 25.000, indirim 12.500; doğrudan tutar öncelikli; paket süresi ve hak değişmez. | BEKLİYOR |
| O-03 | P0 | Taksit planı | 25.000 ₺'yi 3 taksite böl; 31 Ocak vadesini, kuruş farkını ve yanlış toplamı dene. | Son taksit yuvarlamayı düzeltir; toplam tam 25.000; ay sonları geçerli; yanlış toplam reddedilir. | BEKLİYOR |
| O-04 | P0 | Fiyat alanları / Kitchen paket formu | Türkçe, İngilizce ve Almanca kültürde 3000,25 / 3000.25 ve sınır tutarlarını gir. | Geçerli tutar 3.000,25 ₺ olarak korunur; Range/Decimal hatası veya 100 kat tutar oluşmaz. | BEKLİYOR |
| O-05 | P0 | Üyeler → Ödeme geçmişi | Farklı günlerde, aynı günde iki kez kısmi tahsilat kaydet. | Net alınan ve borç doğru; son günün toplamı yalnız o günkü tutarlar; tarih yoksa tahmin edilmez. | BEKLİYOR |
| O-06 | P0 | Taksit tahsilatı | Aynı taksit formunu çift tıkla veya aynı isteği tekrar gönder; sonra listeyi yenile. | Tek tahsilat etkisi; alınan tutar iki katına çıkmaz; tutarlı sonuç mesajı. | BEKLİYOR |
| O-07 | P0 | Sipariş iptal / iade | Ödenmiş, kısmi ödenmiş ve bekleyen siparişleri ayrı ayrı iptal/iade akışından geçir. | İptal para iadesi sayılmaz; doğrulanmış iade net tahsilattan çıkar; geçmiş korunur. | BEKLİYOR |
| O-08 | P0 | Online ödeme sonucu | Test sağlayıcısının aynı başarılı callback'ini iki kez gönder; sırasını değiştir. | Tek ödeme/aktivasyon/stok etkisi; sipariş ve paket ikinci kez oluşturulmaz. | BEKLİYOR |
| O-09 | P0 | Ödeme başarısız / süresi dolmuş | Test ödemesini reddet, beklet veya süresini doldur; geri dönüp durumu yenile. | Ödenmemiş paket aktifleşmez; ayrılmış stok/hak ilgili kurala göre bir kez çözülür. | BEKLİYOR |
| O-10 | P0 | Eski ödeme formu | Aynı kaydı iki oturumda aç; birinde değiştir, diğerinden eski tutar/vade ile gönder. | Güncel kayıt ezilmez; tutarlı hata/yenileme akışı; bakiye ve taksit toplamı korunur. | BEKLİYOR |

**Kısmi otomasyon:** `ManualOrderPaymentServiceTests`, `ManualPaymentRecoveryTests`, `MemberPaymentHistoryTests`, `MoneyAmountModelBinderTests`, `KitchenSubscriptionPackageValidationTests`, `IyzicoPaymentServiceTests`, `IyzicoPendingPaymentServiceTests`, `OfflineCheckoutTests`. Form ve sağlayıcı test ortamı kabulü ayrı değerlendirilir.

## N — Kitchen akışı ve operasyon (8)

| ID | Öncelik | Ekran / rol | Önkoşul ve adımlar | Beklenen sonuç | Durum |
|---|---|---|---|---|---|
| N-01 | P1 | Kalori → öğün → kayıt → ödeme | Kalori/öğün/teslimat seç; kayıt hatası üret; düzeltip devam et. | Aynı seçim ve toplam korunur; spor kaydına dönüşmez; ikinci hesap gerekmez. | BEKLİYOR |
| N-02 | P0 | Teslimat özeti | Aynı pakette salondan teslim → adrese teslim → salondan teslim değiştir; ilk açılışı da kontrol et. | Kurye ücreti yalnız adrese teslimde; ilk özet ile seçim sonrası hesap aynı. | BEKLİYOR |
| N-03 | P1 | Üye → Haftalık Kitchen Planı | Aktif abonelik ve kayıtlı ürün planını aç; uzun ürün adı/adet kullan. | Doğru haftanın kayıtlı ad/adetleri ve öğünleri gösterilir; mobilde etiketli gün kartları okunur. | BEKLİYOR |
| N-04 | P1 | Haftalık Kitchen / geçmiş | Eski abonelik, önceki/sonraki hafta ve yıl sınırında tarih seç. | Yalnız seçili abonelik/hafta görünür; geçmiş veri ve bakiye değişmez; dönem dışı günler bellidir. | BEKLİYOR |
| N-05 | P0 | Öğün/gün pas geçme | Planlı haftada tek öğünü ve bir tam günü pas geç; tekrar gönder. | Porsiyon/kalori ve üretim ihtiyacı doğru değişir; ikinci kez azaltılmaz. | BEKLİYOR |
| N-06 | P1 | Ödeme/plan bekleyen Kitchen | Ödenmemiş, planı hazırlanmamış veya üretimi başarısız aboneliği aç. | Doğru bekleme bilgisi; uydurma ürün/teslimat sözü veya geçmiş tarihe otomatik teslim edildi işareti yok. | BEKLİYOR |
| N-07 | P0 | Alerjen ve plan eşleştirme | Test profiline alerjen ekle; eşleşen ürünü içeren veya uygun ürün bulamayan plan üret. | Sakıncalı ürün önerilmez; uygun plan yoksa açık sonuç; sessizce uygunsuz öğüne geçilmez. | BEKLİYOR |
| N-08 | P0 | Yönetici → Kitchen Stok | Aynı üretim planını iki kez/eşzamanlı tamamla; reçete eksik ve yetersiz stokla da dene. | Stok bir kez düşer; eksik reçete/stok anlaşılır biçimde ele alınır; kısmi bozuk plan kalmaz. | BEKLİYOR |

**Kısmi otomasyon:** `KitchenWeeklyPlanTests`, `KitchenMealSelectionTests`, `KitchenOrderCustomizationTests`, `KitchenAllergenSafetyTests`, `KitchenProductionPlanningServiceTests`, `KitchenProductionPlanCalculatorTests`, `MemberAccessAndLookupTests`. Fiziksel teslimat bu yazılım testinin dışında tutulur.

## Y — Yetki, eşzamanlılık ve dayanıklılık (8)

| ID | Öncelik | Ekran / rol | Önkoşul ve adımlar | Beklenen sonuç | Durum |
|---|---|---|---|---|---|
| Y-01 | P0 | Eğitmen → üye/ölçüm/takvim | Diğer eğitmene ait üyenin ID'siyle arama, detay, güncelleme ve toplu işlem isteği gönder. | Yabancı üye verisi ve hakkı açılmaz/değişmez; aktif antrenör bağlantısı olmayan hesap engellenir. | BEKLİYOR |
| Y-02 | P0 | Gelişim fotoğrafları | Yönetici, ilgili üye, atanmış eğitmen ve yabancı hesapla aynı resim adresini aç. | Yalnız yetkili roller erişir; tahmin edilen ID veya eski bağlantı özel fotoğrafı açmaz. | BEKLİYOR |
| Y-03 | P0 | Yönetici adresleri | Üye/eğitmen/oturumsuz hesapla yönetici sayfasını ve işlem adresini doğrudan çağır. | Yönetici yetkisi zorunlu; sadece menüyü gizlemekle yetinilmez. | BEKLİYOR |
| Y-04 | P0 | Kayıt değiştiren POST'lar | Test isteğinden antiforgery token'ını kaldır veya yanlış token gönder. | İstek reddedilir; ders/ödeme/üye kaydı değişmez; sunucu çökmez. | BEKLİYOR |
| Y-05 | P1 | Oturumu bitmiş form / arama | Form ve üye arama açıkken oturumu bitir; arama veya kaydetmeyi dene. | Giriş gereksinimi anlaşılır; HTML giriş sayfası JSON sonuç sanılmaz; sessiz başarı gösterilmez. | BEKLİYOR |
| Y-06 | P1 | Otomatik ders tamamlama | Ayrı test DB bağlantısını geçici kes; worker turunu bekle; bağlantıyı geri getir. | Hata sebebi loglanır; web süreci ayakta kalır; sonraki turda işleme devam edilir. | BEKLİYOR |
| Y-07 | P0 | Son hak / son kontenjan | Grup–grup, grup–birebir ve farklı eğitmenlerde birebir–birebir istekleri eşzamanlı gönder. | Son hak/kontenjan için tek kazanan; kaybeden anlaşılır hata alır; eksi/çifte bakiye oluşmaz. | BEKLİYOR |
| Y-08 | P1 | Büyük veri / Üyeler ve Takvim | Ayrı DB'de 1000 üye ve 10.000 ders oluştur; beş tekrar yükleme/filtre değişimi ölç. | Süre ve veri miktarı kayıt altına alınır; donma/kırpılma yok; sayfalama/yükleme bütçesi belirlenir. | BEKLİYOR |

**Kısmi otomasyon:** `MemberAccessAndLookupTests`, `MemberProgressTrackingServiceTests`, `CalendarRecurrenceOperationTests`, `LessonCreditSummaryTests`, `SessionCompletionServiceTests`. Gerçek HTTP yetki/antiforgery, DB kesintisi ve yük testleri mevcut controller birim testlerinden ayrı çalıştırılmalı. Y-08 için sayısal performans hedefi referans ortam ve işletme beklentisiyle testten önce netleştirilmeli.

## Önerilen ilk test turu ve düzeltme sırası

1. K-06 kapatıldı: Paket başlangıcı dışındaki grup rezervasyonu reddediliyor; başlangıca eşit tarihteki rezervasyon çalışıyor.
2. G-09 kapatıldı: Yönetici/eğitmen ölçüm karşılaştırması ve geçmiş etiketleri 390/768/1366 px'de doğrulandı.
3. G-08 kapatıldı: Randevu kartı etiketleri ve durum kaydetme doğrulandı. Aynı ortak stili kullanan diğer ödeme/talep listelerinin kapsamlı kabulü bekliyor.
4. T-02/T-03/T-04/T-06/T-08 ve O-02/O-03/O-06: Hak/para değişimlerinin pozitif, negatif ve tekrar istek kontrolleri.
5. Y-01/Y-02/Y-03/Y-04/Y-07: Yetki ve eşzamanlılık.
6. G-04/G-06/G-10 ve N grubu: Kullanım, sınır veri ve Kitchen kabulü.

Her hata düzeltmesinde ilgili vaka önce hatayı üretmeli, sonra düzeltmeden sonra geçmeli. Paket başlangıcı için servis ve gerçek veritabanı testi; etiket/taşma için gerçek tarayıcı testi eklenmesi gerekir. Başlık kaybını yakalamak için yalnız sayfa genişliği kontrolü yeterli değildir: görünür her değerin etiketi de denetlenmelidir.
