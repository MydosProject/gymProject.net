# QA denetimi, ek test senaryoları ve mimari öneriler — 29 Ekim 2026

**Kapsam:** `no23-V8` / `2bda53e`. Kod incelemesi (backend, UI, dağıtım dosyaları) ve geçici xUnit sondaları. Mevcut [QA bulguları](QA_BULGULARI_2026-10-29.md) ve [60 test senaryosu](TEST_CASELERI_2026-10-29.md) tekrar edilmedi; bu belge onların **devamıdır**.

**Revizyon (29.10.2026, ikinci gözden geçirme sonrası):** Test kapsamı ifadesi, Data Protection riski ve BE-02'nin önceliği düzeltildi; ürün kararı gerektiren değerler ve bakım gözlemleri bulgulardan ayrıldı; uygulama sırası güncellendi. Ek olarak ödeme dışı kullanım/UI bulguları (UI-04…UI-06), gözlenen mimari sorunlar ve modülerleştirme önerisi eklendi (§4, §5.8, §5.9).

**Otomatik test durumu:** 673 test geçti, 0 başarısız. 65 test dosyasının (yardımcı `FakeIyzicoCheckoutClient` hariç) 23'ü veritabanı kullanmayan saf birim testidir (hesaplayıcılar, doğrulayıcılar, model binder'lar). Veritabanı kullanan 42 dosyanın tamamı EF Core InMemory sağlayıcısıyla çalışıyor. Kodda yaygın olan `IsRelational() ? BeginTransaction(...) : null` deseni nedeniyle transaction, `FOR UPDATE`, `Serializable`, unique/check kısıtları ve eşzamanlılık yolları bu testlerde çalışmıyor. **Asıl eksik:** ödeme, stok ve ders hakkı işlemlerinin gerçek PostgreSQL üzerinde kalıcı otomatik testlerinin olmaması. Önceki turlarda yapılan manuel PostgreSQL kontrolleri bu kapsamın yerini tutmaz. Aşağıdaki P0/P1 hataların hiçbiri mevcut paket tarafından yakalanmıyor.

**Kanıt seviyesi:**
- **DOĞRULANDI** — Geçici xUnit sondası hatayı üretti. Sonda dosyası rapor sonrası silindi; bu nedenle kanıt şu an depoda tekrar üretilebilir değil. Sondalar ilgili düzeltmeyle birlikte **kalıcı regresyon testi** olarak eklenmelidir (bkz. §7).
- **KOD** — Kod okumasıyla kesin akış izlendi; çalışma anında henüz üretilmedi.
- **RİSK** — Yapılandırma/tasarım riski; ortama bağlı.

---

## 1. Özet tablo

| ID | Öncelik | Alan | Başlık | Kanıt |
|---|---|---|---|---|
| BE-01 | **P0** | Sipariş | Nakit/havale mağaza siparişleri 30 dk sonra otomatik iptal ediliyor | DOĞRULANDI |
| BE-02 | **P0** | Ödeme | Süresi dolmuş siparişe geç gelen başarılı ödeme → "Ödendi" ama stok tekrar düşülmüyor | DOĞRULANDI |
| SEC-01 | **P0*** | Dağıtım | k8s `Development` modunda çalışıyor; parola sıfırlama bağlantıları loglara yazılıyor | KOD |
| SEC-02 | **P0*** | Dağıtım | k8s Postgres'te kalıcı disk yok; pod yeniden oluşturulursa tüm veri silinir | KOD |
| BE-03 | P1 | İndirim | Kampanya kullanım sayacı başarısız/terk edilmiş ödemelerde de tükeniyor | KOD |
| BE-04 | P1 | Sepet | Kartla ödemede her "Öde" denemesi yeni sipariş açıp stoğu yeniden ayırıyor | KOD |
| BE-05 | P1 | Stok | Stok düşümü kilitsiz oku-kontrol-yaz; eşzamanlı siparişte eksi stok/fazla satış | KOD |
| BE-06 | P1 | Ödeme | Callback ile bekleyen ödeme worker'ı aynı kaydı kilitsiz güncelliyor | KOD |
| BE-07 | P1 | Zaman | 57 yerde sunucu saati (`DateTime.Today/Now`), kulüp saati (`ClubTime`) yerine | KOD |
| SEC-03 | P1 | Dağıtım | Konteyner dağıtımında Data Protection anahtarları kalıcı değil; konteyner yeniden oluşturulunca oturumlar/formlar bozulur | RİSK |
| UI-01 | P1 | Mobil | BUG-02/03 ile aynı etiket kaybı 7 ekranda daha var (taksit, ders hakkı geçmişi dahil) | KOD |
| BE-08 | P2 | Ödeme | Callback'te ödenen tutar sipariş tutarıyla karşılaştırılmıyor | KOD |
| BE-09 | P2 | Güvenlik | Anonim POST'larda hız sınırı yok (misafir sipariş stok kilitleyebilir) | KOD |
| SEC-04 | P2 | Dağıtım | Migration uygulama açılışında; güvenlik başlıkları ve ForwardedHeaders yok | RİSK |
| UI-02 | P2 | Formlar | Genel çift gönderim koruması yok (sunucu tarafı korumaya ek destek) | KOD |
| UI-04 | P1 | Admin | Silme/çıkarma işlemlerinin bir kısmı onay sormadan tek tıkla uygulanıyor | KOD |
| UI-05 | P2 | Üye | Grup dersi rezervasyonu onay sormadan tek tıkla iptal ediliyor | KOD |
| UI-06 | P2 | Admin | Büyüyen listelerde sayfalama ve filtre yok | KOD |

\* SEC-01/SEC-02 yalnız canlı ortam [k8s.yaml](../k8s.yaml) ile çalışıyorsa P0'dır; canlının hangi yapılandırmayla çalıştığı bu incelemede doğrulanmadı. [docker-compose.yml](../docker-compose.yml) tarafında veritabanı diski zaten kalıcı tanımlıdır.

**Bulgu dışı notlar:** CSS bakım gözlemi (bkz. §4, M-01) bir hata kanıtı değildir. Kullanıcı başına kampanya limiti (BE-03) ve hız sınırı değerleri (BE-09, O-22) ürün kararı gerektirir.

---

## 2. Backend bulguları

### BE-01 — Nakit/havale mağaza siparişleri 30 dk sonra iptal ediliyor (P0, DOĞRULANDI)

**Kaynak:** [IyzicoPendingPaymentService.cs:100-109](../src/NO23.Web/Services/Payments/IyzicoPendingPaymentService.cs:100). "Sahipsiz kart siparişi" temizliği `Status=Pending`, `PaymentStatus=Pending`, `PaymentTransactions` boş ve 30 dakikadan eski siparişleri seçiyor. Nakit/havale siparişleri ([ManualOrderPaymentService.ConfigureOfflineOrder](../src/NO23.Web/Services/ManualOrderPaymentService.cs:20)) tam olarak bu koşulları sağlıyor; `IsManualPayment`/`PaymentMethod` filtresi yok.

