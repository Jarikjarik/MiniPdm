using System.Text.RegularExpressions;

namespace MiniPdm.Domain.Validation;

public static partial class DesignationValidator
{
    [GeneratedRegex(@"^[А-ЯЁ]{4}\.\d{6}\.\d{3}$")]
    private static partial Regex DesignationRegex();

    public static bool IsValid(string? designation)
    {
        if (string.IsNullOrWhiteSpace(designation))
        {
            return false;
        }

        return DesignationRegex().IsMatch(designation);
    }
}