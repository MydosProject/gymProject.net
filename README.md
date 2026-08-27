# NO23 Sports Club

## Proje Tanıtımı

NO23 Sports Club; spor kulübü deneyimini üyelik, antrenman, beslenme,
alışveriş ve topluluk özellikleriyle tek bir platformda birleştiren
ASP.NET Core MVC tabanlı bir web uygulamasıdır.

Proje dört temel kullanıcı deneyiminden oluşur:

- Ziyaretçilerin kulübü ve hizmetleri inceleyebildiği public site
- Üyelerin kendilerine özel işlemleri gerçekleştirebildiği üye paneli
- Antrenörlerin kişisel antrenman takvimlerini yönetebildiği antrenör paneli
- Kulüp operasyonlarının yönetildiği admin paneli

## V3 Sürüm Notları

Yayın tarihi: 27 Ağustos 2026

### Ticaret ve sipariş deneyimi

- Mağaza ürünlerine beden, renk, stok ve fiyat farkı destekleyen ürün varyantları eklendi.
- Sepet ve sipariş kalemleri seçilen ürün varyantını koruyacak şekilde güncellendi.
- Kargo ve salondan teslim seçenekleri hem ziyaretçi hem üye sipariş akışlarına eklendi.
- Salondan teslim seçeneği, yönetilebilir şube adı ve adres bilgileriyle yapılandırılabilir hale getirildi.
- Türkiye il ve ilçe verileriyle adres girişleri iyileştirildi.
- Admin sipariş listesine teslimat yöntemi ve teslimat bilgileri eklendi.

### Üyelik ve hizmet paketleri

- Ziyaretçilerin üyelik ve hizmet paketleri için başvuru gönderebildiği yeni başvuru akışı eklendi.
- Başvuruların admin panelinden görüntülenmesi ve durumlarının yönetilmesi sağlandı.
- Paket kataloğu ve başvuru formları mobil kullanıma uygun hale getirildi.

### Etkinlik ve ders yönetimi

- Topluluk etkinliklerine kapasite kontrollü rezervasyon ve rezervasyon iptali eklendi.
- Etkinlik yaşam döngüsü, doluluk bilgisi ve kullanıcı rezervasyon durumu ekranlara yansıtıldı.
- Admin ders listesine kapasite, rezervasyon ve katılım bilgileri eklendi.
- Ders rezervasyon kontrolleri ve ilgili test kapsamı genişletildi.

### Kitchen ve kullanıcı deneyimi

- Kitchen siparişlerine porsiyon, malzeme çıkarma ve özel not seçenekleri eklendi.
- Ürün, Kitchen, sepet ve ödeme ekranlarının mobil görünümü yenilendi.
- Üye ve ziyaretçi sipariş akışlarındaki doğrulamalar ve teslimat özetleri iyileştirildi.
- Blog, başarı hikâyeleri, galeri, topluluk ve plan sayfalarında responsive arayüz düzenlemeleri yapıldı.

### Altyapı ve doğrulama

- Yeni özellikler için Entity Framework Core migration ve veritabanı yapılandırmaları eklendi.
- Ürün varyantı, teslimat yöntemi, etkinlik rezervasyonu, Kitchen özelleştirmesi ve paket başvurusu testleri eklendi.
- Iyzico ve şubeden teslim ayarları kaynak kod dışında secret veya environment variable üzerinden yapılandırılabilir hale getirildi.

## V2 ile Gelen Yenilikler

- Antrenörlere üye atama, panel hesabı oluşturma ve kişisel antrenman
  takvimi yönetimi
- Üye bilgilerinin admin panelinden düzenlenmesi ve güvenli biçimde silinmesi
- Mutfak menüsünde ilişkisel alerjen tanımları ve üyeye özel alerjen uyarıları
- Üyelik paketi seçenekleri ile birleşik hizmet paketi kataloğu
- Paket özellikleri, varyantları ve faturalandırma türleri için yönetim ekranları
- Üye profili, mağaza, mutfak ve admin arayüzlerinde kullanılabilirlik iyileştirmeleri
- Yeni özellikleri kapsayan Entity Framework Core migration, seed verileri ve xUnit testleri

## Kullanılan Teknolojiler

- .NET 10
- ASP.NET Core MVC
- Razor Views ve Razor Pages
- Entity Framework Core
- PostgreSQL
- Npgsql Entity Framework Core Provider
- ASP.NET Core Identity
- Bootstrap
- HTML, CSS ve JavaScript
- xUnit
- Swagger
- Docker Compose

## Proje Klasör Yapısı

```text
NO23SportsClub/
├── src/
│   └── NO23.Web/
│       ├── Areas/
│       │   ├── Admin/          # Admin controller ve view dosyaları
│       │   ├── Identity/       # Giriş ve kayıt sayfaları
│       │   ├── Member/         # Üye paneli controller ve view dosyaları
│       │   └── Trainer/        # Antrenör paneli ve takvim ekranları
│       ├── Controllers/        # Public site controller'ları
│       ├── Data/
│       │   ├── Configurations/ # Entity Framework yapılandırmaları
│       │   ├── Migrations/     # Veritabanı migration dosyaları
│       │   └── Seed/           # Başlangıç verileri
│       ├── Domain/
│       │   ├── Entities/       # Veritabanı entity'leri
│       │   └── Enums/          # Durum ve tür tanımları
│       ├── Services/           # Rezervasyon, kalori, sepet ve sipariş işlemleri
│       ├── ViewComponents/     # Tekrar kullanılabilen dinamik arayüz parçaları
│       ├── ViewModels/         # Sayfalara özel veri modelleri
│       ├── Views/              # Public Razor View dosyaları
│       ├── wwwroot/
│       │   ├── css/            # Stil dosyaları
│       │   ├── images/         # Görseller
│       │   ├── js/             # JavaScript dosyaları
│       │   └── videos/         # Video dosyaları
│       └── Program.cs          # Uygulama ve route yapılandırması
├── tests/
│   └── NO23.Tests/             # xUnit test projesi
├── docker-compose.yml          # Yerel PostgreSQL servisi
└── NO23SportsClub.slnx         # Solution dosyası
```