**Sonda sonucu:** Nakit, 2 adet, 31 dk önce oluşturulmuş sipariş → `processed=1, manual=True, status=Cancelled, payment=Expired, stok 3→5`.

**Etki:** Üye ve misafir kullanıcıların tüm nakit/havale mağaza siparişleri (karışık sepette Kitchen kalemleri dahil) yarım saat sonra sessizce iptal edilir, stok iade edilir, aynı ürün başkasına satılabilir. Teslimatta "iptal edilmiş" sipariş görünür.

**Önlem:** Sorguya `!order.IsManualPayment && order.PaymentMethod == PaymentMethod.CreditCard` ekleyin. Regresyon testi: nakit ve havale için "30 dk sonra Pending kalır, stok değişmez". Canlıda geçmişte bu yolla iptal edilmiş siparişleri `IsManualPayment = true AND PaymentStatus = Expired` ile raporlayıp elle gözden geçirin.

### BE-02 — Süresi dolmuş siparişe geç başarılı callback (P0, DOĞRULANDI)

**Kaynak:** [IyzicoPaymentService.cs:239](../src/NO23.Web/Services/Payments/IyzicoPaymentService.cs:239) yalnız `Paid` durumunu tekrar işlemeden çıkarıyor. `Expired`/`Failed` işlem için callback gelirse iyzico'dan yeniden sorgu yapılır ve `SUCCESS` dönerse sipariş `Paid/Confirmed` olur; fakat stok önceden iade edildiği (`StockRestoredAtUtc` dolu) için tekrar düşülmez. Kitchen aboneliği `Cancelled` durumundan `Active`'e, üyelik yenilemesi aktive edilir.

**Sonda sonucu:** Worker: `Cancelled/Expired`, stok 3→5. Geç callback: `Succeeded=True`, `Confirmed/Paid`, **stok 5 kaldı**.

**Etki:** Fazla satış (satılmış ürün tekrar satılabilir), iptal edilmiş Kitchen aboneliğinin sessizce aktifleşmesi, muhasebe/stok uyuşmazlığı. **Yayın engeli** olarak BE-01 ile aynı adımda ele alınmalıdır.

**Önlem:** Ödeme gerçekten alınmışsa ödeme kaydı (PaymentId, tutar, zaman) mutlaka korunmalı; ancak terminal durumdaki (Expired/Failed/Cancelled) işleme gelen başarılı sonuç otomatik "Ödendi"ye çevrilmemeli. Seçenekler: (a) stoğu atomik olarak yeniden ayırmayı dene, başarısızsa iyzico iade/iptal API'si + yönetici uyarısı; (b) siparişi `RequiresReview` durumuna al. Durum geçişlerini tek bir yerde tanımlayın (bkz. Mimari §5.2).

### BE-03 — Kampanya kullanım limiti terk edilmiş ödemelerde tükeniyor (P1, KOD)

**Kaynak:** [CommerceService.cs:327](../src/NO23.Web/Services/CommerceService.cs:327) `UsedCount++` sipariş oluşturulurken yapılıyor. Projede `UsedCount` azaltan hiçbir kod yok; başarısız/süresi dolmuş/iptal siparişler limiti geri vermiyor. Ayrıca `DiscountCampaign` üzerinde concurrency token olmadığından eşzamanlı iki sipariş aynı son hakkı kullanabilir (lost update). Kullanıcı başına limit de yok.

**Önlem:** Kullanımı ödeme onayında say veya iptal/expire akışında `UsedCount--` (tek seferlik bayrakla) yap; `ExecuteUpdateAsync(... WHERE UsedCount < UsageLimit)` ile atomik artır. Üye başına limit bir **ürün kararıdır**; hata değil, değerlendirilecek bir seçenektir.

### BE-04 — Kartla ödemede her deneme yeni sipariş ve yeni stok ayırması (P1, KOD)

**Kaynak:** [CommerceService.cs:344](../src/NO23.Web/Services/CommerceService.cs:344) kart ödemesinde sepet temizlenmiyor (ödeme sonrası temizleniyor). Kullanıcı iyzico sayfasından geri dönüp tekrar "Öde"ye bastığında, aynı sepetten **yeni** sipariş oluşturulur, stok yeniden düşülür ve kampanya sayacı yeniden artar. Önceki siparişler checkout süresi dolana kadar stoğu kilitler.

**Önlem:** Asıl koruma sunucuda olmalı: aynı işlem yalnız bir kez uygulanmalı. Üyenin aynı sepet için açık (Pending, kart) siparişi varsa onu yeniden kullan veya iptal edip stoğu bırak; form başına idempotency anahtarı (gizli GUID + `Orders.IdempotencyKey` unique index). Düğme kilitleme (UI-02) yalnız ek destektir.

### BE-05 — Stok düşümü kilitsiz (P1, KOD)

**Kaynak:** [CommerceService.cs:291-341](../src/NO23.Web/Services/CommerceService.cs:291) ve misafir akışı (~505-547): stok okunur, kontrol edilir, bellekte azaltılır, `SaveChanges`. Transaction, satır kilidi veya concurrency token yok; veritabanında `StockQuantity >= 0` check kısıtı da yok. Karşılaştırma: ders hakkı (`RemainingClassCredits`) concurrency token ile korunuyor.

**Etki:** Son ürüne eşzamanlı iki sipariş → ikisi de geçer (lost update ile stok 1 düşer ama 2 satılır) veya eksi stok.

**Önlem:** Atomik koşullu güncelleme: `ExecuteUpdateAsync(p => p.SetProperty(x => x.StockQuantity, x => x.StockQuantity - q))` + `Where(x => x.Id == id && x.StockQuantity >= q)`; etkilenen satır 0 ise "stok yok". Ek olarak `HasCheckConstraint("CK_Stock_NonNegative", "\"StockQuantity\" >= 0")` ve/veya `UseXminAsConcurrencyToken()`.

### BE-06 — Callback ve worker yarışı (P1, KOD)

Callback ([IyzicoPaymentService](../src/NO23.Web/Services/Payments/IyzicoPaymentService.cs:206)) ve [IyzicoPendingPaymentService.ReconcileAsync](../src/NO23.Web/Services/Payments/IyzicoPendingPaymentService.cs:130) aynı `PaymentTransaction`/`Order` satırlarını kilitsiz okur-yazar. Worker "Expired + stok iade" yazarken callback "Paid" yazarsa son yazan kazanır; sonuç BE-02 ile aynı tutarsızlıktır. Aynı başarılı callback iki kez paralel gelirse Kitchen planı iki kez üretilmeye çalışılabilir.

