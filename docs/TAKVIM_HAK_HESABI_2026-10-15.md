# Takvim programı ve tamamlanan ders hakları

## Düzeltilen davranış

- Haftalık birebir program önizlemesi ve kayıt aynı sunucu hesabını kullanır. Paket bitişine kadar bulunan tüm tarihler artık satın alınan hak gibi gösterilmez.
- Paketinin birebir hakkı 70 olan üyeye, 75 uygun tarih bulunsa da en fazla 70 ders eklenir. Tamamlanan ve önceden planlanan dersler bu sayıdan ayrılır. Aynı üye/antrenör/saat/süredeki mevcut kayıtlar yeniden eklenmez.
- Ekranda paketin birebir hakkı, tamamlanan/kullanılan dersler, önceden planlanan dersler, kalan toplam hak ve yeni planlanabilir hak ayrı gösterilir. Paket toplamı seçilen haftayla değişmez. Geç başlayan program tüm haklara yer bulamazsa eksik kalan sayı açıklanır.
- Kalan toplam hak, mevcut veri modelindeki ortak PT/grup bakiyesidir. Paket toplamı paket varyantından gelir; eski varyantsız üyelerde toplam satın alınan birebir hak bilinmediği için “Belirtilmemiş” gösterilir.
- Aylık yenilenen paketlerin aylık sınırı korunur. Bitişi bilinmeyen eski üyelerin açıkça girdikleri hafta sayısı hakları aşıyorsa işlem reddedilir.
- Önizlemeden sonra haklar değişirse kayıt sırasında yeniden hesaplanır. PostgreSQL üzerinde haftalık kayıt seri hale getirilebilir işlem içinde yapılır; eşzamanlı iki kayıt hakları aşamaz.
- Tamamlanan birebir ve grup dersleri yönetici/eğitmen takviminde varsayılan olarak gizlidir. “Tamamlanan dersleri göster” geçmiş kayıtları görüntüler; kayıtlar silinmez. Görünüm filtresi hak hesabını değiştirmez.

## Önceki sürümün kapsamı ve eski kayıtlar

14 Ekim düzeltmesi, planlanmış bir ders yeni tamamlandığında ortak tamamlama işlemiyle hakkını düşürür. Önceden tamamlanmış ve `CreditConsumed=false` kalmış dersleri otomatik değiştirmez. Bu kayıtlar, yeni sürüm yayınlandıktan sonra da kontrol bekler. Yeni kod bunları planlama hesabında ayrıca ayırır.

Yönetici takviminde “Haftalık sabit birebir program ekle” bölümünde antrenör ve üye seçilince, mevcut paket döneminde işlenmemiş tamamlanan ders varsa hak kontrol formu açılır. Sabit gün seçmek gerekmez.

1. Ders geçmişini ve üyenin kalan bakiyesini kontrol edin.
2. Haklar hiç düşülmemişse “Bu dersler düşülmemiş; bakiyeden düş” seçin.
3. Daha önce manuel düşüm yapıldıysa “Önceden manuel düşülmüş; bakiyeyi koru ve işlenmiş say” seçin. Bu yöntem bakiyeden tekrar düşmez.
4. Kontrol kutusunu işaretleyip işlemi uygulayın.

Her yöntem, dersleri işlenmiş olarak işaretler ve yönetici kimliğiyle işlem geçmişi oluşturur. Aynı kayıt ikinci kez işlenemez. Görüntülenen sayı veya bakiye değişmişse eski form reddedilir. Paket başlangıcı bilinmeyen üyelerin geçmişi otomatik tahmin edilmez; önce üye kaydındaki paket dönemi kontrol edilmelidir. Yeni sürüm yayınlanırken toplu ve koşulsuz bakiye düşümü yapılmaz.

## Doğrulama

- Tam test paketi: 541/541 başarılı. 70 ders önizleme/kayıt eşitliği, hafta değişince paket toplamının sabitliği, kısa dönem uyarısı, aylık kota, önceki kayıtların ayrılması, iptal edilen tarihin yeniden kullanımı, yetki, eski hak düzeltmesi ve tekrar işlem koruması, yönetici/eğitmen tamamlanmış ders filtresi kapsanır.
- Ayrı PostgreSQL 18 test veritabanında gerçek yönetici formu: 70 kayıt; otomatik tamamlanan 2 ders; kalan 68 hak ve 68 planlanmış ders. Varsayılan takvimde bu haftanın yalnızca kalan 1 dersi görünür.
- Eski 3 tamamlanmış dersin yönetici kontrolü: 70 → 67; 3 hak işareti ve 3 işlem geçmişi kaydı.
- Eşzamanlı iki haftalık kayıt: tek başarılı işlem ve toplam 70 ders.
- Eşzamanlı iki eski hak düzeltmesi: tek başarılı işlem, yalnızca 3 düşüm ve 3 işlem geçmişi kaydı.
- Daha önce elle düşülmüş 3 dersin gerçek formdan işaretlenmesi: bakiye 67 olarak korunur.
- Masaüstü ve mobil görünüm kontrolü; gerçek sunucu veya üye verisi değiştirilmeden sahte test üyeleri kullanılmıştır.

Veritabanı şema değişikliği yoktur. Kodun Git'e gönderilmesi canlı yayınlama işlemi değildir.
