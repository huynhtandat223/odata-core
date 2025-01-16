using System.Text.RegularExpressions;

namespace CFW.Core.Utils;

public static class StringUtils
{
    public static bool CompareIgnoreCase(this string? a, string? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;

        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsNotNullOrNotWhiteSpace(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static string FormatOdataFilter(this object value)
    {
        if (value is string str)
        {
            return $"'{str}'";
        }
        if (value is DateTime dt)
        {
            return dt.ToIso8601String();
        }
        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        if (value is Enum @enum)
        {
            return $"'{@enum}'";
        }

        return value.ToString()!;
    }

    public static string ToPascalCase(this string original)
    {
        Regex invalidCharsRgx = new Regex("[^a-zA-Z0-9_-]");
        Regex whiteSpace = new Regex(@"(?<=\s)");
        Regex startsWithLowerCaseChar = new Regex("^[a-z]");
        Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
        Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
        Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

        // Replace whitespace with underscore and remove invalid characters
        var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)
            // Split by underscores and hyphens
            .Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            // Capitalize the first letter of each word
            .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
            // Replace sequences of uppercase letters to lower if no following lowercase exists (ABC -> Abc)
            .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
            // Capitalize the first lowercase letter after a number (Ab9cd -> Ab9Cd)
            .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
            // Convert consecutive uppercase letters to lowercase if followed by a lowercase letter (ABcDEf -> AbcDef)
            .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

        return string.Concat(pascalCase);
    }


    /// <summary>
    /// From Microsoft.AspNetCore.OData source code
    /// Sanitizes the route prefix by stripping leading and trailing forward slashes.
    /// </summary>
    /// <param name="routePrefix">Route prefix to sanitize.</param>
    /// <returns>Sanitized route prefix.</returns>
    public static string SanitizeRoute(string routePrefix)
    {
        if (routePrefix.Length > 0 && routePrefix[0] != '/' && routePrefix[^1] != '/')
        {
            return routePrefix;
        }

        return routePrefix.Trim('/');
    }

    public static object? Parse(this string str, Type type)
    {
        if (str.IsNullOrWhiteSpace())
            return default;

        if (type == typeof(Guid))
        {
            return Guid.Parse(str);
        }

        return Convert.ChangeType(str, type);
    }
}
