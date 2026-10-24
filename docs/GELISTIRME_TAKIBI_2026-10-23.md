# 23 Ekim geliştirme takibi

Maddeler ayrı kabul kriterleriyle ilerletilir. Bir maddenin burada sıraya alınması, geliştirmesinin tamamlandığı anlamına gelmez.

24 Ekim güncellemesi ve yayınlama notları: [Güncel teslimat](TESLIMAT_2026-10-24.md). Kullanıcının son kararıyla **Kitchen'de öğün seçip gelen kişi yalnızca Kitchen hesabı açar; spor paketinden gelen kayıtta paket/süre zorunludur**. Ana satış tutarını yönetici belirleyebilir.

## İlk teslimat

| Madde | Durum | Yapılan iş / kabul kriteri |
|---|---|---|
| Kitchen paket formunun açılırken hata vermesi | Tamamlandı | Decimal Range sınırları kültürden bağımsız okunur. Dört fiyat alanı kuruş hassasiyetli sayı girişidir; para bağlayıcısı nokta/virgülü doğru okur. Oluşturma ve düzenlemenin ortak formu düzeltilmiştir. |
| Pasiften aktife alınan grubun iptal edilmiş dersleri | Tamamlandı | Yönetici ve eğitmen takvimleri iptal durumunu dışlar. Aynı filtre birebir dersler için de uygulanır. Geçmiş kayıt ve hak iadesi korunur. Aktifleştirme, iptal edilmiş seansları tekrar rezerve etmez. |
| PRO kartı | Tamamlandı | Ortak koyu görünüm, yıldızlı “En Çok Tercih Edilen” rozeti; ana sayfa ve paket sayfasında aynı ortak kart kullanılır. |
| Yönetici ödeme yöntemi ve özel tutar | Tamamlandı | Kullanıcının onayıyla spor üyelikleri dahil tüm paketlerde yönetici nakit/havale/kart seçebilir. Ek indirim yüzdesi veya doğrudan anlaşılan satış tutarı girilir. Doğrudan tutar önceliklidir; açıklama, liste bedeli, indirim ve kaydeden yönetici saklanır. Taksitlerin toplamı satış tutarına eşit olmalıdır. |
| Üyeler ekranında ödeme geçmişi | Tamamlandı | Üye listesinde tahsilat, kalan borç, son kayıtlı tahsilat günü ve gün toplamı görünür. Ayrı detay ekranında siparişler, indirim, yöntem, vade, tutar, tahsilat tarihi ve referanslar listelenir. Eski kayıtta tarih yoksa kayıt tarihinden tahmin yapılmaz. İptal borcu kapatır; tek başına para iadesi sayılmaz. İade edilmiş ödemeler net tahsilattan dışlanır. |
| Mobilde iki gün | Tamamlandı | Yönetici ve eğitmen haftalık takvimlerinde iki sütun ve yatay kaydırma. Masaüstünde yedi gün korunur. |
| Ana sayfa Kitchen anlatımı | Tamamlandı | “Öğünlerin hazır, günün sana kalsın.” başlığı, hazırlık yükünü hafifleten açıklama ve “Kitchen menünü oluştur” çağrısı. |

Ödeme kuralı: bu teslimatta yöneticinin manuel kayıt akışı değiştirilmiştir. Halka açık online ödeme akışı kendi katalog ve ödeme kontrollerini kullanmaya devam eder. Gerçek POS/banka tahsilatı gerçekleştirilmez; yönetici tahsilat kayıtlarını işler.

## İkinci teslimat: Kitchen kaydı ve üye erişimi

