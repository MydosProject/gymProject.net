using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NO23.Web.Extensions;

public static class EnumDisplayNameExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.GetName() ?? value.ToString();
    }
}
