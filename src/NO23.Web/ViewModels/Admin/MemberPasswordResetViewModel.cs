using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class MemberPasswordResetViewModel
{
    public int Id { get; set; }
    public string MemberName { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Yeni geçici parola")]
    public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Parolalar aynı olmalıdır."), Display(Name = "Yeni geçici parola (tekrar)")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
