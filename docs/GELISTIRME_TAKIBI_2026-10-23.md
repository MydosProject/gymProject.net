# 23 Ekim geliştirme takibi

Maddeler ayrı kabul kriterleriyle ilerletilir. Bir maddenin burada sıraya alınması, geliştirmesinin tamamlandığı anlamına gelmez.

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

## Sonraki sıra: Kitchen kaydı ve üye erişimi

| Madde | Durum | Kabul kriteri |
|---|---|---|
| Yalnızca Kitchen için kayıt | Sırada | Paket seçiminde ayrı Kitchen seçeneği; Kitchen'den gelince ön seçim ve Kitchen akışına dönüş. Spor paketi zorunlu tutulmaz. Hesap oluşturmak spor salonu veya sınırsız ders hakkı kazandırmaz. Kitchen satın alma ve ödeme akışı korunur. |
| Ölçüm ve gelişimde üye arama | Sırada | Yazdıkça sınırlı sonuç listesi; ad/soyad yanında telefon ve üye numarası gibi ayırt edici bilgiler. Seçim isimle değil üye ID'siyle yapılır. Antrenör yalnızca atanmış üyelerini arayabilir. |
| Antrenörün üye listesi | Sırada | Ayrı “Üyelerim” ekranı; paket, kalan seans, iletişim, takvim ve ölçüm bağlantıları. Başka antrenörün üyesine doğrudan URL ile de erişilemez. |

## Sonraki sıra: Talepler ve takvim

| Madde | Durum | Kabul kriteri |
|---|---|---|
| Genel bakışta yeni talepler | Sırada | Şu an dashboard esas olarak üye PT taleplerini sorguluyor. Misafir randevu ve paket başvuruları da üstte bekleyen işler olarak görünmeli; toplam sayılar ve tüm taleplere ulaşım olmalı. |
| Üye olmayan kişinin ön görüşmesi | Sırada | Talep yönetici tarafından tarih/saat/antrenör/süre ile planlanır. Üyelik gerekmez. Aynı takvimde ayrı renk ve “Ön görüşme” etiketiyle görünür. Ders hakkı tüketmez. Eğitmen çakışması, yeniden planlama ve iptal kontrol edilir. Tercih edilen saat otomatik kesin randevu kabul edilmez. |
| Google Takvim benzeri esneklik | Sırada | Gün/hafta gezinmesi, geçmiş tarihe gitme, okunaklı detaylar ve işlem kapsamları. Geçmiş dersler görüntülenebilir; geçmiş haklar tarih gezinmesiyle değişmez. |

## Sonraki sıra: Kalan seans ve haftalık Kitchen planı

| Madde | Durum | Kabul kriteri |
|---|---|---|
| Ders düşümü ve kalan seans kontrolü | Kural yanıtı bekleniyor | Paket bitişinden sonra da kullanılmamış hakların kullanılıp kullanılmayacağı soruldu. Mevcut kural bu yanıt gelmeden değiştirilmedi. Satın alınan, tamamlanan, önceden planlanan ve yeni planlanabilir haklar ayrılmalı; mükerrer düşüm ve iptalde hak kaybı olmamalı. |
| Üye ekranında kalan seans | Aynı kural ile ele alınacak | Gün sayısı yerine hak sayısı gösterilir. Üye, yönetici ve eğitmen ekranları aynı bakiyeyi kullanır. PT/grup haklarının farklı zamanlarda işlenmesi ve eski kayıtlar ayrıca denetlenir. |
| Haftalık Kitchen tablosu | Sırada | Üye panelinde seçilen haftanın günleri, ana/ara öğün ürünleri, teslim/alım bilgisi ve atlanan günler görünür. Önceki/sonraki hafta gezilir. Ödeme/plan hazırlanması bekleniyorsa açık durum gösterilir. |

## Doğrulama

- 561 test başarılı. Kitchen doğrulama adaptörü Türkçe, İngilizce ve Almanca kültürlerde fiyat sınırlarıyla test edilir.
- Grubu pasife alıp tekrar aktifleştirme; iptal seanslarının iki takvim görünümünde de gizli kalması ve hak iadesinin korunması test edildi.
- Tüm paketlerde yönetici nakit/havale, özel fiyat ve indirim, taksit tutarı eşitliği test edildi. Önceki “üyelikte nakit reddedilir” testleri yeni onaylanmış yönetici kuralına göre güncellendi.
- Ayrı PostgreSQL 18 veritabanında gerçek form: 37.500 ₺ katalog bedelli üyelik, nakit 25.000 ₺ özel satış, iki taksit; ilk tahsilat 12.500 ₺ ve kalan 12.500 ₺. Üye listesi ve ödeme geçmişi doğrulandı.
- Gerçek Kitchen formunda 3.000,25 ₺ fiyat kuruşuyla kaydedildi.
- Masaüstü PRO görünümü ve mobil iki günlük takvim tarayıcıda kontrol edildi.

Bu teslimatta şema değişikliği yoktur. Yalnızca sahte üye ve ayrı test veritabanı kullanılmıştır. Git'e gönderim, canlı yayınlama doğrulaması değildir.
