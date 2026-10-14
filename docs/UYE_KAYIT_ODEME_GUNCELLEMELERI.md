# Üye kaydı, ödeme ve ders yönetimi güncellemeleri

Talep tarihi: 8 Ekim 2026

| Madde | Uygulama | Durum |
|---|---|---|
| 1. Hafta sayısı otomatik hesaplansın | Birebir ders planı, üyenin paket başlangıç/bitiş tarihleri ve seçilen ders günlerinden hesaplanır. Paket dışına taşan dersler kaydedilmez. Bitişi bilinmeyen eski üyelerde manuel hafta seçimi kullanılabilir. | Tamamlandı ve doğrulandı |
| 2. Doğum tarihi | Yeni üye kayıtlarında doğum tarihi zorunlu; admin mevcut üyelerde bilgiyi düzenleyebilir. Eski kayıtlar boş bırakılabilir. | Tamamlandı ve doğrulandı |
| 3. Admin manuel kayıtta ödeme | Üye ve paket ödeme kaydı birlikte oluşturulur. Tutar, yöntem, POS/dekont referansı ve tahsilat bilgileri kaydedilir. | Tamamlandı ve doğrulandı |
| 4. Ödeme yöntemleri | Üyelik kategorisinde kredi kartı zorunlu. Diğer paketlerde, Shop ve Kitchen işlemlerinde kart, nakit veya havale seçilebilir. Nakit/havale admin teyidine kadar ödeme bekler. | Tamamlandı ve doğrulandı |
| 5. Taksit ekranı | Taksit sayısı, tutar, vade ve tahsilat tarihi girilir; kalan tahsilatlar siparişin ödeme ekranından izlenir. | Tamamlandı ve doğrulandı |
| 6. Ders gün / saatlerini toplu güncelleme | Admin ve antrenör takviminde seçilen dersler, bu ve sonraki tekrarlar veya tüm gelecek tekrarlar için gün / saat değiştirilir. Üye filtresiyle bir tarih aralığındaki tüm planlanmış birebir dersler de düzenlenebilir. | Tamamlandı ve doğrulandı |
| 7. Toplu ders silme | Seçilen planlanmış birebir ve grup seansları, bu ve sonraki tekrarlar veya tüm gelecek tekrarlar birlikte silinebilir. Ayrılmış grup ders hakları iade edilir. | Tamamlandı ve doğrulandı |
| 8. Takvimde üye filtresi | Admin ve antrenör takviminde yalnızca seçili üyenin birebir dersleri ve katıldığı grup seansları gösterilir. Hafta geçişleri ve takvim işlemleri filtreyi korur. | Tamamlandı ve doğrulandı |
| 9. Grup silme / pasife alma | Sil, grubu seans ve katılım kayıtlarıyla kaldırır. Pasife al, geçmişi koruyup gelecek seansları iptal eder. Pasif gruplar takvimde ve varsayılan ders programında görünmez. | Tamamlandı ve doğrulandı |
| 10. Büyük harfli parola kontrolü | Tek büyük harf yeterlidir; Ö, Ü, İ, Ç, Ş ve diğer Türkçe harfler de büyük/küçük harf olarak tanınır. | Tamamlandı ve doğrulandı |
| 11. Geçici parola yenileme | Admin, Üyeler listesinden veya üye düzenleme ekranından tekrar tekrar yeni geçici parola belirleyebilir. | Tamamlandı ve doğrulandı |
| 12. Kalça ölçüsü | Admin ve antrenör ölçüm formu, karşılaştırma ve geçmişinde Kalça, Basen'den önce ayrı alan olarak yer alır. | Tamamlandı ve doğrulandı |
| 13. İptalde ders hakkı | Birebir iptal hak tüketmez ve paket ders kotasından sayılmaz. Grup iptalinde rezervasyonda ayrılan hak iade edilir. | Tamamlandı ve doğrulandı |

## İş kuralları

- Üyelik paketleri HYBRID, PRO ve BLACK'tir; PT, grup ve Kids ayrı paket kategorileridir.
- Sabit fiyatlı paketlerde ödeme tutarı dönem toplamından hesaplanır. Kids kardeş indirimi sunucuda uygulanır. Fiyatı teklif ile belirlenen paketlerde tutarı admin girer.
- Taksit tutarlarının toplamı ödeme tutarıyla eşleşir. Yuvarlama farkı son taksite eklenir. Tahsil edilmiş taksit yeniden tahsil edilmiş sayılmaz.
- Admin, kayıt sırasında 1–36 taksit oluşturabilir ve tutar/vadeleri düzenleyebilir. Sonraki tahsilatlar **Siparişler → Ödeme / taksitler** ekranından kaydedilir.
- PostgreSQL'de aynı siparişin eşzamanlı tahsilatları sipariş satırı kilitlenerek sıralanır. Dekont ve tahsilatı giren admin bilgisi korunur.
- Manuel Kitchen aboneliği tüm taksitler tahsil edildiğinde aktifleştirilir. Plan hazırlanamazsa ödeme kaydı korunur; admin aynı ekrandan aktifleştirmeyi yeniden deneyebilir.
- Bu ekran POS veya banka tahsilatının kaydını tutar; kart numarası veya CVV saklamaz. Online kart tahsilatı mevcut iyzico ekranından yapılır.
- Toplu işlemler yalnızca planlanmış derslerde uygulanır. Bir işlem en fazla 500 ders içerir; üye tarih aralığı en fazla iki yıldır. Antrenörün yetkisi kendi dersleriyle sınırlıdır.
- Yalnızca saat girilirse ders tarihi korunur. Gün girilirse her ders aynı Pazartesi–Pazar haftasındaki yeni güne taşınır; saat boşsa mevcut saat korunur. Eğitmen/katılımcı çakışması, paket tarih sınırları ve birebir derslerin aylık paket limitleri kontrol edilir. Bir hata varsa toplu işlemin tamamı reddedilir. PostgreSQL'de işlem tek transaction içinde yapılır.
- Grup seansına yapılan toplu değişiklik tüm katılımcıları etkiler; ekranda bu kapsam açıkça belirtilir. Toplu seans silme, grup dersinin katalog kaydını silmez.
- Grup dersindeki **Sil**, tüm seans ve katılım geçmişini kalıcı kaldırır; ekranda açık silme onayı vardır. **Pasife al** geçmişi korur, gelecek seansları iptal eder ve ayrılmış hakları iade eder. Pasif geçmiş, ders programındaki isteğe bağlı geçmiş görünümünden incelenebilir.
- Parola politikası en az 6 karakter, bir büyük harf, bir küçük harf, rakam ve özel karakterdir. Yenilemede eski parola geçersiz olur; parola açık metin saklanmaz.
- Kalça yeni nullable alandır; mevcut Basen kayıtları aynı alan ve değerlerle korunur. Eski iptaller için otomatik toplu hak düzeltmesi yapılmaz.
- Doğum tarihi/ödeme ve Kalça alanı için iki migration hazırdır. Canlı yayın henüz yapılmamıştır.