| Madde | Durum | Kabul kriteri |
|---|---|---|
| Kitchen ve spor kayıt akışlarının ayrılması | Tamamlandı, 24 Ekim son karar | Öğünlerini seçen Kitchen müşterisi son adımda hesap açıp aynı öğün, kalori hedefi ve teslimat tercihiyle ödemeye döner; spor paketi/süresi ve ders hakkı verilmez. Spor kayıt ekranında yayınlanmış paket ve aktif süre zorunludur. İki akış aynı hesap altyapısını kullanır; mevcut hesapla giriş de korunur. Kitchen kaydı spor kataloğunun açık olmasına bağlı değildir. |
| Ölçüm ve gelişimde üye arama | Tamamlandı | Yönetici ve eğitmende ortak arama alanı. En az 2 karakter, en fazla 20 sonuç; Türkçe ad/soyad, telefon ve #üye numarasıyla arama. İsim, telefon, numara ve paket gösterilir. Seçim ID ile; klavye desteği ve istek iptali vardır. Antrenör yalnızca atanmış üyelerini arar; seçili üye haricinde tüm üyeler sayfaya yüklenmez. |
| Antrenörün üye listesi | Tamamlandı | Menüde “Üyelerim”; atanmış üyelerde arama ve sayfalama. Listede ve detayda paket, mevcut kalan hak, iletişim, tarihler, takvim ve ölçüm bağlantıları. Başka antrenörün üyesi ve atanmamış üye için detay/ölçüm/kayıt işlemi bulunamadı sonucu verir. Aktif antrenör bağlantısı olmayan hesaplar engellenir. |

Kitchen devam akışının ek düzeltmesi: salondan teslim seçiliyken ödeme özetinin ilk açılışta kurye ücretini göstermesi giderildi. İlk özet ve teslimat seçimi değişince gösterilen etiket/tutar aynı yöntemi kullanır. Tahsilat kuralları değiştirilmedi.

KitchenOnly satışa açık bir spor seviyesi değildir. Uygulamanın başlangıç veri hazırlığında eklenir, spor seviyesi düzenleme ve katalog bağlama ekranlarından dışlanır. Yeni kolon veya şema migration'ı yoktur. Sürüm geri alınırken eski kodun bu yeni hesap türünü tanımadığı dikkate alınmalıdır.

### İkinci teslimat doğrulaması

- Toplam 580 test başarılı (önceki 561 teste 19 yeni senaryo eklendi).
- Yeni kayıt, hizmet seçimi, aynı isimli üyelerin ayrımı, arama sınırları ve antrenör yetki kontrolleri test edildi. Spor paketlerinde mevcut kayıt testleri de korunur.
- Ayrı PostgreSQL 18 veritabanında iki Kitchen hesabı açıldı: spor varyantı ve spor tarihleri boş, kalan ders hakkı sıfır. Genel spor kataloğu etkilenmedi.
- Tarayıcıda kalori hesabı → 5 günlük Kitchen planı → kayıt → Kitchen paket ekranına devam akışı doğrulandı; 2270 kcal hedefi ve seçilen öğünler korundu. Gerçek tahsilat yapılmadı.
- Türkçe “ışıl” aramasında yönetici aynı isimli iki üyeyi farklı numara/telefonlarla gördü; antrenör yalnızca kendi üyesini gördü. Klavyeyle seçimden ölçüm ekranına geçildi. Üyelerim detayında kayıtlı 17 seans gösterildi.
- Görsel kontrol sırasında arama alanının odak rengi ve antrenör başlıklarının kontrastı düzeltildi.
- Teslimat özeti ilk açılışta salondan teslim için 3.000 ₺; adrese teslim seçilince 3.475 ₺; tekrar salondan teslimde 3.000 ₺ gösterdi.

## Üçüncü teslimat: Talepler ve takvim

| Madde | Durum | Kabul kriteri |
|---|---|---|
| Genel bakışta yeni talepler | Tamamlandı | Misafir randevu, paket başvurusu ve üye PT talepleri üstte ayrı sayaçlarla görünür. Son 6 bekleyen kayıt zaman sırasıyla listelenir; sayaçlar tüm bekleyenleri sayar. Tür bazında tüm taleplere ve misafir talebinden planlamaya bağlantı vardır. |
| Üye olmayan kişinin ön görüşmesi | Tamamlandı | Yönetici tarih/saat/antrenör/süre ile planlar. Tercih edilen saat tek başına rezervasyon değildir. Yönetici ve atanmış eğitmen takviminde mavi “Ön görüşme” kartı görünür; ders hakkı tüketmez. Çakışma iki yönde ve eşzamanlı kayıtlar için kontrol edilir. Gün/saat/antrenör değişimi, iptal ve işlem geçmişi vardır; iptal kartı takvimden kaldırır. Eğitmen başka eğitmenin görüşmesini göremez. |
| Google Takvim benzeri esneklik | Tamamlandı | Yönetici/eğitmende gün/hafta görünümü, Bugün, ileri/geri gezinme, tarih seçimi, tamamlanmış ders geçmişi ve kart detayları. Gün başlığı o güne odaklanır; işlem sonrası tarih/görünüm/filtre korunur. Mevcut tek ders/tekrar/toplu işlem kapsamları kullanılabilir. |