**Önlem:** Her iki yolda `SELECT ... FOR UPDATE` (projede `ManualOrderPaymentService` zaten bu deseni kullanıyor) veya `xmin` concurrency token + yeniden dene. BE-05 ve BE-06, gerçek PostgreSQL üzerinde **paralel istek** testleriyle kapatılmalıdır; InMemory bu yarışları üretemez.

### BE-07 — Kulüp saati yerine sunucu saati (P1, KOD)

`ClubTime` (UTC+3) tanımlı ve admin takviminde kullanılıyor, ancak 57 yerde `DateTime.Today/Now` kullanılıyor. Docker/k8s konteynerleri varsayılan olarak UTC'dedir; Türkiye saatiyle **00:00–03:00** arasında "bugün" bir gün geride kalır. Örnekler:

- Kitchen başlangıcı "yarın": [IyzicoPaymentService.cs:573-575](../src/NO23.Web/Services/Payments/IyzicoPaymentService.cs:573), [Member/KitchenController.cs:156](../src/NO23.Web/Areas/Member/Controllers/KitchenController.cs:156) → gece ödenen paket aynı gün başlar.
- Öğün pas geçme izni: [Member/KitchenController.cs:1201](../src/NO23.Web/Areas/Member/Controllers/KitchenController.cs:1201) → bugünkü öğün "yarın" sanılıp pas geçilebilir.
- Kalori/hedef günü: [GoalsController.cs:64](../src/NO23.Web/Areas/Member/Controllers/GoalsController.cs:64), meydan okuma ilerlemesi, PT talebi tarih doğrulaması [PersonalTrainingRequestService.cs:27](../src/NO23.Web/Services/PersonalTrainingRequestService.cs:27), eğitmen takvimi haftası.

**Önlem:** .NET `TimeProvider` + `Europe/Istanbul` `TimeZoneInfo` üzerinden tek bir `IClubClock`; sabit `+3` yerine bölge kuralı. `Microsoft.CodeAnalysis.BannedApiAnalyzers` ile `DateTime.Now/Today` derlemede yasaklanmalı.

### BE-08 — Ödenen tutar doğrulanmıyor (P2, KOD)

Callback'te `ConversationId` ve `BasketId` doğrulanıyor, ancak iyzico'dan dönen `PaidPrice`/`Currency` sipariş `Total` ile karşılaştırılmıyor. Tutar sunucuda belirlendiği için risk düşük; savunma katmanı olarak eklenmeli.

### BE-09 — Anonim POST'larda hız sınırı yok (P2, KOD)

Misafir mağaza siparişi (adet 1–99, stok anında düşülür), randevu formu ve paket başvurusu anonim. Hız sınırı/bot koruması olmadığından bir betik tüm stoğu 30 dakikalık döngülerle kilitleyebilir veya admin listelerini doldurabilir. **Önlem:** `AddRateLimiter` (IP + uç nokta bazlı), misafir siparişte stok rezervasyon üst sınırı, isteğe bağlı CAPTCHA. Sınır değerleri (istek/dakika, adet üst sınırı) **ürün kararıdır**; bu raporda önerilen bir değer yoktur.

---

## 3. Dağıtım ve güvenlik bulguları

SEC-01 ve SEC-02 yalnız canlı ortam [k8s.yaml](../k8s.yaml) ile çalışıyorsa acildir. İlk adım: canlının hangi manifest/compose dosyası ve hangi ortam değişkenleriyle çalıştığını doğrulamak.

### SEC-01 — k8s ortamı Development modunda (P0, koşullu)

[k8s.yaml](../k8s.yaml) `ASPNETCORE_ENVIRONMENT=Development`. Sonuçları ([Program.cs:202-210, 244](../src/NO23.Web/Program.cs:202)):
- Swagger ve `/api/*` geliştirme uç noktaları açık; ayrıntılı hata sayfası (stack trace) ve `UseMigrationsEndPoint` aktif.
- SMTP ayarı yoksa [DevelopmentEmailSender.cs:13](../src/NO23.Web/Services/Email/DevelopmentEmailSender.cs:13) **parola sıfırlama bağlantısını tam haliyle loga yazar** → log erişimi olan herkes hesap ele geçirebilir.
- HSTS ve genel hata yakalayıcı devre dışı.

**Önlem:** `Production`; ortama göre ayrı yapılandırma; DevelopmentEmailSender yalnız gerçekten yerel ortamda.

### SEC-02 — Veri kalıcılığı ve gizli bilgiler (P0, koşullu)

k8s'teki Postgres `Deployment`'ında volume yok → pod yeniden oluşturulursa **tüm üye, ödeme ve ders verisi kaybolur**. (Docker Compose'da `no23_postgres_data` volume'u tanımlı; bu sorun yalnız k8s manifestinde.) Parola manifestte düz metin (`change_me`), uygulama imajı `:latest`, liveness/readiness probe ve kaynak limitleri yok.

**Önlem:** Yönetilen PostgreSQL veya `StatefulSet` + `PersistentVolumeClaim`; `Secret` nesnesi; sürüm etiketli imaj; probe'lar; otomatik yedek ve geri yükleme provası.

### SEC-03 — Data Protection / çoklu kopya (P1, konteyner dağıtımında)

Uygulamada anahtar saklama yeri yapılandırılmamış (`AddDataProtection().PersistKeys...` yok). Windows/IIS'te ASP.NET Core anahtarları varsayılan olarak kullanıcı profilinde/kayıt defterinde saklayabilir; bu ortamda risk düşüktür. Linux konteynerinde ise anahtarlar konteynerin geçici dosya sistemine yazılır: **konteyner yeniden oluşturulduğunda** (k8s pod yeniden başlatma, yeni imaj yayını) tüm oturumlar düşer, açık formlar antiforgery hatası (400) verir. Kalıcılık konteyner dağıtımında ayrıca sağlanmalıdır. Oturum `DistributedMemoryCache`, SignalR'da backplane yok, iki hosted service (`SessionCompletion`, `IyzicoPendingPayment`) her kopyada çalışır. Bu haliyle `replicas > 1` güvenli değildir.

**Önlem:** `PersistKeysToDbContext`; ölçeklenecekse Redis (oturum + SignalR backplane) ve worker'lar için `pg_try_advisory_lock` ile lider seçimi.

### SEC-04 — Diğer sertleştirmeler (P2)

