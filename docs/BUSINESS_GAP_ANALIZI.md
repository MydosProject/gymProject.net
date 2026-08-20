# NO23 Sports Club Business Gap Analizi

## 1. Yönetici özeti

Uygulama geniş bir MVP kapsamına sahip: rol bazlı paneller, üyelik paketleri, ders rezervasyonu, PT talepleri, mesajlaşma, Kitchen planlama ve stok, Shop, sipariş/ödeme, challenge ve içerik yönetimi birlikte çalışıyor. En büyük eksik, yeni özellik sayısından çok bazı süreçlerin ticari yaşam döngüsünün yarıda kalmasıdır.

Öncelikli üç alan:

1. Üyeliğin satın alma–yenileme–dondurma–iptal–sona erme yaşam döngüsü.
2. Finansal doğruluk: iade, fatura, mutabakat ve manuel ödeme değişikliklerinin kontrolü.
3. Operasyonel izlenebilirlik: audit log, yetki ayrımı, raporlama ve veri dışa aktarma.

## 2. Önceliklendirilmiş eksikler

| Öncelik | Eksik business yeteneği | Mevcut durum | İş etkisi | Öneri |
|---|---|---|---|---|
| P0 | Üyelik yaşam döngüsü | Üye kayıt olurken paket seçiliyor; başlangıç/bitiş, satın alma, yenileme, dondurma ve iptal akışı görünmüyor | Üyelik hakkı süresiz kalabilir; gelir ve hizmet hakkı eşleşmez | `MembershipSubscription` ve ödeme kaydı ekleyin; başlangıç/bitiş, yenileme, dondurma, iptal ve hak kazanımı kuralları tanımlayın |
| P0 | Gerçek ödeme yapılandırması | Iyzico varsayılan olarak kapalı; yerel geliştirme ödeme almadan ilerleyebilir | Canlıya geçişte gelir akışı çalışmaz veya yanlış beklenti oluşur | Sandbox–prod ayrımı, secret yönetimi, webhook/callback doğrulama, canlıya geçiş kontrol listesi ve smoke test ekleyin |
| P0 | İade operasyonu | `Refunded` durumu var ancak admin ödeme durumunu değiştirebiliyor; gerçek sağlayıcı iade çağrısı ve finansal kanıt ekranı net değil | Sistemde iade edilmiş görünen ama bankada iade edilmemiş işlem oluşabilir | İadeyi yalnızca sağlayıcı API sonucu ile tamamlayın; kısmi/tam iade, neden, yapan kişi, sağlayıcı referansı ve dekont kaydedin |
| P0 | Yetki ayrımı ve audit log | Tek `Admin` rolü geniş operasyonların tamamını yapıyor; değişiklik geçmişi görünmüyor | Hatalı/kötü niyetli değişikliklerin sahibi bulunamaz | SuperAdmin, Operasyon, Finans, Kitchen, İçerik rollerini ayırın; eski/yeni değer, kullanıcı, zaman ve IP içeren audit kayıtları ekleyin |
| P1 | Üye yönetim aksiyonları | Admin üye listesi var; paket değiştirme, üyeyi askıya alma, şifre sıfırlama daveti ve üyelik geçmişi ekranı görünmüyor | Destek ekibi temel talepleri sistemden çözemeyebilir | Üye detay sayfası, durum, paket geçişi, süre/hak geçmişi ve güvenli sıfırlama daveti ekleyin |
| P1 | Ders katılım operasyonu | `Attended` ve `NoShow` durumları modelde var; belirgin yoklama/katılım işaretleme ekranı yok | No-show politikası ve gerçek katılım raporu uygulanamaz | Seans katılımcı listesi, toplu yoklama, geç giriş ve eğitmen notu ekleyin |
| P1 | Bekleme listesi | Kontenjan dolunca rezervasyon reddediliyor | İptal edilen yerler gelir/katılım kaybına dönüşür | Sıralı bekleme listesi, otomatik terfi, süreli onay ve bildirim ekleyin |
| P1 | İptal ve no-show politikası | İptal işlemi var; geç iptal penceresi, ceza veya hak iadesi politikası görünmüyor | Üyeler kapasiteyi bloke edebilir; adalet ve gelir sorunu oluşur | Ders/PT için ayrı iptal süresi, geç iptal, no-show sayacı ve paket hakkı etkisi tanımlayın |
| P1 | PT kapasite ve takvim çakışması | PT planlama var; eğitmen çalışma saatleri/izinleri ve çakışma yönetimi görünmüyor | Aynı eğitmene çakışan seans atanabilir | Eğitmen uygunluk takvimi, izin, tampon süre, lokasyon ve çakışma kontrolü ekleyin |
| P1 | Sipariş teslimat operasyonu | Sipariş durumları var; kargo/kurye entegrasyonu, takip kodu ve teslimat kanıtı yok | Müşteri hizmetleri siparişin fiziksel hareketini doğrulayamaz | Teslimat sağlayıcısı, takip kodu, SLA, teslim alan ve teslimat kanıtı alanları ekleyin |
| P1 | Fatura/e-belge | Sipariş ve ödeme var; fatura, vergi, şirket bilgisi ve e-arşiv entegrasyonu görünmüyor | Muhasebe ve mevzuat süreçleri manuel kalır | Fatura adresi/tipi, vergi alanları, belge numarası ve e-belge entegrasyonu ekleyin |
| P1 | Kitchen teslimat kapasitesi | Üye eve teslim/salondan teslim seçiyor; bölge, saat aralığı, günlük rota kapasitesi ve teslimat ücreti yok | Operasyon karşılanamayacak teslimat sözü verebilir | Posta kodu/bölge, zaman dilimi, kapasite, minimum tutar ve ücret kuralları ekleyin |
| P1 | Kitchen abonelik yönetimi | Duraklatılmış/iptal durumları var; admin ve üye aksiyonları, kalan gün devri ve yeniden başlatma kuralı görünmüyor | Abonelik anlaşmazlığı ve mali kayıp doğar | Duraklatma aralığı, maksimum süre, kalan hak, telafi ve iptal/iade politikası ekleyin |
| P1 | Stok tedarik süreci | Malzeme ve hareket takibi var; tedarikçi, satın alma siparişi, mal kabul ve maliyet yok | Stok tükenmesi ve gıda maliyeti yönetilemez | Supplier, purchase order, goods receipt, lot/son kullanma tarihi ve ortalama maliyet modülleri ekleyin |
| P1 | Raporlama ve dışa aktarma | Dashboard özetleri var; dönemsel gelir, satış, stok, katılım ve retention raporları görünmüyor | Yönetim kararları veriye dayalı alınamaz | Tarih filtreli KPI ekranları ve CSV/Excel dışa aktarma ekleyin |
| P2 | Kampanya/kupon/fiyat kuralı | Sabit ürün ve paket fiyatları var | Pazarlama kampanyaları teknik müdahale ister | Kupon, kampanya tarihi, kullanım limiti, üye segmenti ve indirim dağılımı ekleyin |
| P2 | Etkinlik katılım/rezervasyon | Etkinlik kapasitesi var; challenge katılımı mevcut, normal etkinlik kayıt/check-in akışı belirgin değil | Kapasite gerçek katılımla yönetilemez | Etkinlik kayıt, bekleme listesi, QR/check-in ve iptal akışı ekleyin |
| P2 | Challenge ödülü | Katılım ve ilerleme var; ödül, doğrulama ve kötüye kullanım kontrolü yok | Motivasyon ve kampanya ölçümü zayıf kalır | Ödül kuralları, kanıt/onay, leaderboard ve kazanım kaydı ekleyin |
| P2 | İçerik yayın planlama | Taslak/yayın/arşiv var; ileri tarihli yayın ve onay akışı yok | İçerik operasyonu manuel kalır | `PublishAt`, önizleme ve editör–onaylayan akışı ekleyin |
| P2 | Müşteri destek süreci | Mesajlaşma yalnız üye–eğitmen odağında | Sipariş, ödeme ve üyelik sorunları takip edilemez | Destek talebi, kategori, öncelik, SLA, atama ve çözüm kaydı ekleyin |
| P2 | Veri gizliliği süreçleri | Gizlilik sayfası var; açık rıza sürümü, veri indirme/silme ve saklama politikası görünmüyor | KVKK uyumluluğu operasyonel olarak kanıtlanamaz | Rıza kayıtları, amaç/sürüm/tarih, veri dışa aktarma, anonimleştirme ve saklama işleri ekleyin |

