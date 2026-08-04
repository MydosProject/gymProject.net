using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace NO23.Web.Infrastructure.Validation;

public sealed class TurkishValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var validatorMetadata in context.ValidationMetadata.ValidatorMetadata)
        {
            if (validatorMetadata is ValidationAttribute attribute)
            {
                ApplyDefaultMessage(attribute);
            }
        }
    }

    public static void ApplyDefaultMessage(ValidationAttribute attribute)
    {
        if (!string.IsNullOrWhiteSpace(attribute.ErrorMessage) ||
            attribute.ErrorMessageResourceType is not null ||
            !string.IsNullOrWhiteSpace(attribute.ErrorMessageResourceName))
        {
            return;
        }

        attribute.ErrorMessage = attribute switch
        {
            RequiredAttribute =>
                "{0} alanı zorunludur.",
            RangeAttribute =>
                "{0} {1} ile {2} arasında olmalıdır.",
            StringLengthAttribute stringLength when stringLength.MinimumLength > 0 =>
                "{0} en az {2}, en fazla {1} karakter olmalıdır.",
            StringLengthAttribute =>
                "{0} en fazla {1} karakter olabilir.",
            MinLengthAttribute =>
                "{0} en az {1} karakter olmalıdır.",
            MaxLengthAttribute =>
                "{0} en fazla {1} karakter olabilir.",
            EmailAddressAttribute =>
                "Geçerli bir e-posta adresi girmelisin.",
            PhoneAttribute =>
                "Geçerli bir telefon numarası girmelisin.",
            UrlAttribute =>
                "Geçerli bir URL girmelisin.",
            CompareAttribute =>
                "{0} alanı eşleşmiyor.",
            RegularExpressionAttribute =>
                "{0} için geçerli bir değer girmelisin.",
            _ => attribute.ErrorMessage
        };
    }
}