- Migration uygulama açılışında ([Program.cs:193-197](../src/NO23.Web/Program.cs:193)): çok kopyada yarış, başarısız migration'da uygulama hiç açılmaz. `dotnet ef migrations bundle` ile ayrı Job.
- CSP, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` başlıkları yok.
- Proxy/Ingress arkasında `UseForwardedHeaders` yok: HTTPS yönlendirme döngüsü ve iyzico dönüş adresi kontrolündeki `Request.Host` karşılaştırması etkilenebilir.
- Sağlık kontrolü yalnız Development `/api/health`; üretimde `/health/live` ve DB'li `/health/ready` gerekli.

---

## 4. UI bulguları

### UI-01 — Mobilde etiketsiz tablo hücreleri, 7 ekran daha (P1, KOD)

[admin.css](../src/NO23.Web/wwwroot/css/admin.css) `max-width: 991.98px` altında **tüm** `.admin-content .table` başlıklarını gizliyor ve etiketi `td[data-label]` üzerinden bekliyor. BUG-02/03 düzeltmesi yalnız iki ekranı kapsadı. `data-label` içermeyen tablolar:

| Ekran | Etki |
|---|---|
| [Members/Payments.cshtml:29](../src/NO23.Web/Areas/Admin/Views/Members/Payments.cshtml:29) | Taksit no, **vade**, tutar, **tahsilat tarihi**, referans etiketsiz; vade ile tahsilat tarihi karışır (para) |
| [Members/Credits.cshtml:28](../src/NO23.Web/Areas/Admin/Views/Members/Credits.cshtml:28) | Ders hakkı geçmişi: tür/durum/hak işlemi etiketsiz |
| [ClassSessions/Index.cshtml](../src/NO23.Web/Areas/Admin/Views/ClassSessions/Index.cshtml:85) | Seans listesi |
| [KitchenStock/Index.cshtml](../src/NO23.Web/Areas/Admin/Views/KitchenStock/Index.cshtml:119) | 5 tablo, 32 hücre (stok miktarı/birim/hareket) |
| [ServicePackages/Index.cshtml](../src/NO23.Web/Areas/Admin/Views/ServicePackages/Index.cshtml:53) | Paket listesi |
| [KitchenAllergens/Index.cshtml](../src/NO23.Web/Areas/Admin/Views/KitchenAllergens/Index.cshtml:10) | Alerjen listesi |
| [Trainer/Members/Index.cshtml](../src/NO23.Web/Areas/Trainer/Views/Members/Index.cshtml:15) | Eğitmenin üye listesi (eğitmen düzeni de `.admin-content` kullanıyor) |

**Kalıcı önlem:** Etiketi elle yazmak yerine (a) kart dönüşümünü opt-in sınıfa bağlayın (`.table-cards`), veya (b) `thead th` metninden `data-label`'ı üreten bir TagHelper/partial kullanın. Playwright testi: 390 px'de görünen her `td::before` içeriği boş olmamalı.

### UI-02 — Çift gönderim koruması yok (P2)

`site.js`'de gönderimde düğmeyi kilitleyen genel bir davranış yok. Sunucu tarafı idempotent olmayan akışlarda (BE-04 sepet ödemesi, sipariş oluşturma) çift tıklama mükerrer kayıt üretir. **Önlem:** `form[data-once]` için submit sonrası `disabled` + yükleniyor durumu; asıl koruma sunucuda idempotency anahtarı.

### M-01 — CSS bakım gözlemi (bulgu değil)

`admin.css` + `site.css`'de 187 `!important`, 156 media query; aynı bileşen için birden fazla dosyada çakışan kurallar var. Bu sayılar tek başına hata kanıtı değildir; bakım maliyeti ve regresyon olasılığı göstergesidir (üye tablosu düzeltmesinin nedeni de bir `.table` kuralına yenilmekti). **Öneri:** Bileşen bazlı dosyalar, tasarım token'ları (CSS değişkenleri zaten kısmen var), görsel regresyon testi.

### UI-04 — Onaysız silme/çıkarma (P1, KOD)

Aşağıdaki işlemler tek tıkla, onay penceresi olmadan sunucuya gider:

| Ekran | İşlem | Not |
|---|---|---|
| [Admin/Calendar/Index.cshtml:124](../src/NO23.Web/Areas/Admin/Views/Calendar/Index.cshtml:124) | Üyeyi gruptan "Çıkar" | Ders hakkını etkiler |
| [KitchenAllergens/Index.cshtml:19](../src/NO23.Web/Areas/Admin/Views/KitchenAllergens/Index.cshtml:19) | Alerjen "Sil" | |
| [MembershipPackageOptions/Index.cshtml:8](../src/NO23.Web/Areas/Admin/Views/MembershipPackageOptions/Index.cshtml:8) | Paket seçeneği "Sil" | |
| [ServicePackages/Edit.cshtml:4](../src/NO23.Web/Areas/Admin/Views/ServicePackages/Edit.cshtml:4) | Varyant "Sil" | |
| [MemberProgress/Index.cshtml:81](../src/NO23.Web/Areas/Admin/Views/MemberProgress/Index.cshtml:81) | "Fotoğrafı sil" | Geri alınamaz |

Davranış tutarsızdır: ders seansı iptali, Kitchen menü silme, grup dersi kaldırma ve etkinlik rezervasyonu iptalinde onay penceresi veya `confirm` vardır. **Önlem:** Tek bir `_ConfirmDialog` partial'ı (veya `data-confirm` niteliğini işleyen ortak JS); tüm sil/iptal/çıkar işlemleri bunu kullanır.

### UI-05 — Üyenin grup rezervasyonu tek tıkla iptal oluyor (P2, KOD)

[Member/Reservations/Index.cshtml:105](../src/NO23.Web/Areas/Member/Views/Reservations/Index.cshtml:105) ve Member ana sayfasındaki "İptal Et" formu onay sormuyor. Kişisel antrenman iptalindeki `data-personal-training-cancel-form` ve ilgili JS yalnız güncel duruma göre formu/düğmeyi değiştiriyor; kullanıcı onayı veya gönderim sırasında çift tıklama kilidi sağlamıyor. Dolu bir derste yanlış tıklayan üye yerini kaybeder. **Önlem:** Grup ve PT iptallerini UI-04 ile aynı ortak onay bileşeni üzerinden değerlendirin; gönderim korumasını ayrıca UI-02 kapsamında doğrulayın.

### UI-06 — Sayfalama ve filtre eksikliği (P2, KOD)

24 admin/eğitmen liste ekranının `Index` action'ında sayfalama yok. Zamanla en çok büyüyecek olanlar: Üyeler, Siparişler, Randevu talepleri, PT talepleri, Paket başvuruları, Kitchen abonelikleri. Kayıt arttıkça sayfa yavaşlar ve aranan kayıt bulunamaz hale gelir (mevcut Y-08 performans vakasıyla ilişkili). **Önlem:** Ortak sayfalama + arama/durum filtresi bileşeni; önce Üyeler ve Siparişler.

**Kontrol edilip sorun bulunmayanlar:** Görsellerde `alt` metni, tüm düzenlerde `lang="tr"`, Türkçe doğrulama mesajları.

---

## 5. Mimari öneriler

### 5.1 Test stratejisi (en yüksek getiri)

1. **Testcontainers + gerçek PostgreSQL** ile entegrasyon katmanı. Mevcut saf birim testler korunur; veritabanı davranışına bağlı testler gerçek sağlayıcıya taşınır. InMemory; transaction, kilit, `Serializable` çakışması (40001), unique/check kısıtı, `FOR UPDATE` ve `xmin` davranışını çalıştırmaz — Microsoft'un EF Core test rehberi de bu nedenle üretim veritabanıyla test önerir. Para/hak/stok akışları için zorunlu.
2. **`WebApplicationFactory`** ile HTTP düzeyi testleri: yetki (Y-01…Y-04), antiforgery, model binding, yönlendirmeler.
3. **Playwright** ile UI: 390/768/1366 px, etiket kuralı, yatay taşma, çift tıklama.
4. CI'da `dotnet test` + kapsama raporu; P0 alanlarında (ödeme, stok, ders hakkı) kapsama eşiği.

### 5.2 Ödeme/sipariş durum makinesi

Sipariş ve ödeme durumları şu an callback, worker, admin ve `OrderWorkflowService` içinde ayrı ayrı değiştiriliyor. Öneri: izin verilen geçişleri tek bir yerde tanımlayan bir `OrderStateMachine` (örn. `Pending → Paid | Failed | Expired`, `Expired → RequiresReview` ama asla doğrudan `Paid`). Tüm yazanlar bu servisten ve satır kilidiyle geçmeli. BE-01, BE-02, BE-06 bu tasarımla yapısal olarak engellenir.

### 5.3 Eşzamanlılık standardı

`Order`, `PaymentTransaction`, `ShopProduct`, `ShopProductVariant`, `DiscountCampaign`, `KitchenSubscription` için `UseXminAsConcurrencyToken()`. Sayaç ve stok için atomik `ExecuteUpdateAsync`. Mevcut `ConcurrentUpdateFilter` zaten kullanıcı dostu mesaj veriyor; kapsamı genişler.

### 5.4 İnce controller, uygulama servisleri

54 controller doğrudan `ApplicationDbContext` kullanıyor; [Member/KitchenController.cs](../src/NO23.Web/Areas/Member/Controllers/KitchenController.cs) 1505 satır. İş kuralı controller'da kaldıkça test edilmesi HTTP katmanına bağlanıyor. Büyük bir yeniden yazım yerine: para/hak/stok akışlarından başlayarak iş kuralını servislere (veya feature klasörlü dikey dilimlere) taşıyın; controller yalnız doğrulama + yönlendirme yapsın. Tek proje yapısı bu ölçekte kabul edilebilir; katman ayrımı klasör ve bağımlılık kuralıyla yeterli.

### 5.5 Zaman soyutlaması

`TimeProvider` enjeksiyonu + `IClubClock` (Europe/Istanbul). Testlerde `FakeTimeProvider` ile gece yarısı/ay sonu/yaz saati senaryoları deterministik olur. `DateTime.Now/Today` analizörle yasaklanır.

### 5.6 Üretime hazırlık

| Konu | Öneri |
|---|---|
| Ortam | `Production`; Development yalnız yerel |
| Gizli bilgiler | k8s `Secret` / Key Vault; manifestte parola yok |
| Veritabanı | Yönetilen Postgres veya StatefulSet + PVC; günlük yedek + geri yükleme provası |
| Migration | Ayrı Job (`migrations bundle`), açılışta değil |
| Data Protection | `PersistKeysToDbContext` |
| Ölçekleme | Redis oturum + SignalR backplane; worker'larda advisory lock |
| Gözlemlenebilirlik | Yapılandırılmış log (Serilog/OpenTelemetry), istek kimliği, ödeme/stok olayları için metrik ve alarm (ör. "Paid ama StockRestoredAtUtc dolu" sayısı) |
| Güvenlik | Rate limiting, güvenlik başlıkları, ForwardedHeaders, NuGet güvenlik uyarılarının (teslimat notunda 2 adet) giderilmesi |
| Sağlık | `/health/live`, `/health/ready` + k8s probe |

### 5.7 Arka plan işleri ve yan etkiler

Bildirim/SignalR yayınları `SaveChanges` sonrasında yapılıyor (doğru). Kopya sayısı artacaksa Outbox tablosu + tek işleyici, iş zamanlayıcı olarak Quartz/Hangfire (Postgres depolama) düşünülebilir.

### 5.8 Modülerleştirme: modüler monolit (mikroservis değil)

Uygulamayı daha modüler hale getirmek mantıklıdır; ancak **mikroservise bölmek önerilmez**. Ders hakkı, ödeme, Kitchen aboneliği ve stok aynı işlem içinde tutarlı kalmak zorunda; ayrı servisler ve veritabanları bu raporda bulunan tutarlılık sorunlarını büyütür. Hedef: **tek uygulama ve tek veritabanı, sınırları net modüller.**

Önerilen modüller:

| Modül | Kapsam |
|---|---|
| Kimlik | Hesap, roller, parola işlemleri |
| Üyelik ve Haklar | Paketler, ders hakları, ölçüm ve gelişim |
| Takvim ve Rezervasyon | Grup dersleri, PT, randevular, otomatik tamamlama |
| Ticaret | Sepet, sipariş, ödeme, mağaza stoğu, kampanya |
| Kitchen | Menü, abonelik, haftalık plan, üretim, mutfak stoğu |
| Topluluk ve İçerik | Etkinlik, challenge, blog, başarı hikâyeleri |
| Mesajlaşma ve Bildirim | Eğitmen sohbeti, kullanıcı bildirimleri |

**Kademeli yol (büyük yeniden yazım olmadan):**
1. Klasörleri modüle göre düzenle: her modülün controller, servis ve view model'leri kendi klasöründe. Proje bölmeye gerek yok.
2. Modüller birbirinin tablolarına doğrudan değil, servisleri üzerinden erişsin (ör. Ticaret, Kitchen aboneliğini `KitchenSubscriptionService.Activate(...)` ile aktive eder).
3. Sınırları NetArchTest gibi mimari testlerle CI'da koru.
4. Sınırlar oturduktan sonra, gerekirse modülleri ayrı `.csproj` projelerine böl. Bu adım isteğe bağlıdır.

Yeni özellik geliştirmeyi durdurmadan, her sprintte bir modül taşımak en gerçekçi yoldur. Başlangıç için en riskli ve en çok kopyalanmış mantığın bulunduğu **Ticaret** ve **Kitchen** önerilir.

### 5.9 Gözlenen mimari sorunlar

| Sorun | Kanıt | Neden önemli |
|---|---|---|
| Kalın controller'lar | 54 controller `ApplicationDbContext`'i doğrudan kullanıyor; [Member/KitchenController.cs](../src/NO23.Web/Areas/Member/Controllers/KitchenController.cs) 1505 satır, view'ı 1155 satır | İş kuralı HTTP katmanında kalıyor; test ve yeniden kullanım zorlaşıyor |
| Tutarsız bağımlılık yönetimi | 42 yerde `new XService(dbContext)` (en çok `LessonCreditSummaryService` 11, `MembershipPricingService` 6); 6 constructor'da opsiyonel servis (`Service? x = null`) ve kullanım yerinde `?? new ...` | Bağımlılıklar görünmez; opsiyonel bildirim servisi verilmezse bildirim sessizce atlanır; test kolaylığı üretim koduna sızmış |
| Kopyalanmış iş mantığı | `ActivateKitchenPackageAsync` iki serviste ayrı ayrı: [IyzicoPaymentService.cs:518](../src/NO23.Web/Services/Payments/IyzicoPaymentService.cs:518), [IyzicoPendingPaymentService.cs:421](../src/NO23.Web/Services/Payments/IyzicoPendingPaymentService.cs:421) | Biri düzeltilip diğeri unutulursa davranış ayrışır |
| Dağınık transaction kuralları | Her servis kendi izolasyon seviyesini seçiyor; `IsRelational() ? transaction : null` deseni | Hangi işlemin hangi garantiyle çalıştığı izlenemiyor; testlerde transaction kodu çalışmıyor |
| Statik zaman ve yardımcılar | Statik yardımcılar ve doğrudan sistem saatine erişim; `ClubTime` sabit +3, farklı akışlarda `DateTime.Now/Today` kullanımı | Doğrudan saat okuyan iş kurallarında gün sınırı testleri zorlaşır. Saf hesaplayıcıların statik olması tek başına hata değildir; zaman bağımlılığı ayrıca ele alınmalı |
| Dolu `Program.cs` | ~300 satır inline geliştirme API'si | Başlangıç kodu okunaksız; endpoint'ler ayrı dosyaya taşınmalı |
| Ortak UI bileşeni eksikliği | Tablo, onay penceresi, iptal düğmesi her ekranda yeniden yazılmış; 16 inline script bloğu | UI-01, UI-04, UI-05 gibi aynı hata sınıfı tekrar tekrar ortaya çıkıyor |

**Öncelikli tavsiyeler:**
1. **Ortak UI bileşenleri:** Mobil etiketler ve ortak onay bileşeniyle UI-01, UI-04, UI-05'i ele al. Basit tablolar başlıktan etiket üretebilir; çok satırlı başlık, `colspan`, boş liste ve işlem hücreleri ayrıca tasarlanmalı. Kapanış, etkilenen ekranlarda tarayıcı ve sunucu kontrolleriyle doğrulanmalı.
2. **Ortak sayfalama/filtre:** UI-06; önce Üyeler ve Siparişler.
3. **DI temizliği:** İş servisi bağımlılıklarını açıkça enjekte et; opsiyonel bağımlılıkların davranışını belirle ve uygun servis kayıtlarını yap. Bu değişiklik risksiz değildir: servis yaşam süreleri, hosted service scope'ları, test kurulumları ve döngüsel bağımlılıklar kontrol edilmeli. Modül bazında ilerlenmeli.
4. **`TimeProvider` + İstanbul saati:** BE-07'yi çözer, testleri deterministik yapar.
5. **Modül klasörleri:** Ticaret ve Kitchen'dan başla; kopya `ActivateKitchenPackageAsync` tek Kitchen servisine taşınsın.
6. **Gerçek PostgreSQL entegrasyon testleri:** Taşıma sırasında davranışın değişmediğini güvenceye alır.

---

## 6. Ek test senaryoları (mevcut 60'a ek)

Kurallar ve öncelik tanımları [TEST_CASELERI](TEST_CASELERI_2026-10-29.md) ile aynıdır. "Bağlı" sütunu ilgili bulguyu gösterir; ilk turda bu vakalar **BAŞARISIZ** beklenir.

### O — Ödeme, sipariş, stok (ek)

| ID | Öncelik | Önkoşul ve adımlar | Beklenen | Bağlı |
|---|---|---|---|---|
| O-11 | P0 | Üye sepetinden **nakit** ile mağaza siparişi ver; 31 dk bekle (veya worker'ı tetikle). | Sipariş Pending kalır; stok değişmez; admin listesinde görünür. | BE-01 |
| O-12 | P0 | O-11'i **havale** ve **misafir** siparişiyle tekrarla; karışık sepet (mağaza + Kitchen) de dene. | Hiçbiri otomatik iptal edilmez. | BE-01 |
| O-13 | P0 | Kart siparişi başlat; checkout süresini geçir; worker Expired yapsın; ardından aynı token için SUCCESS callback gönder. | Sipariş otomatik "Ödendi" olmaz; inceleme/iade akışı tetiklenir; stok tutarlı (satılan adet tekrar satılamaz). | BE-02 |
| O-14 | P0 | Kitchen paketi için O-13'ü uygula. | İptal edilmiş abonelik sessizce Active olmaz. | BE-02 |
| O-15 | P0 | Stok=1 ürüne iki ayrı oturumdan aynı anda sipariş gönder (paralel HTTP). | Tek sipariş başarılı; diğeri "stok yok"; stok ≥ 0. | BE-05 |
| O-16 | P1 | Kartla ödemeye geç, iyzico sayfasından geri dön, tekrar "Öde" (3 kez). | Tek açık sipariş; stok bir kez ayrılmış; kampanya sayacı bir kez artmış. | BE-04 |
| O-17 | P1 | Limit=1 kampanya kodu; kartla siparişi başlat, ödemeyi başarısız yap/süresini doldur; başka üye kodu kullansın. | Başarısız ödeme limiti tüketmez; ikinci üye kodu kullanabilir. | BE-03 |
| O-18 | P1 | Limit=1 kampanyayı iki oturumdan eşzamanlı kullan. | Yalnız biri indirim alır. | BE-03 |
| O-19 | P0 | Aynı başarılı callback'i **paralel** iki kez gönder (Kitchen siparişi). | Tek ödeme etkisi; tek beslenme planı; hata yok. | BE-06 |
| O-20 | P0 | Worker turu ile callback'i aynı anda tetikle (sahte iyzico gecikmesiyle). | Sonuç ya Paid (stok düşük) ya Expired (stok iade); ikisinin karışımı yok. | BE-06 |
| O-21 | P2 | Sahte istemciyle `PaidPrice` farklı SUCCESS yanıtı döndür. | Sipariş Paid olmaz; uyarı loglanır. | BE-08 |
| O-22 | P1 | Misafir olarak aynı IP'den, ürün ekibinin belirleyeceği eşiğin üzerinde sipariş gönder (50/dk yalnız örnek test parametresidir). | Hız sınırı devreye girer; stok kilitlenmez. | BE-09 |

### Z — Zaman ve gün sınırı (yeni grup)

Tüm Z vakaları sunucu/konteyner saat dilimi **UTC** iken, Türkiye saatiyle 00:30'da (UTC 21:30, önceki gün) çalıştırılır. `FakeTimeProvider` ile otomatikleştirilmeli.

| ID | Öncelik | Adımlar | Beklenen | Bağlı |
|---|---|---|---|---|
| Z-01 | P1 | TR 00:30'da Kitchen paketi öde. | Başlangıç TR takvimine göre **yarın**. | BE-07 |
| Z-02 | P1 | TR 00:30'da bugünkü öğünü pas geçmeyi dene. | Bugünkü öğün pas geçilemez. | BE-07 |
| Z-03 | P1 | TR 00:30'da kalori kaydı gir ve Hedefler sayfasını aç. | Kayıt TR'deki bugüne yazılır ve bugünde görünür. | BE-07 |
| Z-04 | P1 | TR 00:30'da bugünün tarihiyle PT talebi oluştur. | "Geçmiş tarih" hatası verilmez. | BE-07 |
| Z-05 | P1 | TR Pazartesi 01:00'de admin ve eğitmen takvimini aç. | İkisi aynı haftayı gösterir. | BE-07 |
| Z-06 | P2 | Meydan okuma bitiş günü TR 23:30 ve 00:30'da ilerleme gir. | Gün sınırı TR saatine göre uygulanır. | BE-07 |

### G — Görsel (ek)

| ID | Öncelik | Ekran | Adımlar | Beklenen | Bağlı |
|---|---|---|---|---|---|
| G-13 | P1 | Üyeler → Ödeme geçmişi | 3 taksitli, 1'i ödenmiş satışı 390 px'de aç. | Taksit no, vade, tutar, tahsilat tarihi, referans etiketli. | UI-01 |
| G-14 | P1 | Üyeler → Ders hakları | Geçmişi olan üyeyi 390 px'de aç. | Tarih/tür/ders/durum/hak işlemi etiketli. | UI-01 |
| G-15 | P1 | Kitchen Stok | 5 tabloyu 390/768 px'de incele. | Malzeme, miktar, birim, hareket türü etiketli. | UI-01 |
| G-16 | P2 | Seanslar, Hizmet paketleri, Alerjenler, Eğitmen → Üyeler | 390 px. | Etiketsiz hücre sayısı 0. | UI-01 |
| G-17 | P2 | Tüm admin/eğitmen listeleri | Otomatik tarama: 390 px'de görünür her `td` için `::before` boş değil. | 0 ihlal. | UI-01 |
| G-18 | P2 | Ödeme, sipariş, ders kaydet formları | Gönder düğmesine hızlı 3 tıkla. | Düğme kilitlenir; tek kayıt. | UI-02 |
| G-19 | P1 | Admin takvim, alerjen, paket seçeneği, varyant, ölçüm fotoğrafı | Her sil/çıkar düğmesine bir kez tıkla, ardından onay penceresinde Vazgeç'i seç. | Onay penceresi açılır; Vazgeç sonrası kayıt ve ders hakkı değişmez. | UI-04 |
| G-20 | P2 | Üye → Rezervasyonlar ve ana sayfa | Grup dersi rezervasyonunda "İptal Et"e tıkla, Vazgeç'i seç; sonra onayla. | İlk denemede rezervasyon korunur; onay sonrası iptal edilir ve hak bir kez iade edilir. | UI-05 |
| G-21 | P2 | Üyeler, Siparişler, talep listeleri | 1000 kayıtlık test verisinde listeyi aç; ad/numara/durumla filtrele; son sayfaya git. | Sayfa başına sınırlı kayıt; filtre doğru sonuç verir; sayfa yüklemesi kabul edilen bütçe içinde. | UI-06 |

### D — Dağıtım ve dayanıklılık (yeni grup)

| ID | Öncelik | Adımlar | Beklenen | Bağlı |
|---|---|---|---|---|
| D-01 | P0 | Üretim ortamında `/swagger`, `/api/health`, olmayan bir sayfada hata üret. | Swagger/api 404; hata sayfası stack trace içermez. | SEC-01 |
| D-02 | P0 | SMTP kapalıyken parola sıfırlama iste; uygulama loglarını incele. | Logda token/bağlantı yok. | SEC-01 |
| D-03 | P0 | Test kümesinde Postgres pod'unu sil; yeniden oluşunca veriyi kontrol et. | Veri korunur. | SEC-02 |
| D-04 | P1 | Oturum açık + form açıkken uygulama pod'unu yeniden başlat; formu gönder. | Oturum korunur; form antiforgery hatası vermez. | SEC-03 |
| D-05 | P1 | 2 kopya ile çalıştır; aynı süresi dolan dersin tamamlanmasını ve bildirimleri izle. | Tek işlem; mükerrer bildirim/hak düşümü yok. | SEC-03 |
| D-06 | P2 | Hatalı bir migration ile yayın provası yap. | Migration Job başarısız olur, eski sürüm hizmet vermeye devam eder. | SEC-04 |
| D-07 | P2 | Yanıt başlıklarını kontrol et. | CSP, X-Frame-Options, Referrer-Policy, HSTS mevcut. | SEC-04 |

---

## 7. Önerilen sıra

1. **Ödeme / iptal / stok bütünlüğü (yayın engeli):** BE-01 ve BE-02 düzeltmeleri, ardından BE-05 ve BE-06 (durum makinesi + kilit/xmin). Testcontainers altyapısı ve O-11…O-15, O-19, O-20'nin gerçek PostgreSQL'de otomasyonu. Canlıda BE-01/BE-02'den etkilenmiş siparişleri raporla.
2. **Canlı ortam ve yedek doğrulaması:** Canlının k8s.yaml ile çalışıp çalışmadığını doğrula; çalışıyorsa SEC-01, SEC-02 (ortam, kalıcı disk, secret) ve SEC-03. Yedekten geri yükleme provası (D-03).
3. **Tekrar ödeme ve kampanya:** BE-04 (sunucu tarafı idempotency), BE-03, UI-02 (düğme kilidi ek destek). O-16…O-18, G-18.
4. **Saat ve mobil/kullanım ekranları:** BE-07 (`TimeProvider`, Z-01…Z-06), UI-01 (önce ödeme ve ders hakkı tabloları G-13/G-14, sonra TagHelper ile kalıcı çözüm + G-17 otomatik tarama), UI-04/UI-05 ortak onay bileşeni (G-19/G-20), UI-06 sayfalama (G-21).
5. **Kademeli mimari düzenleme:** DI temizliği (§5.9), modül klasörlerine geçiş Ticaret ve Kitchen ile başlayarak (§5.8), rate limiting (değerler ürün kararıyla), güvenlik başlıkları, BE-08, M-01 CSS sadeleştirme.

**Regresyon testi kuralı:** Her düzeltme önce ilgili vakanın başarısız olduğunu gerçek PostgreSQL'de göstermeli, sonra geçmeli. BE-01 ve BE-02 sondaları (nakit sipariş + 31 dk; Expired + geç SUCCESS callback) silinmiş durumda; bu kurgular ilgili düzeltmeyle aynı değişiklikte **kalıcı regresyon testi** olarak depoya eklenmelidir.

## 8. Ödeme dışı kullanım bulgularının değerlendirmesi

**Kanıt sınırı:** Bu ek değerlendirme kaynak kod karşılaştırmasına dayanır; bu turda tarayıcıda yeniden üretim veya performans ölçümü yapılmadı. UI-04/UI-05'te onaysız formlar, Üyeler ve Randevu Talepleri listelerinde sınırsız `ToListAsync` sorguları görüldü. Bildirilen 24 ekranın tamamı bu turda yeniden sayılmadı. Sayfalama eksikliği doğrulandı; mevcut kullanıcıların ölçülmüş bir yavaşlık yaşadığı sonucu çıkarılmamalı.

**Önerilen ödeme dışı sıra:**
1. **Silme/çıkarma ve rezervasyon iptali:** Etkilenen üye/ders/tarih gösterilerek açık onay alınmalı. Vazgeçildiğinde istek gönderilmemeli; onaylandığında mevcut yetki, iptal süresi ve ders hakkı kuralları korunmalı. Hak iadesi yalnız bir kez uygulanmalı.
2. **Mobil tablolar:** Önce ders hakkı geçmişi ve eğitmenin üye listesi, ardından seans ve Kitchen stok ekranları. Alan adları, uzun isimler ve işlem düğmeleri birlikte kontrol edilmeli; yalnız `data-label` bulunması yeterli kabul edilmemeli.
3. **Kulüp tarihi:** Kitchen öğünleri, hedef kayıtları ve talep tarihlerinde Türkiye gün sınırı tutarlı olmalı. Eğitmen takviminin ana `Index` akışı zaten `ClubTime.Now` kullanıyor; sunucu tarihine dönen POST yönlendirmeleri ayrıca kontrol edilmeli.
4. **Liste yönetimi:** Ödeme ayrı ele alınırken Üyeler ve talep listelerinden başlanmalı. Arama, durum filtresi ve sayfalama veritabanı sorgusunda uygulanmalı; eş isimli üyeler telefon/e-posta veya üye numarasıyla ayırt edilebilmeli. Sıralama kararlı olmalı, filtreler sayfa geçişinde korunmalı.
5. **Çift gönderim:** Düğme kilidi kullanıcıya işlem durumunu göstermeli; doğrulama veya ağ hatasında yeniden denemeye izin vermeli. Her sunucu işleminin tekrar gönderim davranışı ayrıca incelenmeli; bütün formların mükerrer kayıt ürettiği varsayılmamalı.

**Modülerleştirme yaklaşımı:** Modüler monolit bu uygulama için uygun bir yönelimdir. Modül sınırları iş kurallarının sahipliğini ve ortak işlemlerin transaction sorumluluğunu tanımlamalı. Klasör taşıma tek başına modülerlik sağlamaz. Önce davranış ve HTTP testleri eklenmeli, ardından küçük servis çıkarımları ve DI düzenlemesi yapılmalı; controller/view taşıma sırasında Area, route ve Razor view keşfi korunmalı. Ödeme ayrı çalışma alanı olarak bırakılırken Takvim/Rezervasyon ve Üyelik/Haklar üzerinde sınırlı bir başlangıç yapılabilir.

Kaynaklar: [Microsoft — ortak web uygulaması mimarileri](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures), [Microsoft — bağımlılık enjeksiyonu rehberi](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines).

## 9. Uygulama durumu — ödeme dışı ilk paket

`codex/qa-kullanim` dalında sıralı uygulama yapıldı. Ayrıntı ve ekran bazlı kontrol adımları: [Ödeme dışı kullanım teslimatı](TESLIMAT_KULLANIM_2026-10-29.md).

| Bulgu | Güncel durum |
|---|---|
| UI-04 / UI-05 | Hedeflenen onaysız admin/üye işlemlerine ortak onay eklendi; PT ve ön görüşme iptali de kapsandı. Vazgeçmede sıfır POST, iptalde mevcut hak kuralları doğrulandı. |
| UI-01 | Ödeme dışındaki hedef tablolara ortak mobil etiket desteği ve eğitmen kartlarına kontrast düzeltmesi uygulandı. Ödeme geçmişi tablosu ayrı iş olarak açık. Kitchen reçetelerinin bütün dolu durumları için manuel kabul kapsamı tamamlanmadı. |
| BE-07 | Ödeme dışı sunucu günü kullanımları İstanbul gününe bağlandı; kontrollü saatle sınır testleri eklendi. Ödeme servisleri ve checkout tarihleri ayrı pakette açık. |
| UI-06 | Üyeler ve üç talep listesinde arama/durum filtresi ve sunucuda sayfalama var. Diğer uzun listelere yayılması açık. |
| UI-02 | Seçili native POST formlarında tek gönderim ve işlem göstergesi var. Düğme değeri/validasyon/geri dönüş test edildi. Bütün sunucu işlemleri için genel idempotency tamamlanmış sayılmaz. |
| Mimari | Üye listeleme ayrı Membership servisine çıkarıldı; DI ve saat bağımlılığı açık. Modülerleşmenin ilk adımıdır; diğer modüller ve mimari sınır testleri açık. |

Son otomatik tur **695 başarılı / 0 başarısız / 0 atlanan** (başlangıç 673). Ayrı PostgreSQL veritabanı ve tarayıcıda hedeflenen smoke kontrolleri geçti. Kalıcı PostgreSQL/Playwright CI kapsamı, ödeme bulguları ve canlı dağıtım doğrulaması bu teslimatta tamamlanmadı. İlk raporun kanıt ve kapsam notları tarihsel denetim durumunu gösterir; güncel kapanış için bu bölüm ve teslimat notu esas alınmalıdır.
