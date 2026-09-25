using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NO23.Web.Infrastructure.Identity;

public static class MemberLoginName
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    public static string BuildUserName(string firstName, string lastName)
    {
        return BuildUserName($"{firstName} {lastName}");
    }

    public static string BuildUserName(string fullName)
    {
        var normalizedName = Normalize(fullName);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedName));

        return $"member-{Convert.ToHexString(hash)}";
    }

    public static bool Matches(
        string fullName,
        string? firstName,
        string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            return false;
        }

        return string.Equals(
            Normalize(fullName),
            Normalize($"{firstName} {lastName}"),
            StringComparison.Ordinal);
    }

    public static string CleanPart(string value)
    {
        return CollapseWhiteSpace(value);
    }

    private static string Normalize(string value)
    {
        return CollapseWhiteSpace(value).ToUpper(TurkishCulture);
    }

    private static string CollapseWhiteSpace(string value)
    {
        return string.Join(
            ' ',
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));
    }
}
