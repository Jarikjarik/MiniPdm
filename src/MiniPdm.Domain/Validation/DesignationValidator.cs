using System.Text.RegularExpressions;

namespace MiniPdm.Domain.Validation;

public static partial class DesignationValidator
{
    public static bool IsValid(
        string? designation)
    {
        if (string.IsNullOrWhiteSpace(designation))
        {
            return false;
        }

        return DesignationRegex().IsMatch(
            designation);
    }

    [GeneratedRegex(
        @"^[А-ЯЁ]{4}\.[0-9]{6}\.[0-9]{3}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex DesignationRegex();
}