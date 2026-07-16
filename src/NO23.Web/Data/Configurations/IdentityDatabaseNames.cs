using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public static class IdentityDatabaseNames
{
    public static void ApplyIdentityDatabaseNames(this ModelBuilder builder)
    {
        ConfigureUsers(builder);
        ConfigureRoles(builder);
        ConfigureUserRoles(builder);
        ConfigureUserClaims(builder);
        ConfigureRoleClaims(builder);
        ConfigureUserLogins(builder);
        ConfigureUserTokens(builder);
    }

    private static void ConfigureUsers(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("kullanicilar");
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.UserName).HasColumnName("kullanici_adi");
            entity.Property(user => user.NormalizedUserName).HasColumnName("normalize_kullanici_adi");
            entity.Property(user => user.Email).HasColumnName("eposta");
            entity.Property(user => user.NormalizedEmail).HasColumnName("normalize_eposta");
            entity.Property(user => user.EmailConfirmed).HasColumnName("eposta_onayli");
            entity.Property(user => user.PasswordHash).HasColumnName("parola_hash");
            entity.Property(user => user.SecurityStamp).HasColumnName("guvenlik_damgasi");
            entity.Property(user => user.ConcurrencyStamp).HasColumnName("eszamanlilik_damgasi");
            entity.Property(user => user.PhoneNumber).HasColumnName("telefon");
            entity.Property(user => user.PhoneNumberConfirmed).HasColumnName("telefon_onayli");
            entity.Property(user => user.TwoFactorEnabled).HasColumnName("iki_adimli_dogrulama_aktif");
            entity.Property(user => user.LockoutEnd).HasColumnName("kilit_bitis");
            entity.Property(user => user.LockoutEnabled).HasColumnName("kilit_aktif");
            entity.Property(user => user.AccessFailedCount).HasColumnName("basarisiz_giris_sayisi");
            entity.Property(user => user.FirstName).HasColumnName("ad");
            entity.Property(user => user.LastName).HasColumnName("soyad");
            entity.Property(user => user.CreatedAtUtc).HasColumnName("olusturma_zamani");
            entity.Property(user => user.LastLoginAtUtc).HasColumnName("son_giris_zamani");
            entity.HasIndex(user => user.NormalizedEmail).HasDatabaseName("IX_kullanicilar_normalize_eposta");
            entity.HasIndex(user => user.NormalizedUserName)
                .HasDatabaseName("IX_kullanicilar_normalize_kullanici_adi")
                .IsUnique();
        });
    }

    private static void ConfigureRoles(ModelBuilder builder)
    {
        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("roller");
            entity.Property(role => role.Id).HasColumnName("id");
            entity.Property(role => role.Name).HasColumnName("isim");
            entity.Property(role => role.NormalizedName).HasColumnName("normalize_isim");
            entity.Property(role => role.ConcurrencyStamp).HasColumnName("eszamanlilik_damgasi");
            entity.HasIndex(role => role.NormalizedName)
                .HasDatabaseName("IX_roller_normalize_isim")
                .IsUnique();
        });
    }

    private static void ConfigureUserRoles(ModelBuilder builder)
    {
        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("kullanici_rolleri");
            entity.Property(userRole => userRole.UserId).HasColumnName("kullanici_id");
            entity.Property(userRole => userRole.RoleId).HasColumnName("rol_id");
        });
    }

    private static void ConfigureUserClaims(ModelBuilder builder)
    {
        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("kullanici_claimleri");
            entity.Property(claim => claim.Id).HasColumnName("id");
            entity.Property(claim => claim.UserId).HasColumnName("kullanici_id");
            entity.Property(claim => claim.ClaimType).HasColumnName("claim_tipi");
            entity.Property(claim => claim.ClaimValue).HasColumnName("claim_degeri");
        });
    }

    private static void ConfigureRoleClaims(ModelBuilder builder)
    {
        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("rol_claimleri");
            entity.Property(claim => claim.Id).HasColumnName("id");
            entity.Property(claim => claim.RoleId).HasColumnName("rol_id");
            entity.Property(claim => claim.ClaimType).HasColumnName("claim_tipi");
            entity.Property(claim => claim.ClaimValue).HasColumnName("claim_degeri");
        });
    }

    private static void ConfigureUserLogins(ModelBuilder builder)
    {
        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("kullanici_girisleri");
            entity.Property(login => login.LoginProvider).HasColumnName("giris_saglayici");
            entity.Property(login => login.ProviderKey).HasColumnName("saglayici_anahtari");
            entity.Property(login => login.ProviderDisplayName).HasColumnName("saglayici_adi");
            entity.Property(login => login.UserId).HasColumnName("kullanici_id");
        });
    }

    private static void ConfigureUserTokens(ModelBuilder builder)
    {
        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("kullanici_tokenlari");
            entity.Property(token => token.UserId).HasColumnName("kullanici_id");
            entity.Property(token => token.LoginProvider).HasColumnName("giris_saglayici");
            entity.Property(token => token.Name).HasColumnName("ad");
            entity.Property(token => token.Value).HasColumnName("deger");
        });
    }
}
