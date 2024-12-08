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
}
