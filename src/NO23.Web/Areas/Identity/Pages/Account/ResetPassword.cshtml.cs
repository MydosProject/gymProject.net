using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Areas.Identity.Pages.Account;

public class ResetPasswordModel(
    UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet(string? code = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(
                string.Empty,
                "Parola sıfırlama bağlantısı geçersiz veya eksik.");

            return Page();
        }

        try
        {
            Input.Code = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Parola sıfırlama bağlantısı geçersiz veya eksik.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);

        if (user is null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await userManager.ResetPasswordAsync(
            user,
            Input.Code,
            Input.Password);

        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girmelisin.")]
        [Display(Name = "E-posta adresi")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [StringLength(
            100,
            ErrorMessage = "{0} en az {2}, en fazla {1} karakter olmalıdır.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni parola")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Yeni parola tekrar")]
        [Compare(
            nameof(Password),
            ErrorMessage = "Parola ve tekrar alanı eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola sıfırlama bağlantısı geçersiz veya eksik.")]
        public string Code { get; set; } = string.Empty;
    }
}