## Projeyi Çalıştırma

Projeyi çalıştırabilmek için bilgisayarda .NET 10 SDK, Docker Desktop ve
Git kurulu olmalıdır.

1. Repoyu klonlayın ve proje klasörüne geçin:

   ```bash
   git clone <REPOSITORY_URL>
   cd NO23SportsClub
   ```

2. Yerel .NET araçlarını yükleyin:

   ```bash
   dotnet tool restore
   ```

3. Ortam değişkenleri dosyasını oluşturun:

   ```bash
   cp .env.example .env
   ```

   `.env` dosyasındaki PostgreSQL bilgilerini kendi yerel geliştirme
   ortamınıza göre düzenleyin.

4. PostgreSQL servisini başlatın:

   ```bash
   docker compose up -d
   ```

5. `.env` dosyasında belirlediğiniz veritabanı bilgileriyle uygulamanın
   bağlantı metnini user-secrets üzerinden tanımlayın:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=no23db;Username=no23;Password=<YEREL_VERITABANI_SIFRENIZ>" --project src/NO23.Web/NO23.Web.csproj
   ```

6. Admin panelini kullanmak için yerel admin hesabını tanımlayın:

   ```bash
   dotnet user-secrets set "SeedAdmin:Email" "<ADMIN_EPOSTA>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "SeedAdmin:Password" "<GUCLU_ADMIN_SIFRESI>" --project src/NO23.Web/NO23.Web.csproj
   ```

7. Parola sıfırlama e-postalarını gerçek Gmail SMTP üzerinden test etmek
   isterseniz Gmail uygulama şifresini user-secrets ile tanımlayın:

   ```bash
   dotnet user-secrets set "Email:Smtp:Enabled" "true" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "Email:Smtp:UserName" "<GMAIL_EPOSTA>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "Email:Smtp:Password" "<GMAIL_UYGULAMA_SIFRESI>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "Email:Smtp:FromAddress" "<GMAIL_EPOSTA>" --project src/NO23.Web/NO23.Web.csproj
   ```

   Bu adım yerel geliştirme için zorunlu değildir. SMTP bilgileri
   tanımlı değilse development ortamında parola sıfırlama bağlantısı
   uygulama loglarına yazılır.

   Deployment sorumlusu aynı ayarları kendi secret sistemi üzerinden aşağıdaki environment variable adlarıyla tanımlar:

   ```text
   Email__Smtp__Enabled=true
   Email__Smtp__UserName=<GMAIL_EPOSTA>
   Email__Smtp__Password=<GMAIL_UYGULAMA_SIFRESI>
   Email__Smtp__FromAddress=<GMAIL_EPOSTA>
   ```

8. iyzico sandbox ödeme ekranını kullanmak için sandbox hesabınıza ait bilgileri
   kaynak koda veya `.env` dosyasına yazmadan user-secrets ile tanımlayın:

   ```bash
   dotnet user-secrets set "Iyzico:Enabled" "true" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "Iyzico:ApiKey" "<IYZICO_SANDBOX_API_KEY>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "Iyzico:SecretKey" "<IYZICO_SANDBOX_SECRET_KEY>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "Iyzico:CallbackUrl" "https://<HTTPS_TUNNEL_HOST>/payment/iyzico/callback" --project src/NO23.Web/NO23.Web.csproj
   ```

   Callback adresi iyzico tarafından erişilebilir bir HTTPS adresi olmalıdır.
   Yerel HTTP adresi doğrudan callback olarak kullanılamaz; uygulamanın yerel
   portunu güvenilir bir HTTPS geliştirme tüneli üzerinden yayınlayın. Ayarlar
   eksikken online ödeme butonu güvenli biçimde devre dışı kalır ve sipariş ya
   da stok hareketi oluşturulmaz.

   Salondan teslim seçeneğini açmak için gerçek şube bilgilerini yapılandırın:

   ```bash
   dotnet user-secrets set "ClubPickup:Enabled" "true" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "ClubPickup:DisplayName" "<SUBE_ADI>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "ClubPickup:AddressLine" "<SUBE_ACIK_ADRESI>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "ClubPickup:District" "<SUBE_ILCESI>" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "ClubPickup:City" "<SUBE_SEHRI>" --project src/NO23.Web/NO23.Web.csproj
   ```

   Şube bilgileri eksikse seçenek ekranda hazırlık durumunda gösterilir ve
   backend salondan teslim siparişi oluşturmaz.

9. Veritabanı migration'larını uygulayın:

   ```bash
   dotnet ef database update --project src/NO23.Web/NO23.Web.csproj
   ```

10. Uygulamayı çalıştırın:

   ```bash
   dotnet run --project src/NO23.Web/NO23.Web.csproj
   ```

Uygulama varsayılan geliştirme ayarlarıyla
`https://localhost:7032` adresinden açılabilir.
