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

8. Veritabanı migration'larını uygulayın:

   ```bash
   dotnet ef database update --project src/NO23.Web/NO23.Web.csproj
   ```

9. Uygulamayı çalıştırın:

   ```bash
   dotnet run --project src/NO23.Web/NO23.Web.csproj
   ```

Uygulama varsayılan geliştirme ayarlarıyla
`https://localhost:7032` adresinden açılabilir.
