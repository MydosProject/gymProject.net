using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Infrastructure.Validation;

public sealed class OptionalEmailUserValidator : IUserValidator<ApplicationUser>
{
    private static readonly EmailAddressAttribute EmailAddressValidator = new();

    public async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user)
    {
        var email = await manager.GetEmailAsync(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            return IdentityResult.Success;
        }

        if (!EmailAddressValidator.IsValid(email))
        {
            return IdentityResult.Failed(
                manager.ErrorDescriber.InvalidEmail(email));
        }

        var emailOwner = await manager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != user.Id)
        {
            return IdentityResult.Failed(
                manager.ErrorDescriber.DuplicateEmail(email));
        }

        return IdentityResult.Success;
    }
}