## 3. Süreç bazlı değerlendirme

### Üyelik

Paket özellikleri rezervasyon, PT ve community yetkilerini doğru biçimde etkiliyor. Ancak paket seçiminin finansal bir üyelik sözleşmesine dönüşmesi eksik. Üyelik için şu durum makinesi önerilir:

`PendingPayment → Active → Frozen → Active → Expired/Cancelled`

Her geçiş; tarih, neden, yapan kişi, ödeme ve kalan haklarla birlikte kaydedilmelidir.

### Ders ve PT

Rezervasyon servisinde kapasite, haftalık limit ve mükerrer kayıt kontrolleri güçlü bir başlangıçtır. Eksik olan kulüp operasyon katmanıdır: yoklama, bekleme listesi, geç iptal/no-show yaptırımı, mekan/salon, eğitmen çalışma takvimi ve tekrar eden seans üretimi.

### Shop ve ödeme

Sipariş ve ödeme durumları ayrılmış, stok iadesi ve süresi dolan ödeme işleme mantığı bulunuyor. Ticari güvenlik için adminin finansal sonucu manuel enum değişikliğiyle belirlemesi yerine ödeme sağlayıcısının doğrulanmış sonucu tek kaynak olmalıdır. Mutabakat ekranı ve idempotent webhook kaydı eklenmelidir.

