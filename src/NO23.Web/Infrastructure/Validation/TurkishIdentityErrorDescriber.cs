using Microsoft.AspNetCore.Identity;

namespace NO23.Web.Infrastructure.Validation;

public sealed class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
    {
        return Error(nameof(DefaultError), "Bir hata oluştu.");
    }

    public override IdentityError ConcurrencyFailure()
    {
        return Error(nameof(ConcurrencyFailure), "Kayıt başka bir işlem tarafından değiştirildi. Lütfen tekrar dene.");
    }

    public override IdentityError PasswordMismatch()
    {
        return Error(nameof(PasswordMismatch), "Mevcut şifren doğru değil.");
    }

    public override IdentityError InvalidToken()
    {
        return Error(nameof(InvalidToken), "Doğrulama bağlantısı veya kodu geçerli değil.");
    }

    public override IdentityError LoginAlreadyAssociated()
    {
        return Error(nameof(LoginAlreadyAssociated), "Bu giriş yöntemi zaten başka bir hesapla ilişkilendirilmiş.");
    }

    public override IdentityError InvalidUserName(string? userName)
    {
        return Error(nameof(InvalidUserName), $"'{userName}' kullanıcı adı geçerli değil.");
    }

    public override IdentityError InvalidEmail(string? email)
    {
        return Error(nameof(InvalidEmail), $"'{email}' e-posta adresi geçerli değil.");
    }

    public override IdentityError DuplicateUserName(string userName)
    {
        return Error(nameof(DuplicateUserName), $"'{userName}' kullanıcı adı zaten kullanılıyor.");
    }

    public override IdentityError DuplicateEmail(string email)
    {
        return Error(nameof(DuplicateEmail), $"'{email}' e-posta adresi zaten kullanılıyor.");
    }

    public override IdentityError InvalidRoleName(string? role)
    {
        return Error(nameof(InvalidRoleName), $"'{role}' rol adı geçerli değil.");
    }

    public override IdentityError DuplicateRoleName(string role)
    {
        return Error(nameof(DuplicateRoleName), $"'{role}' rol adı zaten kullanılıyor.");
    }

    public override IdentityError UserAlreadyHasPassword()
    {
        return Error(nameof(UserAlreadyHasPassword), "Bu kullanıcı için zaten bir şifre tanımlı.");
    }

    public override IdentityError UserLockoutNotEnabled()
    {
        return Error(nameof(UserLockoutNotEnabled), "Bu kullanıcı için hesap kilitleme etkin değil.");
    }

    public override IdentityError UserAlreadyInRole(string role)
    {
        return Error(nameof(UserAlreadyInRole), $"Kullanıcı zaten '{role}' rolünde.");
    }

    public override IdentityError UserNotInRole(string role)
    {
        return Error(nameof(UserNotInRole), $"Kullanıcı '{role}' rolünde değil.");
    }

    public override IdentityError PasswordTooShort(int length)
    {
        return Error(nameof(PasswordTooShort), $"Şifre en az {length} karakter olmalıdır.");
    }

    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return Error(nameof(PasswordRequiresNonAlphanumeric), "Şifrede en az bir özel karakter bulunmalıdır.");
    }

    public override IdentityError PasswordRequiresDigit()
    {
        return Error(nameof(PasswordRequiresDigit), "Şifrede en az bir rakam bulunmalıdır.");
    }

    public override IdentityError PasswordRequiresLower()
    {
        return Error(nameof(PasswordRequiresLower), "Şifrede en az bir küçük harf bulunmalıdır.");
    }

    public override IdentityError PasswordRequiresUpper()
    {
        return Error(nameof(PasswordRequiresUpper), "Şifrede en az bir büyük harf bulunmalıdır.");
    }

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
    {
        return Error(nameof(PasswordRequiresUniqueChars), $"Şifrede en az {uniqueChars} farklı karakter bulunmalıdır.");
    }

    public override IdentityError RecoveryCodeRedemptionFailed()
    {
        return Error(nameof(RecoveryCodeRedemptionFailed), "Kurtarma kodu kullanılamadı.");
    }

    private static IdentityError Error(string code, string description)
    {
        return new IdentityError
        {
            Code = code,
            Description = description
        };
    }
}
