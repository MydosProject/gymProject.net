namespace NO23.Web.Data.Seed;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string Trainer = "Trainer";

    public static readonly string[] All =
    [
        Admin,
        Member,
        Trainer
    ];
}
