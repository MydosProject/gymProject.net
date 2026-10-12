using Microsoft.AspNetCore.Identity;
using System.Text;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Infrastructure.Validation;

public sealed class UnicodePasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        var options = manager.Options.Password;
        var errors = new List<IdentityError>();
        var runes = (password ?? "").EnumerateRunes().ToArray();
        var describer = manager.ErrorDescriber;
        if (string.IsNullOrWhiteSpace(password) || password.Length < options.RequiredLength)
            errors.Add(describer.PasswordTooShort(options.RequiredLength));
        if (options.RequireUppercase && !runes.Any(Rune.IsUpper)) errors.Add(describer.PasswordRequiresUpper());
        if (options.RequireLowercase && !runes.Any(Rune.IsLower)) errors.Add(describer.PasswordRequiresLower());
        if (options.RequireDigit && !runes.Any(Rune.IsDigit)) errors.Add(describer.PasswordRequiresDigit());
        if (options.RequireNonAlphanumeric && !runes.Any(rune => !Rune.IsLetterOrDigit(rune)))
            errors.Add(describer.PasswordRequiresNonAlphanumeric());
        if (runes.Select(rune => rune.Value).Distinct().Count() < options.RequiredUniqueChars)
            errors.Add(describer.PasswordRequiresUniqueChars(options.RequiredUniqueChars));
        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
    }
}
