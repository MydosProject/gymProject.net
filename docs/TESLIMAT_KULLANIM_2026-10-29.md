# Ödeme dışı kullanım düzeltmeleri — 29 Ekim 2026

Başlangıç: `26d4396`. Dal: `codex/qa-kullanim`. Çalışma küçük commitlere ayrıldı.
Bu paket için veritabanı migration'ı eklenmedi. Canlı sunucuya yayın yapılmadı.

## Yapılanlar ve kontrol ekranları

| Paket | Ekran / yöntem | Kontrol |
|---|---|---|
| Onay | Admin takvim katılımcısı, alerjen, paket seçeneği, varyant, gelişim fotoğrafı; üye ana sayfa/grup rezervasyonu ve PT iptali; ön görüşme iptali | İşlemi başlatınca ilgili kişi/kayıt bilgisiyle onay açılır. Vazgeçince POST gönderilmez. Mevcut iptal süresi, yetki ve hak kuralları korunur. |
| Mobil tablo | Ders hakları, eğitmenin üyeleri, seanslar, Kitchen stok, alerjenler, hizmet paketleri/varyantları ve üyelik seçenekleri | Alan adları mobilde görünür. `mobile-labels` ile seçilen tablolarda ortak TagHelper kullanılır; colspan satırları ve iç tablolar kendi kapsamındadır. Eğitmen kartlarının koyu zemin/açık metin uyumu düzeltildi. |
| Türkiye günü | Kitchen başlangıcı/öğün değişikliği, ölçüm/kalori, PT talebi, topluluk günü, takvim yönlendirmeleri ve tarih varsayılanları | Ödeme dışındaki `DateTime.Today/Now` kullanımları kulüp gününe bağlandı. Ana iş servislerinde `IClubClock` / `TimeProvider`; dönüşümlerde `Europe/Istanbul` kullanılır. UTC saklama sürer. |
| Listeler | Admin Üyeler, Randevu Talepleri, Birebir Talepler, Paket Başvuruları | Veritabanında arama/durum filtresi ve 25 kayıtlık sayfa. Sıralama ID ile kararlı. Genel istatistikler tüm kayıtlardan hesaplanır. Antrenör atama ve liste içi talep durumu değişikliğinde filtre/sayfa korunur. Üyeler #numara ve iletişim bilgileriyle ayırt edilir. |
| Tekrar gönderim | Seçili admin/trainer kayıt/takvim formları ve onaylı silme/iptal formları | `data-submit-once` ile native POST bir kez gönderilir. Tıklanan düğmenin name/value değeri korunur. Geçersiz veya JS tarafından iptal edilen gönderim formu kilitlemez; geri dönüşte form açılır. AJAX akışlarının kendi davranışına müdahale edilmez. |
| Modüler başlangıç | `Features/Membership/MemberDirectoryService` ve `MembershipModule` | Üye listeleme sorgusu controller'dan çıkarıldı. Bağımlılıklar scoped olarak enjekte edilir. URL, Area, Razor ekranı ve yetki kuralları korunur. Diğer modüllerin toplu taşınması bu teslimata dahil değildir. |

## Doğrulama

- Başlangıç otomatik testleri: **673 geçti**. Son tur: **695 geçti, 0 başarısız, 0 atlanan**.
- Ek 22 vaka: İstanbul gün/hafta/yıl sınırları, PT tarih doğrulaması, ölçüm ve Kitchen gün sınırı, liste filtreleri/sayfa sınırları/genel toplamlar/aylık istatistik ve büyük harfli e-posta araması, tekrar iptalde tek hak iadesi.
- Edge/Playwright ve ayrı PostgreSQL test veritabanıyla: admin onayını reddetmede sıfır POST, onayda tek POST; üye rezervasyon iptalinde 69 → 70 hak ve diğer üyede değişmeyen bakiye.
- Mobil smoke: altı admin ekranında 390/768/1366 px etiketler ve sayfa taşması; eğitmenin üye kartında mobil etiket ve renkler. Kitchen üretim planının bütün dolu/reçeteli varyantları ayrıca kapsamlı manuel kabul testi bekler.
- Gerçek PostgreSQL üzerinde 37 üyelik liste: 25/12 sayfalama, genel toplam 37, aynı isimli iki kişi, Türkçe arama, filtreli sayfaya antrenör kaydı sonrası dönüş. Randevu arama/durum kaydı sonrası bağlam korunması doğrulandı.
- Üye ana sayfa, rezervasyon, ölçüm ve Kitchen HTTP ekranları yeni DI kayıtlarıyla açıldı. Ön görüşme iptali onayından vazgeçmede sıfır POST doğrulandı.
- Tarayıcı tekrar gönderim testi: iki gönderim → tek POST/tek onay; düğmenin işlem değeri ve diğer alanlar korunur. Geçersiz form ve sayfaya özel iptal kilitlenmez; geri dönüş kontrol edildi.
- Son kaynak için Razor derlemesi başarılı; `git diff --check` temiz. Mevcut `System.Security.Cryptography.Pkcs` NU1902/NU1903 uyarıları sürüyor.

Manuel PostgreSQL/tarayıcı kontrolleri geçici, izole test ortamında yapıldı; bunlar henüz kalıcı PostgreSQL/Playwright CI paketi değildir. Otomatik regresyon vakaları `tests/NO23.Tests` altında saklandı. Rapordaki bütün kabul senaryolarının tamamlandığı anlamına gelmez.

## Ayrı devam edecekler

- Ödeme/sipariş/stok tutarlılığı: BE-01…BE-06, BE-08; ilgili ödeme ekranları ve kart tekrar deneme akışı. Ödeme servislerindeki tarih kullanımları da bu pakette değiştirilmedi.
- Ödeme geçmişinin mobil tablosu bu turda değiştirilmedi. Siparişler ve Kitchen abonelikleri dahil diğer listelere ortak sayfalama/filtrenin yayılması sürmeli.
- Seçilmemiş formlarda genel tekrar gönderim garantisi verilmez. Sunucuda idempotency, işlem türü bazında ayrıca değerlendirilir; mevcut rezervasyon iptalinin tek hak iadesi testle korundu.
- SEC-01…SEC-04: gerçek canlı ortam, kalıcı disk/anahtar, yedek geri yükleme ve çoklu kopya doğrulaması yapılmadı. Bu konuların kapandığı söylenemez.
- Kalıcı PostgreSQL entegrasyon CI'ı, mimari sınır testleri, diğer modüllerin çıkarılması ve CSS'nin geniş kapsamlı düzenlenmesi sonraki işlerdir.

## Test ortamı notu

Test başlangıcında PowerShell bağlantı oluşturucusu nedeniyle uygulama kısa süre yerel varsayılan veritabanına bağlandı. İşlem durduruldu, oluşturulan tek QA yönetici hesabı temizlendi ve bağlantıya test veritabanı adı kontrolü eklendi. Sonraki fonksiyonel kontroller ayrı `no23_qa_usability_20261029` veritabanında yapıldı. Canlı sunucuya bağlanılmadı.

Testler bittikten sonra QA uygulaması durduruldu ve yalnız bu iş için oluşturulan test veritabanı kaldırıldı. Geçici TRX ve ekran görüntüleri yerelde tutuldu.