## Doğrulama

- Çözüm derlemesi başarılı; 469/469 test geçti, atlanan test yok.
- Masaüstü ve 390 piksel mobil ekranda kayıt, paket seçimi ve taksit düzeni kontrol edildi; yatay taşma yok.
- Paket bitişinden hafta/ders hesabı, üyelikte kart zorunluluğu, paket tutarı, kuruş yuvarlaması ve ay sonu vadeleri tarayıcıda doğrulandı.
- Deneme taksiti tahsilatı ve ödenmiş Kitchen paketinin tekrar aktifleştirilmesi yerel test verisiyle doğrulandı.
- Toplu saat değişikliği, çakışmada tüm işlemin reddi, antrenör yetki sınırı, üye ve tarih aralığı, toplu silme ve tek seferlik hak iadesi test edildi.
- Ö/Ü/İ/Ç dahil tek büyük harfli parola kabulü, diğer parola kuralları ve art arda iki yenilemede eski parolanın geçersizleşmesi test edildi.
- Tarayıcıda iki birebir ders birlikte güncellendi; tarih ve üye filtresi korundu. Kalça ayrı kaydedildi, Basen değeri korundu. Admin ve antrenör takvimi 390 piksel mobil görünümde kontrol edildi.
- `20261008125009_AddMemberBirthDatesAndPaymentPlans` ve `20261008134703_AddGluteMeasurements` migration'ları ile PostgreSQL SQL çıktısı hazır. EF model ile migration arasında bekleyen değişiklik bulunmuyor. Migration'lar canlı veritabanına uygulanmadı.

## 14 Ekim 2026 — Takvimde ders tekrarları

1. Takvimde değiştirmek veya silmek istediğiniz ders kartını seçin.
2. **İşlem kapsamı** alanında **Yalnızca seçilen dersler**, **Bu ve sonraki tekrarlar** veya **Tüm gelecek tekrarlar** seçin.
3. Taşımak için yeni gün ve / veya saat girip **Gün / saati güncelle** düğmesine basın. Kaldırmak için **Dersleri sil** düğmesini kullanın.

- Tekrar kapsamları yalnızca gelecekteki planlanmış derslere uygulanır; geçmiş ve sonuçlanmış derslerin programı korunur. Grup seansındaki işlem tüm katılımcıları etkiler; üye filtresi diğer haftaların katılımcılarını kapsam dışında bırakmaz.
- Yeni haftalık programlarda her seçili gün ayrı bir tekrar kimliği alır. Ayrı oluşturulan programlar ve tekil dersler birbirine karışmaz. Bir ders tek başına ertelense de program bağlantısı korunur.
- Eski kayıtlarda tekrar kimliği olmadığı için birebir dersler üye, antrenör, gün, saat, süre ve nota; grup seansları grup dersi, gün, saat ve kontenjana göre eşleştirilir. Başarılı güncellemede eşleşen eski derslere kalıcı tekrar kimliği atanır. Daha önce farklı güne / saate taşınmış eski kayıtlar bu eşleşmeye dahil olmayabilir.
- Üyenin tarih aralığındaki tüm birebir dersleri seçeneği ile tekrar kapsamı aynı anda kullanılamaz. 500 ders sınırı, tekrarlar dahil toplam ders sayısına uygulanır.
- `20261014080736_AddCalendarRecurrenceIds` migration'ı derslere nullable tekrar kimliği ve indeks ekler. Yayında bu migration uygulanmalıdır; canlı veritabanına bu çalışma kapsamında işlem yapılmadı.
- Çözüm derlemesi başarılı; 499/499 test geçti. Tekrar kapsamı, gün / saat taşıma, eski derslerin bağlantısı, yetki sınırı, hak iadesi, çakışma / paket / aylık limitte tüm işlemin reddi ve tekrarlar dahil 500 ders sınırı doğrulandı. JavaScript sözdizimi ve form etkileşimleri de kontrol edildi.
- Ayrı ve geçici PostgreSQL 18 veritabanında 55 migration'ın tamamı uygulandı. Gerçek Npgsql / Serializable işlem yolunda tekrarları taşıma, eski kayıtları bağlama, tüm grup katılımcılarını kapsama, tek seferlik hak iadesi ve çakışma / paket / aylık limitte hiçbir değişiklik yapılmaması doğrulandı. Test konteyneri kaldırıldı.
