namespace CFW.Core.Utils;
public static class DateTimeUtils
{
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public static bool IsDateTimeType(this Type type)
    {
        return type == typeof(DateTime)
             || type == typeof(DateTime?)
             || type == typeof(DateTimeOffset)
             | type == typeof(DateTimeOffset?);
    }
}
