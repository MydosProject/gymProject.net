namespace NO23.Web.Data.Seed;

public static class ApplicationRoles
{
    public const string Admin = "admin";
    public const string Member = "uye";
    public const string Trainer = "egitmen";

    public static readonly string[] All =
    [
        Admin,
        Member,
        Trainer
    ];
}
