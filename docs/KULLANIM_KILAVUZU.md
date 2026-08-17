# NO23 Sports Club Kullanım Kılavuzu

## 1. Amaç ve kullanıcı rolleri

NO23; spor kulübü üyeliği, grup dersi rezervasyonu, birebir antrenman, beslenme planı, Kitchen siparişleri, mağaza, topluluk etkinlikleri ve içerik yönetimini tek uygulamada toplar.

| Rol | Temel amaç | Erişim alanı |
|---|---|---|
| Ziyaretçi | Kulübü, paketleri ve hizmetleri incelemek; kayıt olmak | Halka açık site |
| Üye | Ders, PT, beslenme, sipariş ve kişisel ilerleme süreçlerini yönetmek | Üye paneli |
| Eğitmen | Kendisine gelen PT taleplerini, derslerini ve üye mesajlarını yönetmek | Eğitmen paneli |
| Yönetici | Ürün, sipariş, Kitchen, içerik, üye, eğitmen ve ders operasyonlarını yönetmek | Admin paneli |

## 2. Sisteme giriş

- Uygulama yerelde `http://localhost:5044` adresinde çalışır.
- Giriş: `/Identity/Account/Login`
- Kayıt: `/Identity/Account/Register`
- Şifremi unuttum: giriş ekranındaki parola sıfırlama bağlantısı.
- Admin paneli: `/Admin/Dashboard`
- Üye paneli: `/Member/Home`
- Eğitmen paneli: `/Trainer/Dashboard`

Kullanıcı giriş yaptıktan sonra rolüne uygun panel bağlantısı üst menüde görünür. Yetkisiz bir role ait adres açılırsa erişim reddedilir.

## 3. Halka açık site

### Hakkımızda

Kulübün yaklaşımını ve marka anlatısını açıklar. Yeni ziyaretçiye NO23'ün ne sunduğunu anlatmak için kullanılır.

### Hizmetler

Ana sayfadaki hizmetler bölümüne gider. Antrenman, beslenme ve topluluk değer önerilerini topluca gösterir.

### Kitchen

Aktif Kitchen menü ürünlerini gösterir. Ziyaretçi ürün ayrıntısını ve fiyatı inceleyebilir, tek seferlik sipariş akışına geçebilir.

### Shop

Aktif mağaza ürünlerini gösterir. Ürün seçimi, sipariş bilgileri ve ödeme/teslimat akışı burada başlar.

### Community

- **Etkinlikler – Challenge'lar:** Planlanan etkinlikleri ve challenge'ları listeler; ayrıntı sayfalarını açar.
- **Blog:** Yayındaki blog yazılarını listeler ve yazı detayını gösterir.
- **Başarı Hikâyeleri:** Yayındaki üye başarı hikâyelerini gösterir.

### Dersler ve eğitmenler

Halka açık ders programı, kapasite ve kalan kontenjan bilgilerini; eğitmen sayfası ise aktif eğitmenleri ve verdikleri dersleri tanıtır.

### Kayıt

Kullanıcı e-posta, parola ve üyelik paketi seçerek üye hesabı oluşturur. Seçilen paket daha sonra ders limiti, topluluk ve PT haklarının belirlenmesinde kullanılır.

## 4. Üye paneli

### Genel Bakış

Üyenin günlük özet ekranıdır. Üyelik paketini, yaklaşan rezervasyonları ve uygun dersleri gösterir. Uygun bir seansa hızlı rezervasyon yapılabilir; mevcut rezervasyon iptal edilebilir.

### Ders Programı

Aktif grup derslerini ve planlanan seansları gösterir. Dersin eğitmeni, seviyesi, saati, kapasitesi ve kalan kontenjanı incelenir.

### Rezervasyonlarım

- Mevcut grup dersi rezervasyonlarını görüntüler.
- Uygun seansa rezervasyon yapar veya rezervasyonu iptal eder.
- Paket PT desteği içeriyorsa aktif eğitmene birebir antrenman talebi gönderir.
- Bekleyen PT talebini iptal eder.

Ders rezervasyonunda seans durumu, kontenjan, mükerrer rezervasyon ve paketin haftalık ders limiti kontrol edilir.

### Eğitmenler

Aktif eğitmenleri ve uzmanlıklarını gösterir. Üye, PT talebi için uygun eğitmeni seçmek amacıyla kullanır.

### Kitchen Planı

Üyenin beslenme aboneliği ve kişiselleştirilmiş öğün planı alanıdır.