### Kitchen

Uygulamanın en gelişmiş alanlarından biridir: kişiselleştirilmiş plan, öğün/gün atlama, teslimat seçimi, reçete, stok ve üretim planı bulunur. Canlı operasyona geçiş için tedarik, lot/son kullanma tarihi, alerjen, çapraz bulaşma, teslimat bölgesi/slot kapasitesi ve gerçek maliyet hesabı gereklidir.

### Community ve içerik

Etkinlik, challenge, blog ve başarı hikâyeleri içerik tarafını kapsıyor. Normal etkinliklerde kayıt/check-in, challenge'da ödül/doğrulama ve içerikte planlı yayın/onay eksikleri bulunuyor.

### İletişim

Üye–eğitmen mesajlaşması ve gerçek zamanlı bildirimler mevcut. Operasyonel iletişim için e-posta/SMS/push tercihleri, şablonlar, gönderim geçmişi ve destek talepleri eklenmelidir. SMTP varsayılan olarak kapalı olduğundan parola sıfırlama ve kritik bildirimlerin canlı ortam hazırlığı ayrıca doğrulanmalıdır.

## 4. Veri ve operasyon güvenliği

- Kritik tablolarda optimistic concurrency/row version yaklaşımı standartlaştırılmalı; özellikle stok, kapasite ve ödeme yarış koşulları entegrasyon testleriyle doğrulanmalıdır.
- Sipariş, ödeme, stok, üyelik ve admin değişiklikleri silinmemeli; ters kayıt veya durum geçişiyle düzeltilmelidir.
- Kişisel veri ve ödeme loglarında hassas alan maskelemesi yapılmalıdır.
- Yedekleme, geri yükleme tatbikatı, hata izleme, uptime alarmı ve iş kuyruğu gözlemlenebilirliği tanımlanmalıdır.
- Admin için iki faktörlü kimlik doğrulama ve oturum politikası uygulanmalıdır.

## 5. Önerilen teslimat yol haritası

### Faz 1 — Canlıya çıkış güvenliği

1. Üyelik yaşam döngüsü ve üyelik ödemesi.
2. Iyzico canlı yapılandırma, gerçek iade ve mutabakat.
3. Audit log, admin rol ayrımı ve 2FA.
4. SMTP/parola sıfırlama canlı testi.
5. Fatura ve temel KVKK kayıtları.

### Faz 2 — Operasyon verimliliği

1. Ders yoklama, no-show ve bekleme listesi.
2. Eğitmen uygunluk/çakışma takvimi.
3. Kitchen teslimat slotu ve tedarik süreci.
4. Üye detay ve destek operasyon ekranları.
5. Raporlama ve dışa aktarma.

### Faz 3 — Büyüme

1. Kupon/kampanya ve referans programı.
2. Etkinlik check-in ve challenge ödülleri.
3. Segmentli iletişim ve pazarlama izinleri.
4. Retention, churn ve kohort analitiği.

## 6. Kabul kriteri örnekleri

- Süresi biten üyelik, yeni ders/PT/community hakkı kullanamaz; geçmiş kayıtlarını görmeye devam eder.
- İade yalnızca sağlayıcıdan başarı yanıtı gelince `Refunded` olur ve stok/abonelik etkisi bir kez uygulanır.
- Aynı son kontenjana eşzamanlı iki istek geldiğinde yalnız biri rezervasyon oluşturabilir.
- Geç iptal/no-show kuralı paket hakkını tanımlanan politikaya göre etkiler ve üyeye bildirilir.
- Kitchen teslimat günü, yalnız hizmet verilen bölge ve müsait zaman dilimi seçildiğinde onaylanır.
- Her kritik admin değişikliği kim, ne zaman, hangi değeri değiştirdi sorularını cevaplar.

## 7. Analiz sınırı

Bu rapor mevcut kaynak kod, route, ekran, model, servis ve testlerin statik incelenmesine dayanır. Business sözleşmeleri, muhasebe yöntemi, fiziksel kulüp prosedürleri ve hukuki politikalar repoda bulunmadığından öneriler ürün kararı olarak teyit edilmelidir.