## Dördüncü teslimat: Kalan seans ve haftalık Kitchen planı

| Madde | Durum | Kabul kriteri |
|---|---|---|
| Ders düşümü ve kalan seans kontrolü | Tamamlandı | Birebir tamamlandı/katılmadı bir hak tüketir; grup rezervasyonu hak ayırır, tamamlanınca ikinci kez düşmez; iptal iade eder. Son hakkın eşzamanlı grup/birebir kaydıyla iki kez kullanılması engellendi. Eski işlenmemiş dersler ve iptal rezervasyonları Üyeler → Ders hakları ekranından önceki manuel işlem korunarak veya tek kez bakiye düzeltilerek denetlenir. |
| Üye ekranında kalan seans | Tamamlandı | Üye genel bakışı, yönetici üye listesi/takvimi ve eğitmen üye listesi/detayı/takvimi ortak özeti kullanır: paket toplamı, tamamlanan, kalan, planlanmış ve yeni planlanabilir seans. Hafta değişince bakiye değişmez. Süre dolması bakiyeyi sıfırlamaz; yeni ders için aktif paket şartı korunur. |
| Haftalık Kitchen tablosu | Tamamlandı | Beslenme → Haftalık Kitchen Planı, sayfanın üstünde Pazartesi–Pazar ürün/öğün/adet ve teslimat tercihi tablosunu açar. Önceki/sonraki hafta, tarih seçimi ve eski paket seçimi vardır. Bugün, pas geçilen öğün/gün ve paket dışı günler ayrılır. Ödeme veya plan hazırlanması bekleniyorsa ürün teslimatı vaat edilmez. Mobilde gün kartlarına dönüşür. Başka üyeye ait abonelik açılamaz. |
| Üyeler ekranında kesilen antrenör adları | Tamamlandı | Atanmış antrenör adı tam ve satıra bölünebilir biçimde gösterilir; seçim alanı genişletildi, Kaydet alt satıra alındı. İşlem düğmeleri düzenlendi; dar ekranlarda kart görünümü ve koyu arka plan korunur. |

## İlk teslimat doğrulaması

- 561 test başarılı. Kitchen doğrulama adaptörü Türkçe, İngilizce ve Almanca kültürlerde fiyat sınırlarıyla test edilir.
- Grubu pasife alıp tekrar aktifleştirme; iptal seanslarının iki takvim görünümünde de gizli kalması ve hak iadesinin korunması test edildi.
- Tüm paketlerde yönetici nakit/havale, özel fiyat ve indirim, taksit tutarı eşitliği test edildi. Önceki “üyelikte nakit reddedilir” testleri yeni onaylanmış yönetici kuralına göre güncellendi.
- Ayrı PostgreSQL 18 veritabanında gerçek form: 37.500 ₺ katalog bedelli üyelik, nakit 25.000 ₺ özel satış, iki taksit; ilk tahsilat 12.500 ₺ ve kalan 12.500 ₺. Üye listesi ve ödeme geçmişi doğrulandı.
- Gerçek Kitchen formunda 3.000,25 ₺ fiyat kuruşuyla kaydedildi.
- Masaüstü PRO görünümü ve mobil iki günlük takvim tarayıcıda kontrol edildi.

Bu teslimatta şema değişikliği yoktur. Yalnızca sahte üye ve ayrı test veritabanı kullanılmıştır. Git'e gönderim, canlı yayınlama doğrulaması değildir.

Son teslimatta tüm listedeki geliştirmeler uygulanmıştır. Ekranlar ve kısa kabul senaryoları: [Kabul testleri](KABUL_TESTLERI_2026-10-24.md). Önceki doğrulama sayıları ilgili teslimata aittir; güncel toplam ve PostgreSQL/tarayıcı sonuçları güncel teslimat belgesindedir.