- Kalori hesaplayıcısıyla hedefe uygun enerji ihtiyacı hesaplanır.
- Uygun Kitchen paketi seçilir ve abonelik/ödeme akışına geçilir.
- Oluşturulan öğün planında gün ve öğünler görüntülenir.
- Tek öğün veya tüm gün atlanabilir; daha sonra geri alınabilir.
- Gün bazında salondan teslim veya eve teslim seçilebilir.

### Menü

Aktif Kitchen ürünlerini listeler. Ürün sepete eklenerek mağaza sepetiyle birlikte ödeme akışında satın alınabilir.

### İlerleme

Üyenin hedef değerlerini ve challenge kapsamında kaydettiği kalori/aktivite ilerlemesini takip eder.

### Kalori Takibi ve Ölçümler

Tarih bazında kilo ve vücut ölçümleri kaydedilir. Kayıtlar kişisel gelişimin zaman içinde izlenmesi için kullanılır.

### NO23 Shop

Aktif mağaza ve Kitchen ürünleri sepete eklenir. Sepette adet değişikliği/kaldırma yapılır; teslimat bilgileri girilerek ödeme tamamlanır. Stok, ödeme ve sipariş kaydı bu akışta birlikte yönetilir.

### Siparişlerim

Üyenin geçmiş ve güncel siparişlerini; sipariş ve ödeme durumlarıyla birlikte listeler.

- Sipariş: Beklemede, Onaylandı, Hazırlanıyor, Teslimata çıktı, Teslim edildi veya İptal edildi.
- Ödeme: Bekleniyor, Ödendi, Başarısız, İade edildi veya Süresi doldu.

### Profil

Ad, soyad ve üye profil bilgilerini görüntüleme/güncelleme alanıdır. Mevcut üyelik paketi de burada gösterilir.

### Hedefler

Beslenme/performans hedeflerini ve üyelik paketine dahil hakları gösterir; kişisel hedeflerin güncellenmesini sağlar.

### Hesap Ayarları

Hesap güvenliği alanıdır. Mevcut parola doğrulanarak yeni parola belirlenir.

### Community Etkinlikleri

Etkinlik ve challenge içeriklerini üye bağlamında gösterir. Üyelik paketi topluluk erişimi içeriyorsa aktif challenge'a katılım sağlanır ve ilerleme kaydedilir.

### Mesajlar

Üyenin eğitmeniyle gerçek zamanlı görüşmesini sağlar. Okunmamış mesaj sayısı menü rozetinde görünür.

### Bildirimler ve sepet

- Üst çandaki bildirim menüsü yeni mesaj, PT, ders, sipariş ve diğer operasyon değişikliklerini gösterir; tek tek veya topluca okundu yapılabilir.
- Sepet çekmecesi eklenen ürünleri ve sipariş toplamını gösterir.

## 5. Eğitmen paneli

### Genel Bakış

Eğitmenin günlük operasyon özetidir. Yaklaşan dersleri, PT taleplerini ve ilgili sayıları gösterir.

### Birebir Talepler

Eğitmene atanmış PT taleplerini durumlarına göre listeler.

- Bekleyen talep uygun tarih ve saat verilerek planlanır.
- Gerekli durumda talep reddedilir.
- Planlanmış talep yeniden düzenlenebilir ve süreç tamamlandı durumuna taşınabilir.
- Durum değişiklikleri üyeye bildirim üretir.

### Derslerim

Eğitmenin sorumlu olduğu aktif grup derslerini ve planlanan seansları gösterir. Program, kapasite ve katılımcı bilgilerini operasyonel takip için sunar.

### Mesajlar

Eğitmen ve üyeler arasındaki gerçek zamanlı mesajlaşma ekranıdır. Konuşmalar ve okunmamış mesajlar takip edilir.

### Bildirimler

Yeni PT talebi, mesaj ve ders değişiklikleri gibi olayları gösterir. Bildirimler okundu yapılabilir.

## 6. Admin paneli

### Genel Bakış

Üye, aktif eğitmen, sipariş ve operasyon sayılarını özetler. Yöneticiye hızlı durum resmi ve ilgili yönetim ekranlarına kısayollar sağlar.

### Siparişler

Shop, tek seferlik Kitchen ve Kitchen abonelik siparişlerini listeler. Yönetici sipariş ve ödeme durumunu ayrı ayrı günceller. Durum geçişleri stok iadesi ve üye bildirimleri gibi yan etkileri tetikleyebilir.

### Shop Ürünleri

Mağaza ürünlerini oluşturur ve düzenler. Ad, açıklama, fiyat, stok, görsel ve aktiflik bilgileri yönetilir. Pasif ürünler satış ekranında gösterilmez.

### Kitchen > Menü Ürünleri

