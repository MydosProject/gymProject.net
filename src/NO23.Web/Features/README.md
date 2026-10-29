# Uygulama modülleri

İlk sınır `Membership/MemberDirectoryService`: üye listesinin sorgusu, filtreleri,
sayfalaması ve genel istatistikleri bu servise aittir. Controller HTTP girdisini
doğrular ve mevcut Razor ekranına aynı satır modelini verir.

`MembershipModule` bağımlılıkları scoped olarak kaydeder; servisler aynı request
içindeki `ApplicationDbContext` örneğini paylaşır. Saat `IClubClock` ile sağlanır.
Listeleme yalnız okuma yapar; paket, ders hakkı ve tahsilat yazma kuralları mevcut
servislerinde kalır. Özetler mevcut okuma servislerinden alınır.

Sonraki çıkarımlarda önce davranış testleri eklenir. Controller ve view dosyaları
taşınırken Area, route, antiforgery ve yetki kontrolleri korunur. Modüller arası
yazma yapılacaksa transaction sahibi ve tekrar gönderim davranışı önceden belirlenir.
