using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RevertIdentitySchemaToDefaultEnglishNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kullanici_claimleri_kullanicilar_kullanici_id",
                table: "kullanici_claimleri");

            migrationBuilder.DropForeignKey(
                name: "FK_kullanici_girisleri_kullanicilar_kullanici_id",
                table: "kullanici_girisleri");

            migrationBuilder.DropForeignKey(
                name: "FK_kullanici_rolleri_kullanicilar_kullanici_id",
                table: "kullanici_rolleri");

            migrationBuilder.DropForeignKey(
                name: "FK_kullanici_rolleri_roller_rol_id",
                table: "kullanici_rolleri");

            migrationBuilder.DropForeignKey(
                name: "FK_kullanici_tokenlari_kullanicilar_kullanici_id",
                table: "kullanici_tokenlari");

            migrationBuilder.DropForeignKey(
                name: "FK_rol_claimleri_roller_rol_id",
                table: "rol_claimleri");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roller",
                table: "roller");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rol_claimleri",
                table: "rol_claimleri");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kullanicilar",
                table: "kullanicilar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kullanici_tokenlari",
                table: "kullanici_tokenlari");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kullanici_rolleri",
                table: "kullanici_rolleri");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kullanici_girisleri",
                table: "kullanici_girisleri");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kullanici_claimleri",
                table: "kullanici_claimleri");

            migrationBuilder.RenameTable(
                name: "roller",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "rol_claimleri",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "kullanicilar",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "kullanici_tokenlari",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "kullanici_rolleri",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "kullanici_girisleri",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "kullanici_claimleri",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalize_isim",
                table: "AspNetRoles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "isim",
                table: "AspNetRoles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "eszamanlilik_damgasi",
                table: "AspNetRoles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameIndex(
                name: "IX_roller_normalize_isim",
                table: "AspNetRoles",
                newName: "RoleNameIndex");

            migrationBuilder.Sql("""
                UPDATE "AspNetUserRoles" AS ur
                SET "rol_id" = turkce_rol."Id"
                FROM "AspNetRoles" AS turkce_rol, "AspNetRoles" AS ingilizce_rol
                WHERE turkce_rol."NormalizedName" = 'UYE'
                  AND ingilizce_rol."NormalizedName" = 'MEMBER'
                  AND ur."rol_id" = ingilizce_rol."Id";

                DELETE FROM "AspNetRoles" AS ingilizce_rol
                USING "AspNetRoles" AS turkce_rol
                WHERE turkce_rol."NormalizedName" = 'UYE'
                  AND ingilizce_rol."NormalizedName" = 'MEMBER';

                UPDATE "AspNetUserRoles" AS ur
                SET "rol_id" = turkce_rol."Id"
                FROM "AspNetRoles" AS turkce_rol, "AspNetRoles" AS ingilizce_rol
                WHERE turkce_rol."NormalizedName" = 'EGITMEN'
                  AND ingilizce_rol."NormalizedName" = 'TRAINER'
                  AND ur."rol_id" = ingilizce_rol."Id";

                DELETE FROM "AspNetRoles" AS ingilizce_rol
                USING "AspNetRoles" AS turkce_rol
                WHERE turkce_rol."NormalizedName" = 'EGITMEN'
                  AND ingilizce_rol."NormalizedName" = 'TRAINER';

                UPDATE "AspNetRoles"
                SET "Name" = CASE "NormalizedName"
                    WHEN 'ADMIN' THEN 'Admin'
                    WHEN 'UYE' THEN 'Member'
                    WHEN 'EGITMEN' THEN 'Trainer'
                    ELSE "Name"
                END,
                "NormalizedName" = CASE "NormalizedName"
                    WHEN 'ADMIN' THEN 'ADMIN'
                    WHEN 'UYE' THEN 'MEMBER'
                    WHEN 'EGITMEN' THEN 'TRAINER'
                    ELSE "NormalizedName"
                END;
                """);

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "rol_id",
                table: "AspNetRoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_tipi",
                table: "AspNetRoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameColumn(
                name: "claim_degeri",
                table: "AspNetRoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameIndex(
                name: "IX_rol_claimleri_rol_id",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "telefon_onayli",
                table: "AspNetUsers",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "telefon",
                table: "AspNetUsers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "soyad",
                table: "AspNetUsers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "son_giris_zamani",
                table: "AspNetUsers",
                newName: "LastLoginAtUtc");

            migrationBuilder.RenameColumn(
                name: "parola_hash",
                table: "AspNetUsers",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "olusturma_zamani",
                table: "AspNetUsers",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "normalize_kullanici_adi",
                table: "AspNetUsers",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalize_eposta",
                table: "AspNetUsers",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "kullanici_adi",
                table: "AspNetUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "kilit_bitis",
                table: "AspNetUsers",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "kilit_aktif",
                table: "AspNetUsers",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "iki_adimli_dogrulama_aktif",
                table: "AspNetUsers",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "guvenlik_damgasi",
                table: "AspNetUsers",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "eszamanlilik_damgasi",
                table: "AspNetUsers",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "eposta_onayli",
                table: "AspNetUsers",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "eposta",
                table: "AspNetUsers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "basarisiz_giris_sayisi",
                table: "AspNetUsers",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "ad",
                table: "AspNetUsers",
                newName: "FirstName");

            migrationBuilder.RenameIndex(
                name: "IX_kullanicilar_normalize_kullanici_adi",
                table: "AspNetUsers",
                newName: "UserNameIndex");

            migrationBuilder.RenameIndex(
                name: "IX_kullanicilar_normalize_eposta",
                table: "AspNetUsers",
                newName: "EmailIndex");

            migrationBuilder.RenameColumn(
                name: "deger",
                table: "AspNetUserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "ad",
                table: "AspNetUserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "giris_saglayici",
                table: "AspNetUserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "kullanici_id",
                table: "AspNetUserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "rol_id",
                table: "AspNetUserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "kullanici_id",
                table: "AspNetUserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_kullanici_rolleri_rol_id",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "saglayici_adi",
                table: "AspNetUserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "kullanici_id",
                table: "AspNetUserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "saglayici_anahtari",
                table: "AspNetUserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "giris_saglayici",
                table: "AspNetUserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "IX_kullanici_girisleri_kullanici_id",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "kullanici_id",
                table: "AspNetUserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_tipi",
                table: "AspNetUserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameColumn(
                name: "claim_degeri",
                table: "AspNetUserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameIndex(
                name: "IX_kullanici_claimleri_kullanici_id",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "kullanici_tokenlari");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "kullanicilar");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "kullanici_rolleri");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "kullanici_girisleri");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "kullanici_claimleri");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "roller");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "rol_claimleri");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "kullanici_tokenlari",
                newName: "deger");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "kullanici_tokenlari",
                newName: "ad");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "kullanici_tokenlari",
                newName: "giris_saglayici");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "kullanici_tokenlari",
                newName: "kullanici_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "kullanicilar",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "kullanicilar",
                newName: "kullanici_adi");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "kullanicilar",
                newName: "iki_adimli_dogrulama_aktif");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "kullanicilar",
                newName: "guvenlik_damgasi");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "kullanicilar",
                newName: "telefon_onayli");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "kullanicilar",
                newName: "telefon");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "kullanicilar",
                newName: "parola_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "kullanicilar",
                newName: "normalize_kullanici_adi");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "kullanicilar",
                newName: "normalize_eposta");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "kullanicilar",
                newName: "kilit_bitis");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "kullanicilar",
                newName: "kilit_aktif");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "kullanicilar",
                newName: "soyad");

            migrationBuilder.RenameColumn(
                name: "LastLoginAtUtc",
                table: "kullanicilar",
                newName: "son_giris_zamani");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "kullanicilar",
                newName: "ad");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "kullanicilar",
                newName: "eposta_onayli");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "kullanicilar",
                newName: "eposta");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "kullanicilar",
                newName: "olusturma_zamani");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "kullanicilar",
                newName: "eszamanlilik_damgasi");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "kullanicilar",
                newName: "basarisiz_giris_sayisi");

            migrationBuilder.RenameIndex(
                name: "UserNameIndex",
                table: "kullanicilar",
                newName: "IX_kullanicilar_normalize_kullanici_adi");

            migrationBuilder.RenameIndex(
                name: "EmailIndex",
                table: "kullanicilar",
                newName: "IX_kullanicilar_normalize_eposta");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "kullanici_rolleri",
                newName: "rol_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "kullanici_rolleri",
                newName: "kullanici_id");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "kullanici_rolleri",
                newName: "IX_kullanici_rolleri_rol_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "kullanici_girisleri",
                newName: "kullanici_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "kullanici_girisleri",
                newName: "saglayici_adi");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "kullanici_girisleri",
                newName: "saglayici_anahtari");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "kullanici_girisleri",
                newName: "giris_saglayici");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "kullanici_girisleri",
                newName: "IX_kullanici_girisleri_kullanici_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "kullanici_claimleri",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "kullanici_claimleri",
                newName: "kullanici_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "kullanici_claimleri",
                newName: "claim_degeri");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "kullanici_claimleri",
                newName: "claim_tipi");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "kullanici_claimleri",
                newName: "IX_kullanici_claimleri_kullanici_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "roller",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "roller",
                newName: "normalize_isim");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "roller",
                newName: "isim");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "roller",
                newName: "eszamanlilik_damgasi");

            migrationBuilder.RenameIndex(
                name: "RoleNameIndex",
                table: "roller",
                newName: "IX_roller_normalize_isim");

            migrationBuilder.Sql("""
                UPDATE "kullanici_rolleri" AS kr
                SET "rol_id" = eski_rol."id"
                FROM "roller" AS eski_rol, "roller" AS yeni_rol
                WHERE eski_rol."normalize_isim" = 'MEMBER'
                  AND yeni_rol."normalize_isim" = 'UYE'
                  AND kr."rol_id" = yeni_rol."id";

                DELETE FROM "roller" AS yeni_rol
                USING "roller" AS eski_rol
                WHERE eski_rol."normalize_isim" = 'MEMBER'
                  AND yeni_rol."normalize_isim" = 'UYE';

                UPDATE "kullanici_rolleri" AS kr
                SET "rol_id" = eski_rol."id"
                FROM "roller" AS eski_rol, "roller" AS yeni_rol
                WHERE eski_rol."normalize_isim" = 'TRAINER'
                  AND yeni_rol."normalize_isim" = 'EGITMEN'
                  AND kr."rol_id" = yeni_rol."id";

                DELETE FROM "roller" AS yeni_rol
                USING "roller" AS eski_rol
                WHERE eski_rol."normalize_isim" = 'TRAINER'
                  AND yeni_rol."normalize_isim" = 'EGITMEN';

                UPDATE "roller"
                SET "isim" = CASE "normalize_isim"
                    WHEN 'ADMIN' THEN 'admin'
                    WHEN 'MEMBER' THEN 'uye'
                    WHEN 'TRAINER' THEN 'egitmen'
                    ELSE "isim"
                END,
                "normalize_isim" = CASE "normalize_isim"
                    WHEN 'ADMIN' THEN 'ADMIN'
                    WHEN 'MEMBER' THEN 'UYE'
                    WHEN 'TRAINER' THEN 'EGITMEN'
                    ELSE "normalize_isim"
                END;
                """);

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "rol_claimleri",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "rol_claimleri",
                newName: "rol_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "rol_claimleri",
                newName: "claim_degeri");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "rol_claimleri",
                newName: "claim_tipi");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "rol_claimleri",
                newName: "IX_rol_claimleri_rol_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_kullanici_tokenlari",
                table: "kullanici_tokenlari",
                columns: new[] { "kullanici_id", "giris_saglayici", "ad" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_kullanicilar",
                table: "kullanicilar",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_kullanici_rolleri",
                table: "kullanici_rolleri",
                columns: new[] { "kullanici_id", "rol_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_kullanici_girisleri",
                table: "kullanici_girisleri",
                columns: new[] { "giris_saglayici", "saglayici_anahtari" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_kullanici_claimleri",
                table: "kullanici_claimleri",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roller",
                table: "roller",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_rol_claimleri",
                table: "rol_claimleri",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_kullanici_claimleri_kullanicilar_kullanici_id",
                table: "kullanici_claimleri",
                column: "kullanici_id",
                principalTable: "kullanicilar",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kullanici_girisleri_kullanicilar_kullanici_id",
                table: "kullanici_girisleri",
                column: "kullanici_id",
                principalTable: "kullanicilar",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kullanici_rolleri_kullanicilar_kullanici_id",
                table: "kullanici_rolleri",
                column: "kullanici_id",
                principalTable: "kullanicilar",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kullanici_rolleri_roller_rol_id",
                table: "kullanici_rolleri",
                column: "rol_id",
                principalTable: "roller",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kullanici_tokenlari_kullanicilar_kullanici_id",
                table: "kullanici_tokenlari",
                column: "kullanici_id",
                principalTable: "kullanicilar",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rol_claimleri_roller_rol_id",
                table: "rol_claimleri",
                column: "rol_id",
                principalTable: "roller",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
