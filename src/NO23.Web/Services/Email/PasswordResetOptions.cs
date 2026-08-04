namespace NO23.Web.Services.Email;

public class PasswordResetOptions
{
    public int TokenLifespanMinutes { get; set; } = 60;
}