Yemek/içecek ürünlerini, kategori, besin değerleri, fiyat, görsel, reçete bileşenleri ve plan uygunluğuyla yönetir. Kullanılmayan ürünler silinebilir; ilişkili operasyon kaydı olan ürünlerde silme kısıtlanabilir.

### Kitchen > Stok Takibi

- Malzeme kartlarını ve kritik stok seviyelerini yönetir.
- Stok giriş, çıkış ve düzeltme hareketi kaydeder.
- Seçilen tarih için üretim planı oluşturur.
- Üretim planını Taslak, Hazırlanıyor, Tamamlandı veya İptal durumuna taşır.
- Plan kalemlerini Başlanmadı, Hazırlanıyor veya Hazır olarak işaretler.

### Kitchen > Paketler

5, 10, 20 günlük ve aylık Kitchen abonelik paketlerini; fiyat ve aktiflik bilgileriyle yönetir.

### Kitchen > Abonelikler

Üyelerin Kitchen aboneliklerini, ödeme ve abonelik durumlarıyla izler. Aktif, duraklatılmış, iptal, tamamlanmış, ödeme bekleyen ve ödeme başarısız abonelikler görünür.

### Etkinlikler

Topluluk etkinliklerini oluşturur ve düzenler. Tür, tarih, konum, kapasite, açıklama ve durum yönetilir. Gerektiğinde etkinlik iptal edilir.

### Challenge'lar

Challenge oluşturma, düzenleme, ayrıntı/katılımcı ilerlemesi görüntüleme ve iptal işlemlerini sağlar. Yakında, aktif, tamamlandı ve iptal durumları kullanılır.

### Blog

Blog yazılarını oluşturur ve düzenler. Taslak, yayında ve arşiv durumları içerik yayın takvimini kontrol eder.

### Başarı Hikâyeleri

Üye başarı içeriklerini oluşturur ve düzenler; yalnızca yayına uygun kayıtlar halka açık alanda görünür.

### Üyeler & Paketler > Üyeler

Kayıtlı üyeleri, iletişim bilgilerini ve atanmış üyelik paketlerini listeler. Operasyon ekibinin üye tabanını görmesi için kullanılır.

### Üyeler & Paketler > Üyelik Paketleri

Paket adı, fiyat, hedef kitle, açıklama, haftalık ders limiti, PT desteği, topluluk erişimi ve aktiflik bilgilerini yönetir.

### Eğitmenler

Eğitmen profili oluşturur/düzenler, aktifliği yönetir ve eğitmene panel hesabı açar. Panel hesabı oluşturulduktan sonra eğitmen rolüyle giriş yapılabilir.

### Ders Yönetimi > Grup Dersleri

Ders tanımını, eğitmenini, kapasitesini, zorluk seviyesini ve aktifliğini yönetir. Bu kayıt tekrar kullanılabilen ders şablonudur.

### Ders Yönetimi > Ders Programı

Grup dersi için tarih/saat bazlı seans oluşturur ve düzenler. Seans kapasitesi ders kapasitesinden farklı belirlenebilir. Seans iptal edildiğinde rezervasyon sahiplerine bildirim gönderilir.

### Ders Yönetimi > Birebir Talepler

Tüm PT taleplerini merkezi olarak listeler ve durum/eğitmen/tarih bilgilerini yönetir. Eğitmen panelindeki operasyonun yönetici görünümüdür.

## 7. Önerilen günlük operasyon sırası

1. Admin **Genel Bakış** ve bildirimleri kontrol eder.
2. **Siparişler** ve ödeme bekleyen kayıtlar incelenir.
3. **Kitchen > Stok Takibi** üzerinden kritik stok ve günlük üretim planı kontrol edilir.
4. **Ders Programı** ve iptal/değişiklikler kontrol edilir.
5. **Birebir Talepler** bekleyen kayıtlar için takip edilir.
6. İçerik ve topluluk ekranlarında yaklaşan yayın/etkinlikler gözden geçirilir.

## 8. Önemli durum kuralları

- Pasif ürün, paket, eğitmen veya ders yeni işlemlerde seçilemez.
- Dolu veya iptal edilmiş seansa rezervasyon yapılamaz.
- Haftalık ders limiti paket bazında uygulanır.
- PT talebi yalnızca PT desteği içeren paketlerde açılır.
- Challenge katılımı topluluk erişimi içeren paketlere bağlıdır.
- Başarısız, süresi dolmuş veya iade edilmiş ödeme ilgili sipariş/abonelik ve stok süreçlerini etkiler.
- Üretim planı ve kalem durumları gerçek mutfak operasyonuna paralel güncellenmelidir.
